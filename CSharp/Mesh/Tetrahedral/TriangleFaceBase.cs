using Core.Maths;
using Core.Maths.Tensors;

namespace FiniteElementAnalysis.Mesh.Tetrahedral
{

    public class TriangleFaceBase : FaceBase
    {
        public Node NodeA { get { return Nodes[0]; } }
        public Node NodeB { get { return Nodes[1]; } }
        public Node NodeC { get { return Nodes[2]; } }
        public Vector3D Normal
        {
            get
            {
                // Use the order of nodes as provided by TetGen, which points the normal outward
                Vector3D v1 = Nodes[1] - Nodes[0];
                Vector3D v2 = Nodes[2] - Nodes[0];

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
                var a = Nodes[0];
                var b = Nodes[1];
                var c = Nodes[2];
                return GeometryHelper.TriangleArea(a.X, a.Y, a.Z, b.X, b.Y, b.Z, c.X, c.Y, c.Z);
            }
        }
        public TriangleFaceBase(Node[] nodes) : base(nodes)
        {

        }
    }
}