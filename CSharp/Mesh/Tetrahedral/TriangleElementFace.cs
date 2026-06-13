using Core.Maths;
using Core.Maths.Tensors;
using FiniteElementAnalysis.Boundaries;

namespace FiniteElementAnalysis.Mesh.Tetrahedral
{

    public class TriangleElementFace : TriangleFaceBase
    {
        public TetrahedronElement Element { get; private set; }

        public TriangleElementFace(Node nodeA, Node nodeB, Node nodeC,
            TetrahedronElement element) : base(new Node[] {nodeA, nodeB, nodeC})
        {
            Element = element;
        }
    }
}