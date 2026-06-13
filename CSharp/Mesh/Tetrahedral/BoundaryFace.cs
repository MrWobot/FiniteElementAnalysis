using Core.Maths;
using Core.Maths.Tensors;
using FiniteElementAnalysis.Boundaries;

namespace FiniteElementAnalysis.Mesh.Tetrahedral
{

    public class BoundaryFace : TriangleFaceBase
    {
        public int Marker { get; }
        public Boundary? Boundary { get; }
        public TetrahedronElement[] Elements { get; private set; }
        public void AddElement(TetrahedronElement element)
        {
            var oldElements = Elements;
            Elements = new TetrahedronElement[oldElements.Length + 1];
            Array.Copy(oldElements, Elements, oldElements.Length);
            Elements[oldElements.Length] = element;
        }
        public BoundaryFace(int marker, Node[] nodes, 
            Boundary? boundary, TetrahedronElement element)
            :base(nodes)
        {
            Marker = marker;
            Boundary = boundary;
            Elements = new TetrahedronElement[] { element };
        }
        public BoundaryFace(int marker, Node[] nodes, 
            Boundary? boundary, TetrahedronElement[] elements)
            :base(nodes)
        {
            Marker = marker;
            Boundary = boundary;
            Elements = elements;
        }
    }
}