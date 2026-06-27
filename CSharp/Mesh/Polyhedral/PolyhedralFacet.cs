using FiniteElementAnalysis.Boundaries;

namespace FiniteElementAnalysis.Mesh.Polyhedral
{

    public class PolyhedralFacet
    {
        public PolyhedralPolygon[] Polygons { get; }
        public int BoundaryMarker { get; }
        public PolyhedralFacet(PolyhedralDomain domain, int boundaryMarker, params PolyhedralPolygon[] polygons)
        {
            Polygons = polygons;
            BoundaryMarker = boundaryMarker;
            domain.Add(this);
            foreach (PolyhedralPolygon polygon in polygons)
            {
                polygon.SetBelongsTo(this);
            }
        }
    }
}