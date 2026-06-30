using Core.Maths;
using Core.Maths.Tensors;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Mesh.Interfaces;

namespace FiniteElementAnalysis.Mesh.Tetrahedral
{

    public class FaceBase
    {
        public INode[] Nodes { get; private set; }
        public void ReverseNodes() {
            Nodes = Nodes.Reverse().ToArray();
        }
        public FaceBase(TetrahedralNode[] nodes)
        {
            if (nodes.Length != 3) throw new ArgumentException($"There should only be three nodes in a face. {nodes.Length} was provided");
            Nodes = nodes;
        }
        public int[] NodeIdentifiersLowToHigh
        {
            get
            {

                return Nodes.Select(n => n.Identifier).OrderBy(i => i).ToArray();
            }
        }
    }
}