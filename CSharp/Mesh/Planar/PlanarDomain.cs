using Core.Geometry;
using Core.Graphics;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Mesh.Interfaces;
using FiniteElementAnalysis.Mesh.Parsing.MtlFiles;
using FiniteElementAnalysis.Mesh.Planar.Thickness;
namespace FiniteElementAnalysis.Mesh.Planar
{

    public class PlanarDomain:IMesh
    {
        public BoundariesCollection Boundaries { get; }
        public VolumesCollection Volumes { get; }
        public INode[] Nodes { get; }
        public IElement[] Elements { get; }
        public IBoundaryPrimitive[] Edges { get; }
        private PlanarThicknessSourceBase _ThicknessSource;
        public PlanarDomain(BoundariesCollection boundaries, VolumesCollection volumes,
            PlanarThicknessSourceBase thicknessSource,
            PlanarNode[] nodes, PlanarSegment[] segments, PlanarEdge[] edges)
        {
            Boundaries = boundaries;
            Volumes = volumes;
            _ThicknessSource = thicknessSource;
            Nodes = nodes;
            Edges = edges;
            Elements = segments;
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
                PlanarNode firstNode = (PlanarNode)Nodes[0];
                xMin = firstNode.X;
                xMax = xMin;
                yMin = firstNode.Y;
                yMax = yMin;
                for (int i = 1; i < Nodes.Length; i++)
                {
                    var node = (PlanarNode)Nodes[i];
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

        public bool IsPartOfResult { get; set; }

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
            return _ThicknessSource.GetThickness(position);
        }

        public bool HasNonLinearBoundaries()
        {
            return Boundaries
                .Entries
                .Where(b => b != null && b.IsNonLinear)
                .Where(b => GetPrimitivesForBoundary(b).Any()).Any();
        }
        private Dictionary<Boundary, IBoundaryPrimitive[]>? _MapBoundaryToPrimatives;

        public IBoundaryPrimitive[] GetPrimitivesForBoundary(Boundary boundary)
        {
            if (_MapBoundaryToPrimatives == null)
            {
                _MapBoundaryToPrimatives = Edges
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
    }
}