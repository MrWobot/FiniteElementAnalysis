using Core.Geometry;
using Core.Graphics;
using Core.Maths.Tensors;
using Core.Trees;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Mesh.Interfaces;
using FiniteElementAnalysis.Mesh.Parsing.MtlFiles;
using FiniteElementAnalysis.Mesh.Planar.Thickness;
using FiniteElementAnalysis.Mesh.Tetrahedral;

namespace FiniteElementAnalysis.Mesh.Planar
{

    public class PlanarDomain:IMesh
    {
        public BoundariesCollection Boundaries { get; }
        public VolumesCollection Volumes { get; }
        public IReadOnlySet<PlanarNode> PlanarNodes { get; }
        private IReadOnlySet<INode>? _Nodes;
        public IReadOnlySet<INode> Nodes { 
            get {
                if (_Nodes == null) {
                    _Nodes = new HashSet<INode>(PlanarNodes);
                }
                return _Nodes;
            } 
        }
        public IReadOnlySet<PlanarSegment> PlanarSegments { get; }
        private IReadOnlySet<IElement>? _Elements;
        public IReadOnlySet<IElement> Elements { 
            get {
                if (_Elements == null) {
                    _Elements = new HashSet<IElement>(PlanarSegments);
                }
                return _Elements;
            } 
        }
        public IReadOnlySet<PlanarEdge> PlanarEdges { get; }
        public PlanarThicknessSourceBase ThicknessSource { get; }
        public PlanarDomain(
            BoundariesCollection boundaries, 
            VolumesCollection volumes,
            PlanarThicknessSourceBase thicknessSource,
            IReadOnlySet<PlanarNode> planarNodes, 
            IReadOnlySet<PlanarSegment> planarSegments,
            IReadOnlySet<PlanarEdge> planarEdges)
        {
            Boundaries = boundaries;
            Volumes = volumes;
            ThicknessSource = thicknessSource;
            PlanarNodes = planarNodes;
            PlanarEdges = planarEdges;
            PlanarSegments= planarSegments;
            ValidateDomainComplete();
        }
        private void ValidateDomainComplete()
        {
            //TODO be done later i know it works
        }
        /*
        public void CheckForNodesTooCloseTogether(double distance = 0.0001)
        {
            for (int nodeIndex = 0; nodeIndex < Nodes.Length; nodeIndex++)
            {
                PlanarNode node =(PlanarNode)Nodes[nodeIndex];
                for (int otherNodeIndex = nodeIndex + 1; otherNodeIndex < Nodes.Length; otherNodeIndex++)
                {

                    PlanarNode otherNode = (PlanarNode)Nodes[otherNodeIndex];
                    double magnitude = (node - otherNode).Magnitude();
                    if (magnitude < distance)
                    {

                    }
                }
            }
        }*/
        private Rectangle? _Rectangle;
        public Rectangle Rectangle
        {
            get
            {
                if (_Rectangle != null) return _Rectangle;
                double xMin;
                double xMax;
                double yMin;
                double yMax;
                PlanarNode firstNode = PlanarNodes.First();
                xMin = firstNode.X;
                xMax = xMin;
                yMin = firstNode.Y;
                yMax = yMin;
                foreach (PlanarNode node in PlanarNodes)
                {
                    double x = node.X;
                    double y = node.Y;
                    if (xMin > x)
                    {
                        xMin = x;
                    }
                    if (xMax < x)
                    {
                        xMax = x;
                    }
                    if (yMin > y)
                    {
                        yMin = y;
                    }
                    if (yMax < y)
                    {
                        yMax = y;
                    }
                }
                _Rectangle = new Rectangle(xFrom: xMin, yFrom: yMin, xTo: xMax, yTo: yMax);
                return _Rectangle;
            }
        }

        public int NNodesPerElement => 3;


        private Dictionary<int, int>? _MapNodeIdentifierToGlobalIndex;

        public int GetGlobalIndexForNode(int nodeIdentifier)
        {
                int nodeIndex = 0;
                if (_MapNodeIdentifierToGlobalIndex == null)
                {
                    _MapNodeIdentifierToGlobalIndex = Nodes.ToDictionary(n => n.Identifier, n => nodeIndex++);
                }
                return _MapNodeIdentifierToGlobalIndex[nodeIdentifier];
        }

