using Core.Collections;
using Core.Trees;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Mesh.Interfaces;
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
        private Dictionary<int, int>? _MapNodeToGlobalIndex;
        public Dictionary<int, int> MapNodeIndexToGlobalIndex
        {
            get
            {

                int nodeIndex = 0;
                if (_MapNodeToGlobalIndex == null)
                {
                    _MapNodeToGlobalIndex = Nodes.ToDictionary(n => n.Index, n => nodeIndex++);
                }
                return _MapNodeToGlobalIndex;
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

        private BVH<TetrahedronElement>? _ElementsBVHTree;
        public BVH<TetrahedronElement> ElementsBVHTree
        {
            get
            {
                if (_ElementsBVHTree == null)
                {
                    _ElementsBVHTree = new BVH<TetrahedronElement>(Elements.Cast<TetrahedronElement>().ToList(),
                        e => e.BoundingCuboid, (e, p) => e.IsPointInside(p));
                }
                return _ElementsBVHTree;
            }
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
                    if (Elements.GroupBy(e => e.Index).Where(g => g.Count() > 1).Any())
                    {

                    }
                    foreach (var element in Elements)
                    {
                        foreach (var node in element.Nodes)
                        {
                            if (!_MapNodeToElementsBelongsTo.ContainsKey(node.Index))
                            {
                                _MapNodeToElementsBelongsTo[node.Index] = new List<IElement>();
                            }
                            _MapNodeToElementsBelongsTo[node.Index].Add(element);
                        }
                    }
                }
                return _MapNodeToElementsBelongsTo;
            }
        }
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
            return GetFacesForBoundary(boundary)?.SelectMany(b => b.Nodes).GroupBy(n => n).Select(g => g.First()).ToArray();
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

            Func<string, Boundary, Boundary> getNewBoundaryFromOld = (operationIdentifier, oldBoundary) =>
            {
                if (oldBoundary.BoundaryConditionType.Equals(BoundaryConditionType.OperationSpecific))
                {
                    return ((MultipleOperationBoundary)oldBoundary).GetByOperationIdentifier(operationIdentifier);
                }
                return oldBoundary;
            };

            Dictionary<Node, Node> mapOldNodeToNewNode = new Dictionary<Node, Node>();
            Func<Node, Node> getNewNodeFromOld = (oldNode) =>
            {
                if (mapOldNodeToNewNode.TryGetValue(oldNode, out Node? newNode))
                {
                    return newNode;
                }
                newNode = new Node(oldNode.Index, oldNode.X, oldNode.Y, oldNode.Z, oldNode.Attributes);
                mapOldNodeToNewNode[oldNode] = newNode;
                return newNode;
            };

            Dictionary<TetrahedronElement, TetrahedronElement> mapOldElementToNewElement = Elements
                .Where(e => mapOldVolumeToNewVolume.ContainsKey(e.VolumeBelongsTo))
                .Cast<TetrahedronElement>()
                .ToDictionary(e => e, e => new TetrahedronElement(
                    e.Index,
                    e.Nodes.Cast<Node>().Select(getNewNodeFromOld).ToArray(),
                    mapOldVolumeToNewVolume[e.VolumeBelongsTo])
                );

            var newNodes = mapOldNodeToNewNode.Values.ToArray();
            var newBoundaryFaces = BoundaryFaces
                .Select(f =>
                    new
                    {
                        face = f,
                        newElementsForBoundaryFace = f.Elements
                            .Where(e => mapOldElementToNewElement.ContainsKey(e))
                            .Select(e => mapOldElementToNewElement[e]).ToArray()
                    })
                .Where(o => o.newElementsForBoundaryFace.Any())
                .Select(o => new BoundaryFace(
                    o.face.Marker,
                    o.face.Nodes.Select(getNewNodeFromOld).ToArray(),
                    getNewBoundaryFromOld(operationIdentifier, o.face.Boundary),
                    o.newElementsForBoundaryFace))
                .ToArray();

            var newElements = mapOldElementToNewElement.Values.ToArray();
            BoundariesCollection newBoundaries = new BoundariesCollection(
                newBoundaryFaces.Select(f=>f.Boundary).GroupBy(b=>b).Select(g=>g.First()).ToArray());
            VolumesCollection newVolumes = new VolumesCollection(mapOldVolumeToNewVolume.Values.ToArray());

            return new TetrahedralMesh(newBoundaries, newVolumes, newNodes, newBoundaryFaces, newElements, null);
        }

        public TetrahedralMesh(BoundariesCollection boundaries, VolumesCollection volumes, Node[] nodes,
            BoundaryFace[] boundaryFaces, TetrahedronElement[] elements, BVH<TetrahedronElement>? elementsBVH)
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
                        nodeA.Index, nodeB.Index, nodeC.Index, out TriangleFaceBase? face))
                    {
                        if (face == null) continue;
                        face.AddElement(tetrahedronElement);
                    }
                    else
                    {
                        face = new NonBoundaryFace(elementFace, tetrahedronElement);
                        mapNodeIdentifiersIncreasingToFace.Map(nodeA.Index, nodeB.Index, nodeC.Index, face);
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
