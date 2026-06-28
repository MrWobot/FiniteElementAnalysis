using Core.Maths.Tensors;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Mesh.Interfaces;
namespace FiniteElementAnalysis.Mesh.Tetrahedral
{

    public class Node : Vector3D, INode
    {
        public int Index{ get; }
        private double[]? _Attributes;
        public double[] Attributes { get { return _Attributes!; } }
        public override bool Equals(object? obj)
        {
            return obj is Node node &&
                   Index == node.Index;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Index);
        }
        public double[]? Values { get; set; }
        public double ScalarValue
        {
            get
            {
                if (Values == null) throw new Exception($"{nameof(Values)}  was not set");
                if (Values.Length > 1) throw new Exception($"{nameof(Values)} did not contain a single value for a single degree of freedom");
                return Values[0];
            }
        }

        public double[] Position => new double[] { X, Y, Z };

        public Node(int index, double x, double y, double z, double[]? attributes)
            : base(x, y, z)
        {
            Index = index;
            _Attributes = attributes;
        }
    }
}