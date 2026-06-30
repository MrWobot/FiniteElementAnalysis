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
        public INode[] Nodes { get; }
        public IElement[] Elements { get; }
        public int NNodesPerElement => 4;
        public bool IsPartOfResult { get; set; }
        public BoundaryFace[] BoundaryFaces { get; }
        private NonBoundaryFace[]? _NonBoundaryFaces;
        public NonBoundaryFace[] NonBoundaryFaces
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
        public TriangleFaceBase[] AllFaces
        {
            get
            {
                NonBoundaryFace[] nonBoundaryFaces = NonBoundaryFaces;
                TriangleFaceBase[] allFaces = new TriangleFaceBase[BoundaryFaces.Length + nonBoundaryFaces.Length];
                Array.Copy(BoundaryFaces, 0, allFaces, 0, BoundaryFaces.Length);
                Array.Copy(nonBoundaryFaces, 0, allFaces, BoundaryFaces.Length, nonBoundaryFaces.Length);
                return allFaces;
            }
        }
        public bool HasBoundaries { get { return Boundaries.HasEntries; } }

        private Dictionary<Boundary, BoundaryFace[]>? _MapBoundaryToFaces;

        private DictionaryDictionaryDictionaryDictionary<int, TetrahedronElement>? _MapNodesToElement;
        public DictionaryDictionaryDictionaryDictionary<int, TetrahedronElement> MapNodesToElement
        {
            get
            {
                if (_MapNodesToElement == null)
                {
                    _MapNodesToElement = new DictionaryDictionaryDictionaryDictionary<int, TetrahedronElement>();
                    foreach (var element in Elements)
                    {
                        TetrahedronElement tetrahedronElement = (TetrahedronElement)element;
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
        private Dictionary<int, int>? _MapNodeIdentifierToGlobalIndex;
        public Dictionary<int, int> MapNodeIdentifierToGlobalIndex
        {
            get
            {

                int nodeIndex = 0;
                if (_MapNodeIdentifierToGlobalIndex == null)
                {
                    _MapNodeIdentifierToGlobalIndex = Nodes.ToDictionary(n => n.Identifier, n => nodeIndex++);
                }
                return _MapNodeIdentifierToGlobalIndex;
            }
        }
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

        private BVH3D<TetrahedronElement>? _ElementsBVHTree;
        public BVH3D<TetrahedronElement> ElementsBVHTree
        {
            get
            {
                if (_ElementsBVHTree == null)
                {
                    _ElementsBVHTree = new BVH3D<TetrahedronElement>(Elements.Cast<TetrahedronElement>().ToList(),
                        e => e.BoundingCuboid, (e, p) => e.IsPointInside(p));
                }
                return _ElementsBVHTree;
            }
        }
        public IEnumerable<IElement> GetElementsContainingPoint(double[] point) {
            return ElementsBVHTree.QueryBVH(new Vector3D(point[0], point[1], point[2]));
        }

        // New property for lazy loading of the map of nodes to elements they belong to
        private Dictionary<int, List<IElement>>? _MapNodeToElementsBelongsTo;
        public Dictionary<int, List<IElement>> MapNodeToElementsBelongsTo
        {
            get
            {
                if (_MapNodeToElementsBelongsTo == null)
                {
                    _MapNodeToElementsBelongsTo = new Dictionary<int, List<IElement>>();
                    if (Elements.GroupBy(e => e.Identifier).Where(g => g.Count() > 1).Any())
                    {

                    }
                    foreach (var element in Elements)
                    {
                        foreach (var node in element.Nodes)
                        {
                            if (!_MapNodeToElementsBelongsTo.ContainsKey(node.Identifier))
                            {
                                _MapNodeToElementsBelongsTo[node.Identifier] = new List<IElement>();
                            }
                            _MapNodeToElementsBelongsTo[node.Identifier].Add(element);
                        }
                    }
                }
                return _MapNodeToElementsBelongsTo;
            }
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

        public Node[]? GetNodesForBoundary(Boundary boundary)
        {
            return GetFacesForBoundary(boundary)?.SelectMany(b => b.Nodes).GroupBy(n => n).Select(g => g.First()).Cast<Node>().ToArray();
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

            var mapOldNodeToNewNode = new Dictionary<Node, Node>();
            Func<Node, Node> getNewNodeFromOld = (oldNode) =>
            {
                if (mapOldNodeToNewNode.TryGetValue(oldNode, out Node? newNode))
                {
                    return newNode;
                }
                newNode = new Node(oldNode.Identifier, oldNode.X, oldNode.Y, oldNode.Z, oldNode.Attributes);
                mapOldNodeToNewNode[oldNode] = newNode;
                return newNode;
            };

            var mapOldElementToNewElement = new Dictionary<TetrahedronElement, TetrahedronElement>();
            var newElements = new List<TetrahedronElement>();
            foreach(TetrahedronElement oldElement in Elements.Cast<TetrahedronElement>()) {
                Volume? newVolume = getNewVolumeFromOld(oldElement.VolumeBelongsTo);
                if (newVolume == null) continue;
                TetrahedronElement newElement = new TetrahedronElement(
                    oldElement.Identifier,
                    oldElement.Nodes.Cast<Node>().Select(getNewNodeFromOld).ToArray(),
                    newVolume!);
                mapOldElementToNewElement[oldElement] = newElement;
                newElements.Add(newElement);
            }
            var newBoundaryFaces = new List<BoundaryFace>();
            foreach (BoundaryFace oldBoundaryFace in BoundaryFaces)
            {
                Boundary? newBoundary = getNewBoundaryFromOld(oldBoundaryFace.Boundary);
                if (newBoundary == null) continue;
                TetrahedronElement[] newElementsForBoundary = oldBoundaryFace
                    .Elements
                    .Cast<TetrahedronElement>()
                    .Select(oldElement =>
                    {
                        mapOldElementToNewElement.TryGetValue(oldElement, out TetrahedronElement? newElementForBoundary);
                        return newElementForBoundary;
                    }).Where(n => n != null)
                    .Select(e=>e!)
                    .ToArray();
                if (!newElementsForBoundary.Any()) continue;
                BoundaryFace newBoundaryFace = new BoundaryFace(
                    oldBoundaryFace.Nodes.Cast<Node>().Select(getNewNodeFromOld).ToArray(),
                    newBoundary!,
                    newElementsForBoundary
                );
                newBoundaryFaces.Add(newBoundaryFace);
            }
            BoundariesCollection newBoundaries = new BoundariesCollection(
                newBoundaryFaces.Select(f=>f.Boundary).GroupBy(b=>b).Select(g=>g.First()).ToArray());
            VolumesCollection newVolumes = new VolumesCollection(mapOldVolumeToNewVolume.Values.ToArray());

            return new TetrahedralMesh(newBoundaries, newVolumes, mapOldNodeToNewNode.Values.ToArray(), newBoundaryFaces.ToArray(), 
                newElements.ToArray(), null);
        }
        public IMesh Clone() {
            return CloneT();
        }
        public TetrahedralMesh CloneT(
            Func<Node, Node>? createNewNode = null,
            Func<TetrahedronElement, TetrahedronElement>? createNewElement = null)
        {
            if (createNewNode == null) createNewNode = (Node n) => new Node(n.Identifier, n.X, n.Y, n.Z, n.Attributes);
            var mapOldNodeToNewNode = new Dictionary<Node, Node>();
            var newNodes = new List<Node>();
            foreach (var oldNode in Nodes.Cast<Node>()) {
                Node newNode = createNewNode(oldNode);
                mapOldNodeToNewNode[oldNode] = newNode;
                newNodes.Add(newNode);
            }
            Func<Node, Node> getNewNodeFromOld = (oldNode) =>mapOldNodeToNewNode[oldNode];
            if (createNewElement == null) createNewElement = (TetrahedronElement e) => new TetrahedronElement(e.Identifier, e.Nodes.Cast<Node>().Select(getNewNodeFromOld).ToArray(), e.VolumeBelongsTo);
            var newElements = new List<TetrahedronElement>();
            var mapOldElementToNewElement = new Dictionary<TetrahedronElement, TetrahedronElement>();
            foreach (var oldElement in Elements.Cast<TetrahedronElement>()) {
                TetrahedronElement newElement = createNewElement(oldElement);
                mapOldElementToNewElement[oldElement] = newElement;
                newElements.Add(newElement);
            }
            Func<TetrahedronElement, TetrahedronElement> getNewElementFromOld = (oldElement)=>mapOldElementToNewElement[oldElement];

            BoundaryFace[] newBoundaryFaces = BoundaryFaces
                .Select(b => new BoundaryFace(
                    b.Nodes.Cast<Node>().Select(getNewNodeFromOld).ToArray(),
                    b.Boundary,
                    b.Elements.Cast<TetrahedronElement>().Select(getNewElementFromOld).ToArray()
                 ))
                .ToArray();

            return new TetrahedralMesh(Boundaries, Volumes, newNodes.ToArray(), newBoundaryFaces, newElements.ToArray(), null); ;
        }

        public TetrahedralMesh(BoundariesCollection boundaries, VolumesCollection volumes, Node[] nodes,
            BoundaryFace[] boundaryFaces, TetrahedronElement[] elements, BVH3D<TetrahedronElement>? elementsBVH)
        {
            Boundaries = boundaries;
            Volumes = volumes;
            Nodes = nodes;
            BoundaryFaces = boundaryFaces;
            Elements = elements;
            _ElementsBVHTree = elementsBVH;
        }
        private NonBoundaryFace[] LoadNonBoundaryFaces()
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
                TetrahedronElement tetrahedronElement = (TetrahedronElement)element;
                Node[] n = ((TetrahedronElement)element).NodesOrderedByIdentifiers;
                Node[][] elementFaces = new Node[][] {
                    new Node[]{n[0], n[1], n[2] },
                    new Node[]{n[0], n[1], n[3] },
                    new Node[]{n[0], n[2], n[3] },
                    new Node[]{n[1], n[2], n[3] }
                };
                foreach (Node[] elementFace in elementFaces)
                {
                    Node nodeA = elementFace[0];
                    Node nodeB = elementFace[1];
                    Node nodeC = elementFace[2];
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
            return nonBoundaryFaces.ToArray();
        }
        public void RotateNodes90DegreesAroundZ()
        {
            foreach (Node node in Nodes)
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
        public IBoundaryPrimitive[] GetPrimitivesForBoundary(Boundary boundary)
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
