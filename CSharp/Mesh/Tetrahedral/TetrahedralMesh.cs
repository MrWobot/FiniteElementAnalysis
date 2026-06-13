using Core.Collections;
using Core.Trees;
using FiniteElementAnalysis.Boundaries;

namespace FiniteElementAnalysis.Mesh.Tetrahedral
{
    public class TetrahedralMesh
    {
        public Node[] Nodes { get; }
        public bool IsPartOfResult { get; set; }
        public BoundaryFace[] BoundaryFaces { get; }
        private BoundaryFace[]? _NonBounndaryFaces;
        public BoundaryFace[] NonBoundaryFaces
        {
            get
            {
                if (_NonBounndaryFaces == null)
                {
                    _NonBounndaryFaces = LoadNonBoundaryFaces();
                }
                return _NonBounndaryFaces;
            }
        }
        public BoundaryFace[] AllFaces
        {
            get
            {
                BoundaryFace[] nonBoundaryFaces = NonBoundaryFaces;
                BoundaryFace[] allFaces = new BoundaryFace[BoundaryFaces.Length + nonBoundaryFaces.Length];
                Array.Copy(BoundaryFaces, 0, allFaces, 0, BoundaryFaces.Length);
                Array.Copy(nonBoundaryFaces, 0, allFaces, BoundaryFaces.Length, nonBoundaryFaces.Length);
                return allFaces;
            }
        }
        public TetrahedronElement[] Elements { get; }
        public BoundariesCollection Boundaries { get; }
        public VolumesCollection Volumes { get; }
        public bool HasBoundaries { get { return Boundaries.HasEntries; } }

