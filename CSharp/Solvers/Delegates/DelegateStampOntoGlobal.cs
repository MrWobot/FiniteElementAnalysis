using Core.Maths.Matrices;
using FiniteElementAnalysis.Mesh;
using MathNet.Numerics.Statistics;
using System.Text;

namespace FiniteElementAnalysis.Solvers
{
    public delegate void DelegateStampOntoGlobal(Node[] nodes, double[][] Ke, double[] rhsE);
}