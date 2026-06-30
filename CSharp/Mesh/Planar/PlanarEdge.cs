using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Mesh.Interfaces;

namespace FiniteElementAnalysis.Mesh.Planar
{

    public class PlanarEdge : IBoundaryPrimitive
    {
        public PlanarSegment[] Segments { get; }
        public Boundary Boundary { get; }
        public PlanarNode Node1 { get; }
        public PlanarNode Node2 { get; }

        public INode[] Nodes { get; }

        public IElement[] Elements { get; }
        private double? _Measure;
        public double Measure
        {
            get
            {
                if (_Measure == null)
                {
                    _Measure = Math.Sqrt(Math.Pow(Node2.X - Node1.X, 2) + Math.Pow(Node2.Y - Node1.Y, 2));
                }
                return (double)_Measure;
            }
        }
        private double[]? _UnitNormal;
        public double[] UnitNormal
        {
            get
            {
                if (_UnitNormal == null)
                {
                    double dx = Node2.X - Node1.X;
                    double dy = Node2.Y - Node1.Y;
                    double length = Measure;
                    _UnitNormal = new double[] { -dy / length, dx / length };
                }
                return _UnitNormal;
            }
        }

        public double[] Centre =>[(Node1.X+Node2.X)/2, (Node1.Y+Node2.Y)/2];

        public PlanarEdge(
            PlanarNode node1,
            PlanarNode node2,
            Boundary boundary,
            params PlanarSegment[] segments)
        {
            Node1 = node1;
            Node2 = node2;
            Segments = segments;
            Boundary = boundary;
            Nodes = new INode[] { node1, node2 };
            Elements = segments.Cast<IElement>().ToArray();
            /*foreach (PlanarSegment segment in segments)
            {
                segment.SetBelongsTo(this);
            }*/
        }
    }
}