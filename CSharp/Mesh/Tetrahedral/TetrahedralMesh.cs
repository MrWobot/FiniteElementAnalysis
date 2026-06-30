using Core.Collections;
using Core.Maths.Tensors;
using Core.Trees;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Mesh.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace FiniteElementAnalysis.Mesh.Tetrahedral
{
    public class TetrahedralMesh:IMesh
    {
        public BoundariesCollection Boundaries { get; }
        public VolumesCollection Volumes { get; }
        public IReadOnlySet<TetrahedralNode> TetrahedralNodes { get; }
        private IReadOnlySet<INode>? _Nodes;
        public IReadOnlySet<INode> Nodes { get {
                if (_Nodes == null)
                {
                    _Nodes = new HashSet<INode>(TetrahedralNodes);
                }
                return _Nodes;
            } }
        public IReadOnlySet<TetrahedralElement> TetrahedralElements { get; }
        private IReadOnlySet<IElement>? _Elements;
        public IReadOnlySet<IElement> Elements { get {
                if (_Elements == null) { 
                    _Elements = new HashSet<IElement>(TetrahedralElements);
                }
                return _Elements;
            } }
        public int NNodesPerElement => 4;
        public bool IsPartOfResult { get; set; }
        public IReadOnlySet<BoundaryFace> BoundaryFaces { get; }
        private IReadOnlySet<NonBoundaryFace>? _NonBoundaryFaces;
        public IReadOnlySet<NonBoundaryFace> NonBoundaryFaces
        {
            get
            {
                if (_NonBoundaryFaces == null)
                {
                    _NonBoundaryFaces = LoadNonBoundaryFaces();
                }
                return _NonBoundaryFaces;
            }
        }
        public IReadOnlySet<TriangleFaceBase> AllFaces
        {
            get
            {
                var allFaces = new HashSet<TriangleFaceBase>(BoundaryFaces.Count + NonBoundaryFaces.Count);
                allFaces.UnionWith(BoundaryFaces);
                allFaces.UnionWith(NonBoundaryFaces);
                return allFaces;
            }
        }
        public bool HasBoundaries { get { return Boundaries.HasEntries; } }

        private Dictionary<Boundary, BoundaryFace[]>? _MapBoundaryToFaces;

        private DictionaryDictionaryDictionaryDictionary<int, TetrahedralElement>? _MapNodesToElement;
        public DictionaryDictionaryDictionaryDictionary<int, TetrahedralElement> MapNodesToElement
        {
            get
            {
                if (_MapNodesToElement == null)
                {
                    _MapNodesToElement = new DictionaryDictionaryDictionaryDictionary<int, TetrahedralElement>();
                    foreach (var element in Elements)
                    {
                        TetrahedralElement tetrahedronElement = (TetrahedralElement)element;
                        _MapNodesToElement.Map(tetrahedronElement.NodeIdentifiersLowToHigh, tetrahedronElement);
                    }
                }
                return _MapNodesToElement;
            }
        }
        /*
        private Dictionary<int, TetrahedronElement>? _MapElementIdentifierToElement;
        public Dictionary<int, TetrahedronElement> MapElementIdentifierToElement
        {
            get
            {
                if (_MapElementIdentifierToElement == null)
                {
                    _MapElementIdentifierToElement = Elements.ToDictionary(e => e.Index, e => e);
                }
                return _MapElementIdentifierToElement;
            }
        }*/
        /*
        private Dictionary<Boundary, Node[]>? _MapBoundaryToNodes;
        private Dictionary<Boundary, Node[]> MapBoundaryToNodes
        {
            get
            {
                if (_MapBoundaryToNodes == null)
                {
                    _MapBoundaryToNodes =
                        Nodes.Where(n => n.Boundary != null)
                             .GroupBy(n => n.Boundary)
                             .ToDictionary(g => g.First().Boundary!, g => g.ToArray());
                }
                return _MapBoundaryToNodes;
            }
        }*/

        private BVH3D<TetrahedralElement>? _ElementsBVHTree;
        public BVH3D<TetrahedralElement> ElementsBVHTree
        {
            get
            {
                if (_ElementsBVHTree == null)
                {
                    _ElementsBVHTree = new BVH3D<TetrahedralElement>(Elements.Cast<TetrahedralElement>().ToList(),
                        e => e.BoundingCuboid, (e, p) => e.IsPointInside(p));
                }
                return _ElementsBVHTree;
            }
        }
        public IEnumerable<IElement> GetElementsContainingPoint(double[] point) {
            return ElementsBVHTree.QueryBVH(new Vector3D(point[0], point[1], point[2]));
        }

        private Dictionary<int, int>? _MapNodeIdentifierToGlobalIndex;
        public int GetGlobalIndexForNode(int nodeIdentifier) {

            if (_MapNodeIdentifierToGlobalIndex == null)
            {
                int nodeIndex = 0;
                _MapNodeIdentifierToGlobalIndex = Nodes.ToDictionary(n => n.Identifier, n => nodeIndex++);
            }
            return _MapNodeIdentifierToGlobalIndex[nodeIdentifier];
        }
        private Dictionary<int, HashSet<IElement>>? _MapNodeToElementsBelongsTo;
        public IReadOnlySet<IElement> GetElementsThatNodeBelongsTo(int nodeIdentifier) {

            if (_MapNodeToElementsBelongsTo == null)
            {
                _MapNodeToElementsBelongsTo = new Dictionary<int, HashSet<IElement>>();
                foreach (var element in Elements)
                {
                    foreach (var node in element.Nodes)
                    {
                        if (!_MapNodeToElementsBelongsTo.ContainsKey(node.Identifier))
                        {
                            _MapNodeToElementsBelongsTo[node.Identifier] = new HashSet<IElement>();
                        }
                        _MapNodeToElementsBelongsTo[node.Identifier].Add(element);
                    }
                }
            }
            return _MapNodeToElementsBelongsTo[nodeIdentifier];
        }

        public int NodePositionLength => 3;

        /*
public INode[] GetNeighbouringNodes(Node node)
{
List<IElement> elementsBelongsTo = MapNodeToElementsBelongsTo[node.Index];
return elementsBelongsTo
.SelectMany(e => e.Nodes)
.GroupBy(n => n.Index)
.Select(g => g.First())
.Where(n => n.Index != node.Index)
.ToArray();
}*/

        public bool HasNonLinearBoundaries()
        {
            return Boundaries
                .Entries
                .Where(b => b != null && b.IsNonLinear)
                .Where(b => GetFacesForBoundary(b) != null || GetNodesForBoundary(b) != null).Any();
        }

        public TetrahedralNode[]? GetNodesForBoundary(Boundary boundary)
        {
            return GetFacesForBoundary(boundary)?.SelectMany(b => b.Nodes).GroupBy(n => n).Select(g => g.First()).Cast<TetrahedralNode>().ToArray();
        }

        public BoundaryFace[]? GetFacesForBoundary(Boundary boundary)
        {

            if (_MapBoundaryToFaces == null)
            {
                _MapBoundaryToFaces =
                    BoundaryFaces == null
                    ? new Dictionary<Boundary, BoundaryFace[]> { }
                    : BoundaryFaces.Where(f => f.Boundary != null)
                           .GroupBy(f => f.Boundary)
                           .ToDictionary(g => g.First().Boundary!, g => g.ToArray());
            }
            _MapBoundaryToFaces.TryGetValue(boundary, out BoundaryFace[]? faces);
            return faces;
        }

        public TetrahedralMesh ToOperationSpecificMesh(string operationIdentifier)
        {
            var mapOldVolumeToNewVolume = Volumes.Entries.Select(v =>
            new
            {
                oldVolume = v,
                newVolume = typeof(MultipleOperationVolume).IsAssignableFrom(v.GetType())
                ? ((MultipleOperationVolume)v).GetByOperationIdentifierAllowNull(operationIdentifier)
                : v
            })
            .Where(v => v.newVolume != null)
            .ToDictionary(v => v.oldVolume, v => v.newVolume!);

            Func<Boundary, Boundary?> getNewBoundaryFromOld = (oldBoundary) =>
            {
                if (oldBoundary.BoundaryConditionType.Equals(BoundaryConditionType.OperationSpecific))
                {
                    return ((MultipleOperationBoundary)oldBoundary).GetByOperationIdentifier(operationIdentifier);
                }
                return oldBoundary;
            };
            Func<Volume, Volume?> getNewVolumeFromOld = (oldVolume) =>
            {
                mapOldVolumeToNewVolume.TryGetValue(oldVolume, out Volume? volume);
                return volume;
            };

            var mapOldNodeToNewNode = new Dictionary<TetrahedralNode, TetrahedralNode>();
            Func<TetrahedralNode, TetrahedralNode> getNewNodeFromOld = (oldNode) =>
            {
                if (mapOldNodeToNewNode.TryGetValue(oldNode, out TetrahedralNode? newNode))
                {
                    return newNode;
                }
                newNode = new TetrahedralNode(oldNode.Identifier, oldNode.X, oldNode.Y, oldNode.Z, oldNode.Attributes);
                mapOldNodeToNewNode[oldNode] = newNode;
                return newNode;
            };

            var mapOldElementToNewElement = new Dictionary<TetrahedralElement, TetrahedralElement>();
            var newElements = new HashSet<TetrahedralElement>();
            foreach(TetrahedralElement oldElement in Elements.Cast<TetrahedralElement>()) {
                Volume? newVolume = getNewVolumeFromOld(oldElement.VolumeBelongsTo);
                if (newVolume == null) continue;
                TetrahedralElement newElement = new TetrahedralElement(
                    oldElement.Identifier,
                    oldElement.Nodes.Cast<TetrahedralNode>().Select(getNewNodeFromOld).ToArray(),
                    newVolume!);
                mapOldElementToNewElement[oldElement] = newElement;
                newElements.Add(newElement);
            }
            var newBoundaryFaces = new List<BoundaryFace>();
            foreach (BoundaryFace oldBoundaryFace in BoundaryFaces)
            {
                Boundary? newBoundary = getNewBoundaryFromOld(oldBoundaryFace.Boundary);
                if (newBoundary == null) continue;
                TetrahedralElement[] newElementsForBoundary = oldBoundaryFace
                    .Elements
                    .Cast<TetrahedralElement>()
                    .Select(oldElement =>
                    {
                        mapOldElementToNewElement.TryGetValue(oldElement, out TetrahedralElement? newElementForBoundary);
                        return newElementForBoundary;
                    }).Where(n => n != null)
                    .Select(e=>e!)
                    .ToArray();
                if (!newElementsForBoundary.Any()) continue;
                BoundaryFace newBoundaryFace = new BoundaryFace(
                    oldBoundaryFace.Nodes.Cast<TetrahedralNode>().Select(getNewNodeFromOld).ToArray(),
                    newBoundary!,
                    newElementsForBoundary
                );
                newBoundaryFaces.Add(newBoundaryFace);
            }
            BoundariesCollection newBoundaries = new BoundariesCollection(
                newBoundaryFaces.Select(f=>f.Boundary).GroupBy(b=>b).Select(g=>g.First()).ToArray());
            VolumesCollection newVolumes = new VolumesCollection(mapOldVolumeToNewVolume.Values.ToArray());

            return new TetrahedralMesh(newBoundaries, newVolumes, mapOldNodeToNewNode.Values.ToHashSet(), newBoundaryFaces.ToHashSet(), 
                newElements, null);
        }
        public IMesh Clone() {
            return CloneT();
        }
        public TetrahedralMesh CloneT(
            Func<TetrahedralNode, TetrahedralNode>? createNewNode = null,
            Func<TetrahedralElement, TetrahedralElement>? createNewElement = null)
        {
            if (createNewNode == null) createNewNode = (TetrahedralNode n) => new TetrahedralNode(n.Identifier, n.X, n.Y, n.Z, n.Attributes);
            var mapOldNodeToNewNode = new Dictionary<TetrahedralNode, TetrahedralNode>();
            foreach (var oldNode in Nodes.Cast<TetrahedralNode>()) {
                TetrahedralNode newNode = createNewNode(oldNode);
                mapOldNodeToNewNode[oldNode] = newNode;
            }
            Func<TetrahedralNode, TetrahedralNode> getNewNodeFromOld = (oldNode) =>mapOldNodeToNewNode[oldNode];
            if (createNewElement == null) createNewElement = (TetrahedralElement e) => new TetrahedralElement(e.Identifier, e.Nodes.Cast<TetrahedralNode>().Select(getNewNodeFromOld).ToArray(), e.VolumeBelongsTo);
            var newElements = new HashSet<TetrahedralElement>();
            var mapOldElementToNewElement = new Dictionary<TetrahedralElement, TetrahedralElement>();
            foreach (var oldElement in Elements.Cast<TetrahedralElement>()) {
                TetrahedralElement newElement = createNewElement(oldElement);
                mapOldElementToNewElement[oldElement] = newElement;
                newElements.Add(newElement);
            }
            Func<TetrahedralElement, TetrahedralElement> getNewElementFromOld = (oldElement)=>mapOldElementToNewElement[oldElement];

            HashSet<BoundaryFace> newBoundaryFaces = BoundaryFaces
                .Select(b => new BoundaryFace(
                    b.Nodes.Cast<TetrahedralNode>().Select(getNewNodeFromOld).ToArray(),
                    b.Boundary,
                    b.Elements.Cast<TetrahedralElement>().Select(getNewElementFromOld).ToArray()
                 ))
                .ToHashSet();

            return new TetrahedralMesh(Boundaries, Volumes, mapOldNodeToNewNode.Values.ToHashSet(), newBoundaryFaces, newElements, null); ;
        }

        public TetrahedralMesh(BoundariesCollection boundaries, VolumesCollection volumes,
            IReadOnlySet<TetrahedralNode> tetrahedralNodes,
            IReadOnlySet<BoundaryFace> boundaryFaces, 
            IReadOnlySet<TetrahedralElement> tetrahedralElements, 
            BVH3D<TetrahedralElement>? elementsBVH)
        {
            Boundaries = boundaries;
            Volumes = volumes;
            TetrahedralNodes = tetrahedralNodes;
            BoundaryFaces = boundaryFaces;
            TetrahedralElements = tetrahedralElements;
            _ElementsBVHTree = elementsBVH;
        }
        private IReadOnlySet<NonBoundaryFace> LoadNonBoundaryFaces()
        {

            DictionaryDictionaryDictionary<int, TriangleFaceBase?> mapNodeIdentifiersIncreasingToFace
                = new DictionaryDictionaryDictionary<int, TriangleFaceBase?>();
            foreach (var face in BoundaryFaces)
            {
                int[] identifiers = face.NodeIdentifiersLowToHigh;
                mapNodeIdentifiersIncreasingToFace.Map(identifiers[0], identifiers[1], identifiers[2], null);
            }
            List<NonBoundaryFace> nonBoundaryFaces = new List<NonBoundaryFace>();
            foreach (IElement element in Elements)
            {
                TetrahedralElement tetrahedronElement = (TetrahedralElement)element;
                TetrahedralNode[] n = ((TetrahedralElement)element).NodesOrderedByIdentifiers;
                TetrahedralNode[][] elementFaces = new TetrahedralNode[][] {
                    new TetrahedralNode[]{n[0], n[1], n[2] },
                    new TetrahedralNode[]{n[0], n[1], n[3] },
                    new TetrahedralNode[]{n[0], n[2], n[3] },
                    new TetrahedralNode[]{n[1], n[2], n[3] }
                };
                foreach (TetrahedralNode[] elementFace in elementFaces)
                {
                    TetrahedralNode nodeA = elementFace[0];
                    TetrahedralNode nodeB = elementFace[1];
                    TetrahedralNode nodeC = elementFace[2];
                    if (mapNodeIdentifiersIncreasingToFace.TryGetValue(
                        nodeA.Identifier, nodeB.Identifier, nodeC.Identifier, out TriangleFaceBase? face))
                    {
                        if (face == null) continue;
                        face.AddElement(tetrahedronElement);
                    }
                    else
                    {
                        face = new NonBoundaryFace(elementFace, tetrahedronElement);
                        mapNodeIdentifiersIncreasingToFace.Map(nodeA.Identifier, nodeB.Identifier, nodeC.Identifier, face);
                        nonBoundaryFaces.Add((NonBoundaryFace)face);
                    }
                }
            }
            return nonBoundaryFaces.ToHashSet();
        }
        public void RotateNodes90DegreesAroundZ()
        {
            foreach (TetrahedralNode node in Nodes)
            {
                // Original coordinates
                double x = node.X;
                double y = node.Y;
                double z = node.Z;

                // Apply rotation matrix
                node.X = -y; // New x coordinate
                node.Y = x;  // New y coordinate
                node.Z = z;  // z coordinate remains unchanged
            }
        }

        public double GetThickness(double[] position)
        {
            return 1d;
        }
        private Dictionary<Boundary, IBoundaryPrimitive[]>? _MapBoundaryToPrimatives;
        public IReadOnlyList<IBoundaryPrimitive> GetPrimitivesForBoundary(Boundary boundary)
        {
            if (_MapBoundaryToPrimatives == null)
            {
                _MapBoundaryToPrimatives = BoundaryFaces
                    .GroupBy(e => e.Boundary)
                    .ToDictionary(
                        g => g.First().Boundary,
                        g => g.Select(b => (IBoundaryPrimitive)b).ToArray()
                    );
            }
            if (_MapBoundaryToPrimatives.TryGetValue(boundary, out IBoundaryPrimitive[]? primitives))
            {
                return primitives;
            }
            return new IBoundaryPrimitive[0];
        }
    }
}
