namespace FiniteElementAnalysis.Polyhedrals
{

    public class PolyhedralPolygon
    {
        public PolyhedralNode[] Nodes { get; }
        private PolyhedralFacet _Facet;
        public PolyhedralFacet Facet { 
            
            get
            {
                if (_Facet == null) throw new Exception($"{nameof(Facet)} was not set");
                return _Facet;
            }
        }
        public PolyhedralPolygon(params PolyhedralNode[] nodes)
        {
            Nodes = nodes;
            foreach (PolyhedralNode node in nodes)
            {
                node.AddBelongsTo(this);
            }
        }
        public void SetBelongsTo(PolyhedralFacet facet) {
            _Facet = facet;
        }
    }
}