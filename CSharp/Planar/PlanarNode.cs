using Core.Maths.Tensors;
namespace FiniteElementAnalysis.Polyhedrals
{

    public class PlanarNode : Vector2D
    {
        private List<PlanarSegment>? _SegmentsBelongsTo = null;
        public List<PlanarSegment>? SegmentsBelongsTo { get { return _SegmentsBelongsTo; } }
        public int Index { get; }
        public PlanarNode(double x, double y, int index)
            : base(x, y)
        {
            Index = index;
        }
        public void AddBelongsTo(PlanarSegment segment)
        {
            if (_SegmentsBelongsTo == null)
                _SegmentsBelongsTo = new List<PlanarSegment>();
            _SegmentsBelongsTo.Add(segment);
        }
    }
}