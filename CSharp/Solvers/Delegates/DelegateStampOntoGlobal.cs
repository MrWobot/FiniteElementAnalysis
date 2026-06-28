using FiniteElementAnalysis.Mesh.Interfaces;

namespace FiniteElementAnalysis.Solvers
{
    public delegate void DelegateStampOntoGlobal(INode[] nodes, double[][] Ke, double[] rhsE);
}