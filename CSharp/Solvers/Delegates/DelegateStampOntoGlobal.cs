using FiniteElementAnalysis.Mesh.Interfaces;

namespace FiniteElementAnalysis.Solvers
{
    public delegate void DelegateStampOntoGlobal(IReadOnlyList<INode> nodes, double[][] Ke, double[] rhsE);
}