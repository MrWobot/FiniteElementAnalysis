using Core.Maths;
using Core.Maths.Tensors;
using FiniteElementAnalysis.Boundaries;

namespace FiniteElementAnalysis.Mesh.Tetrahedral
{

    public class FaceBase
    {
        public Node[] Nodes { get; private set; }
        public void ReverseNodes() {
            Nodes = Nodes.Reverse().ToArray();
        }
        public Vector3D CenterPoint { 
            get
            {
                double x = Nodes.Sum(n => n.X) / 3d;
                double y = Nodes.Sum(n => n.Y) / 3d;
                double z = Nodes.Sum(n => n.Z) / 3d;
                return new Vector3D(x, y, z);
            } 
        }
        public FaceBase(Node[] nodes)
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