using FiniteElementAnalysis.Boundaries;

namespace FiniteElementAnalysis.Mesh.Tetrahedral
{

    public class BoundaryFace : TriangleFaceBase
    {
        public Boundary Boundary { get; }
        public int Marker { get; }
        public BoundaryFace(int marker, Node[] nodes,
            Boundary boundary, TetrahedronElement element)
            : base(nodes, new TetrahedronElement[] {element})
        {
            Marker = marker;
            Boundary = boundary;
        }
        public BoundaryFace(int marker, Node[] nodes, 
            Boundary boundary, TetrahedronElement[] elements)
            :base(nodes, elements)
        {
            Marker = marker;
            Boundary = boundary;
        }
    }
}