using Core.Graphics;
using Core.Maths.Tensors;
using FiniteElementAnalysis.Boundaries;

namespace FiniteElementAnalysis.Polyhedrals
{

    public class PlanarEdge
    {
        public PlanarSegment[] Segments{ get; }
        public Boundary Boundary { get; }
        public PlanarNode Node1 { get; }
        public PlanarNode Node2 { get; }
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
            foreach (PlanarSegment segment in segments) {
                segment.SetBelongsTo(this);
            }
        }
    }
}