        private Dictionary<int, HashSet<IElement>>? _MapNodeToElementsBelongsTo;
        public IReadOnlySet<IElement> GetElementsThatNodeBelongsTo(int nodeIdentifier)
        {
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

        public bool IsPartOfResult { get; set; }

        public int NodePositionLength => 2;

        public void ApplyColours(MtlFile file)
        {

            var mapMaterialNameToRGBF = file.Materials.ToDictionary(m => m.Name, m => m.Kd);
            foreach (var volume in Volumes.Entries)
            {

                if (mapMaterialNameToRGBF.TryGetValue(volume.Name, out RGBF? color) && color != null)
                {
                    volume.Color = color;
                }
            }
            foreach (var boundary in Boundaries.Entries)
            {

                if (mapMaterialNameToRGBF.TryGetValue(boundary.Name, out RGBF? color) && color != null)
                {
                    boundary.Color = color;
                }
            }
        }

        public double GetThickness(double[] position)
        {
            return ThicknessSource.GetThickness(position);
        }

        public bool HasNonLinearBoundaries()
        {
            return Boundaries
                .Entries
                .Where(b => b != null && b.IsNonLinear)
                .Where(b => GetPrimitivesForBoundary(b).Any()).Any();
        }
        private Dictionary<Boundary, IBoundaryPrimitive[]>? _MapBoundaryToPrimatives;

        public IReadOnlyList<IBoundaryPrimitive> GetPrimitivesForBoundary(Boundary boundary)
        {
            if (_MapBoundaryToPrimatives == null)
            {
                _MapBoundaryToPrimatives = PlanarEdges
                    .GroupBy(e => e.Boundary)
                    .ToDictionary(
                        g => g.First().Boundary, 
                        g => g.Select(b => (IBoundaryPrimitive)b).ToArray()
                    );
            }
            if (_MapBoundaryToPrimatives.TryGetValue(boundary, out IBoundaryPrimitive[]? primitives)) { 
                return primitives;
            }
            return new IBoundaryPrimitive[0];
        }
        public IMesh Clone()
        {
            var createNewNode = (PlanarNode oldNode) => new PlanarNode(oldNode.X, oldNode.Y, oldNode.Identifier);
            var mapOldNodeToNewNode = new Dictionary<PlanarNode, PlanarNode>();
            foreach (var oldNode in Nodes.Cast<PlanarNode>())
            {
                PlanarNode newNode = createNewNode(oldNode);
                mapOldNodeToNewNode[oldNode] = newNode;
            }
            var getNewNodeFromOld = (INode oldNode) => mapOldNodeToNewNode[(PlanarNode)oldNode];
            var mapOldElementToNewElement = new Dictionary<PlanarSegment, PlanarSegment>();
            foreach (var oldElement in Elements.Cast<PlanarSegment>())
            {
                PlanarSegment newElement = new PlanarSegment(
                    oldElement.Nodes.Select(getNewNodeFromOld).ToArray(),
                    oldElement.Identifier,
                    oldElement.VolumeBelongsTo);
                mapOldElementToNewElement[oldElement] = newElement;
            }
            var getNewElementFromOld = (IElement oldElement) => mapOldElementToNewElement[(PlanarSegment)oldElement];
            HashSet<PlanarEdge> newBoundaryEdges = PlanarEdges
                .Select(b => new PlanarEdge(
                    b.Node1,
                    b.Node2,
                    b.Boundary,
                    b.Elements.Select(getNewElementFromOld).ToArray()
                ))
                .ToHashSet();
            return new PlanarDomain(Boundaries, Volumes, ThicknessSource, mapOldNodeToNewNode.Values.ToHashSet(), mapOldElementToNewElement.Values.ToHashSet(), newBoundaryEdges);
        }

        private BVH2D<PlanarSegment>? _ElementsBVHTree;
        public BVH2D<PlanarSegment> ElementsBVHTree
        {
            get
            {
                if (_ElementsBVHTree == null)
                {
                    _ElementsBVHTree = new BVH2D<PlanarSegment>(Elements.Cast<PlanarSegment>().ToList(),
                        e => e.BoundingRectangle, (e, p) => e.IsPointInside(p.ToArray()));
                }
                return _ElementsBVHTree;
            }
        }
        public IEnumerable<IElement> GetElementsContainingPoint(double[] point)
        {
            return ElementsBVHTree.QueryBVH(new Vector2D(point[0], point[1]));
        }
    }
}