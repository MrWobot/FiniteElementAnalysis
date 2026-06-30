using Core.Maths;
using Core.Maths.Vectors;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Mesh.Interfaces;
using FiniteElementAnalysis.Mesh.Planar;

namespace FiniteElementAnalysis.Mesh.Tetrahedral
{

    public class BoundaryFace : TriangleFaceBase, IBoundaryPrimitive
    {
        private double? _Measure;
        public double Measure
        {
            get
            {
                if (_Measure == null)
                {
                    double[] ab = new double[] { NodeB.X - NodeA.X, NodeB.Y - NodeA.Y, NodeB.Z - NodeA.Z };
                    double[] ac = new double[] { NodeC.X - NodeA.X, NodeC.Y - NodeA.Y, NodeC.Z - NodeA.Z };
                    double[] cross = VectorHelper.Cross(ab, ac);
                    _Measure = VectorHelper.Magnitude(cross) / 2.0;
                }
                return (double)_Measure;
            }
        }
        public double[] UnitNormal
        {
            get
            {
                double[] ab = new double[] { NodeB.X - NodeA.X, NodeB.Y - NodeA.Y, NodeB.Z - NodeA.Z };
                double[] ac = new double[] { NodeC.X - NodeA.X, NodeC.Y - NodeA.Y, NodeC.Z - NodeA.Z };
                double[] cross = VectorHelper.Cross(ab, ac);
                double magnitude = VectorHelper.Magnitude(cross);
                return new double[] { cross[0] / magnitude, cross[1] / magnitude, cross[2] / magnitude };
            }
        }

        public double[] Centre
        {
            get
            {
                double xSum = 0, ySum = 0, zSum = 0;
                foreach (INode node in Nodes)
                {
                    Node n = (Node)node;
                    xSum += n.X;
                    ySum += n.Y;
                    zSum += n.Z;
                }
                return new double[] { xSum / 3d, ySum / 3d, zSum / 3d };
            }
        }
        public Boundary Boundary { get; }

        public BoundaryFace(Node[] nodes,
            Boundary boundary, TetrahedronElement element)
            : base(nodes, new TetrahedronElement[] {element})
        {
            Boundary = boundary;
        }
        public BoundaryFace(Node[] nodes, 
            Boundary boundary, TetrahedronElement[] elements)
            :base(nodes, elements)
        {
            Boundary = boundary;
        }
    }
}