using Core.Maths;
using Core.Maths.Tensors;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Mesh.Interfaces;

namespace FiniteElementAnalysis.Mesh.Tetrahedral
{

    public class TriangleFaceBase : FaceBase
    {
        public IElement[] Elements { get; private set; }
        public void AddElement(TetrahedronElement element)
        {
            var oldElements = Elements;
            Elements = new TetrahedronElement[oldElements.Length + 1];
            Array.Copy(oldElements, Elements, oldElements.Length);
            Elements[oldElements.Length] = element;
        }
        public Node NodeA { get { return (Node)Nodes[0]; } }
        public Node NodeB { get { return (Node)Nodes[1]; } }
        public Node NodeC { get { return (Node)Nodes[2]; } }
        public Vector3D Normal
        {
            get
            {
                // Use the order of nodes as provided by TetGen, which points the normal outward
                Vector3D v1 = NodeB - NodeA;
                Vector3D v2 = NodeC - NodeA;

                // Compute the cross product to get the normal vector
                Vector3D normal = v1.Cross(v2);

                // Normalize the normal vector to get a unit normal vector
                return normal.Normalize();
            }
        }
        public double Area
        {
            get
            {
                return GeometryHelper.TriangleArea(NodeA.X, NodeA.Y, NodeA.Z, NodeB.X, NodeB.Y, NodeB.Z, NodeC.X, NodeC.Y, NodeC.Z);
            }
        }

        public TriangleFaceBase(Node[] nodes, TetrahedronElement[] elements) : base(nodes)
        {
            Elements = elements;
        }
    }
}