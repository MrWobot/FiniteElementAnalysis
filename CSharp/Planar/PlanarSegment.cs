namespace FiniteElementAnalysis.Polyhedrals
{

    public class PlanarSegment
    {
        public PlanarNode[] Nodes { get; }
        private PlanarEdge _Edge;
        public PlanarEdge Edge
        { 
            
            get
            {
                if (_Edge == null) throw new Exception($"{nameof(Edge)} was not set");
                return _Edge;
            }
        }
        public PlanarSegment(params PlanarNode[] nodes)
        {
            Nodes = nodes;
            foreach (PlanarNode node in nodes)
            {
                node.AddBelongsTo(this);
            }
        }
        public void SetBelongsTo(PlanarEdge edge) {
            _Edge = edge;
        }
    }
}