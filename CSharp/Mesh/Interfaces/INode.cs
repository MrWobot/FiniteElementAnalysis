using Core.Maths.Tensors;

namespace FiniteElementAnalysis.Mesh.Interfaces
{
    public interface INode
    {
        public double[] Position { get; }
        public int Index { get; }
        public double[]? Values { get; set; }
    }
}
