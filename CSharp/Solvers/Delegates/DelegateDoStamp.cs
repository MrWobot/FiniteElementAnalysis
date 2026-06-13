using Core.Maths.Matrices;
using MathNet.Numerics.Statistics;
using System.Text;

namespace FiniteElementAnalysis.Solvers
{
    public delegate void DelegateDoStamp(out double[] rhs, out IBigMatrix K);
}