using FiniteElementAnalysis.Boundaries;

namespace FiniteElementAnalysis.Polyhedrals
{

    public class PlanarEdge
    {
        public PlanarSegment[] Segments{ get; }
        public int BoundaryMarker { get; }
        public Boundary Boundary { get; }
        public PlanarEdge(
            PlanarDomain domain,
            int boundaryMarker,
            Boundary boundary, 
            params PlanarSegment[] segments)
        {
            Segments = segments;
            BoundaryMarker = boundaryMarker;
            Boundary = boundary;
            domain.Add(this);
            foreach (PlanarSegment segment in segments) {
                segment.SetBelongsTo(this);
            }
        }
    }
}