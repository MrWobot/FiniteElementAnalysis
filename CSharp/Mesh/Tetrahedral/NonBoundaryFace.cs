namespace FiniteElementAnalysis.Mesh.Tetrahedral
{

    public class NonBoundaryFace : TriangleFaceBase
    {
        public NonBoundaryFace(TetrahedralNode[] nodes,
            TetrahedralElement element)
            : base(nodes, new TetrahedralElement[] {element})
        {

        }
        public NonBoundaryFace(TetrahedralNode[] nodes,
            TetrahedralElement[] elements)
            :base(nodes, elements)
        {

        }
    }
}