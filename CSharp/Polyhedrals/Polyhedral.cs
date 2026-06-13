
namespace FiniteElementAnalysis.Polyhedrals
{

    public abstract class Polyhedral
    {
        public PolyhedralNode[] Nodes { get; }
        public Polyhedral(params PolyhedralNode[] nodes)
        {
            Nodes = nodes;
        }
    }
}