        private Dictionary<Boundary, BoundaryFace[]>? _MapBoundaryToFaces;
        private Dictionary<Boundary, BoundaryFace[]> MapBoundaryToFaces
        {
            get
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
                return _MapBoundaryToFaces;
            }
        }

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
                        _MapNodesToElement.Map(element.NodeIdentifiersLowToHigh, element);
                    }
                }
                return _MapNodesToElement;
            }
        }
        private Dictionary<int, TetrahedronElement>? _MapElementIdentifierToElement;
        public Dictionary<int, TetrahedronElement> MapElementIdentifierToElement
        {
            get
            {
                if (_MapElementIdentifierToElement == null)
                {
                    _MapElementIdentifierToElement = Elements.ToDictionary(e => e.Identifier, e => e);
                }
                return _MapElementIdentifierToElement;
            }
        }
        private Dictionary<int, int>? _MapNodeToGlobalIndex;
        public Dictionary<int, int> MapNodeIdentifierToGlobalIndex
        {
            get
            {

                int nodeIndex = 0;
                if (_MapNodeToGlobalIndex == null)
                {
                    _MapNodeToGlobalIndex = Nodes.ToDictionary(n => n.Identifier, n => nodeIndex++);
                }
                return _MapNodeToGlobalIndex;
            }
        }
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
        }

        private BVH<TetrahedronElement>? _ElementsBVHTree;
        public BVH<TetrahedronElement> ElementsBVHTree
        {
            get
            {
                if (_ElementsBVHTree == null)
                {
                    _ElementsBVHTree = new BVH<TetrahedronElement>(Elements.ToList(),
                        e => e.BoundingCuboid, (e, p) => e.IsPointInside(p));
                }
                return _ElementsBVHTree;
            }
        }

        // New property for lazy loading of the map of nodes to elements they belong to
        private Dictionary<int, List<TetrahedronElement>>? _MapNodeToElementsBelongsTo;
        public Dictionary<int, List<TetrahedronElement>> MapNodeToElementsBelongsTo
        {
            get
            {
                if (_MapNodeToElementsBelongsTo == null)
                {
                    _MapNodeToElementsBelongsTo = new Dictionary<int, List<TetrahedronElement>>();
                    if (Elements.GroupBy(e => e.Identifier).Where(g => g.Count() > 1).Any())
                    {

                    }
                    foreach (var element in Elements)
                    {
                        foreach (var node in element.Nodes)
                        {
                            if (!_MapNodeToElementsBelongsTo.ContainsKey(node.Identifier))
                            {
                                _MapNodeToElementsBelongsTo[node.Identifier] = new List<TetrahedronElement>();
                            }
                            _MapNodeToElementsBelongsTo[node.Identifier].Add(element);
                        }
                    }
                }
                return _MapNodeToElementsBelongsTo;
            }
        }
        public Node[] GetNeighbouringNodes(Node node)
        {
            List<TetrahedronElement> elementsBelongsTo = MapNodeToElementsBelongsTo[node.Identifier];
            return elementsBelongsTo
                .SelectMany(e => e.Nodes)
                .GroupBy(n => n.Identifier)
                .Select(g => g.First())
                .Where(n => n.Identifier != node.Identifier)
                .ToArray();
        }

        public bool HasNonLinearBoundaries()
        {
            return Boundaries
                .Entries
                .Where(b => b != null && b.IsNonLinear)
                .Where(b => GetFacesForBoundary(b) != null || GetNodesForBoundary(b) != null).Any();
        }

        public Node[]? GetNodesForBoundary(Boundary boundary)
        {
            MapBoundaryToNodes.TryGetValue(boundary, out Node[]? nodes);
            return nodes;
        }

        public BoundaryFace[]? GetFacesForBoundary(Boundary boundary)
        {
            MapBoundaryToFaces.TryGetValue(boundary, out BoundaryFace[]? faces);
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

            Func<string, Boundary?, Boundary?> getNewBoundaryFromOld = (operationIdentifier, oldBoundary) =>
            {
                if (oldBoundary == null) return null;
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
                newNode = new Node(oldNode.Identifier, oldNode.X, oldNode.Y, oldNode.Z, oldNode.Attributes,
                    getNewBoundaryFromOld(operationIdentifier, oldNode.Boundary));
                mapOldNodeToNewNode[oldNode] = newNode;
                return newNode;
            };

            var mapOldElementToNewElement = Elements
                .Where(e => mapOldVolumeToNewVolume.ContainsKey(e.VolumeIsAPartOf))
                .ToDictionary(e => e, e => new TetrahedronElement(
                    e.Identifier,
                    e.Nodes.Select(getNewNodeFromOld).ToArray(),
                    mapOldVolumeToNewVolume[e.VolumeIsAPartOf])
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
            var withBoundary = newNodes.Where(n => n.Boundary != null).ToArray();
            var newBoundariesArray = newNodes.Where(n => n.Boundary != null)
                .GroupBy(n => n.Boundary).Select(g => g.First().Boundary!)
                .Concat(
                    newBoundaryFaces
                    .GroupBy(f => f.Boundary)
                    .Select(g => g.First().Boundary)
                )
                .GroupBy(b => b)
                .Select(g => g.First())
                .ToArray();

            BoundariesCollection newBoundaries = new BoundariesCollection(newBoundariesArray);
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
        private BoundaryFace[] LoadNonBoundaryFaces()
        {

            DictionaryDictionaryDictionary<int, BoundaryFace> mapNodeIdentifiersIncreasingToFace
                = new DictionaryDictionaryDictionary<int, BoundaryFace>();
            foreach (var face in BoundaryFaces)
            {
                int[] identifiers = face.NodeIdentifiersLowToHigh;
                mapNodeIdentifiersIncreasingToFace.Map(identifiers[0], identifiers[1], identifiers[2], null);
            }
            List<BoundaryFace> nonBoundaryFaces = new List<BoundaryFace>();
            foreach (var element in Elements)
            {
                Node[] n = element.NodesOrderedByIdentifiers;
                Node[][] elementFaces = new Node[][] {
                    new Node[]{n[0], n[1], n[2] },
                    new Node[]{n[0], n[1], n[3] },
                    new Node[]{n[0], n[2], n[3] },
                    new Node[]{n[1], n[2], n[3] }
                };
                foreach (var elementFace in elementFaces)
                {
                    Node nodeA = elementFace[0];
                    Node nodeB = elementFace[1];
                    Node nodeC = elementFace[2];
                    if (mapNodeIdentifiersIncreasingToFace.TryGetValue(
                        nodeA.Identifier, nodeB.Identifier, nodeC.Identifier, out BoundaryFace? face))
                    {
                        if (face == null) continue;
                        face.AddElement(element);
                    }
                    else
                    {
                        face = new BoundaryFace(-1, elementFace, null, element);
                        mapNodeIdentifiersIncreasingToFace.Map(nodeA.Identifier, nodeB.Identifier, nodeC.Identifier, face);
                        nonBoundaryFaces.Add(face);
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
    }
}
