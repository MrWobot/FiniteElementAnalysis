namespace FiniteElementAnalysis.Mesh.Tetrahedral
{

    public class NonBoundaryFace : TriangleFaceBase
    {
        public NonBoundaryFace(Node[] nodes,
            TetrahedronElement element)
            : base(nodes, new TetrahedronElement[] {element})
        {

        }
        public NonBoundaryFace(Node[] nodes,
            TetrahedronElement[] elements)
            :base(nodes, elements)
        {

        }
    }
}