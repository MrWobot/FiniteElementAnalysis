
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Mesh.Tetrahedral;

namespace FiniteElementAnalysis.Mesh.Interfaces
{
    public interface IBoundaryPrimitive
    {

        public INode[] Nodes { get; }
        public IElement[] Elements { get; }
        public double Measure { get; }
        public double[] UnitNormal { get; }
        public Boundary Boundary { get; }
        public double[] Centre { get; }
    }
}
