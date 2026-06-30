using Core.Maths.Tensors;
using FiniteElementAnalysis.Mesh.Interfaces;
namespace FiniteElementAnalysis.Mesh.Planar
{

    public class PlanarNode : Vector2D, INode
    {
        private List<PlanarSegment>? _SegmentsBelongsTo = null;
        public List<PlanarSegment>? SegmentsBelongsTo { get { return _SegmentsBelongsTo; } }
        public int Identifier { get; }

        public double[] Position => ToArray();

        public double[]? Values { get; set; }

        public PlanarNode(double x, double y, int index)
            : base(x, y)
        {
            Identifier = index;
        }
        public void AddBelongsTo(PlanarSegment segment)
        {
            if (_SegmentsBelongsTo == null)
                _SegmentsBelongsTo = new List<PlanarSegment>();
            _SegmentsBelongsTo.Add(segment);
        }
    }
}