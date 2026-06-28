using Core.Geometry;
using Core.Maths.Tensors;
namespace FiniteElementAnalysis.Mesh.Polyhedral
{

    public class PolyhedralNode : Vector3D
    {
        private List<PolyhedralPolygon>? _PolygonsBelongsTo = null;
        public List<PolyhedralPolygon>? PolygonsBelongsTo { get { return _PolygonsBelongsTo; } }
        public int Index { get; set; }
        public PolyhedralNode(double x, double y, double z, PolyhedralDomain domain)
            : base(x, y, z)
        {
            domain.Add(this);
        }
        public void AddBelongsTo(PolyhedralPolygon polygons)
        {
            if (_PolygonsBelongsTo == null)
                _PolygonsBelongsTo = new List<PolyhedralPolygon>();
            _PolygonsBelongsTo.Add(polygons);
        }
    }
}