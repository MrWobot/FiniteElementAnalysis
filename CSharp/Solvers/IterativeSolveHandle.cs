using Core.Maths.Matrices;
using MathNet.Numerics.Statistics;
using System.Text;

namespace FiniteElementAnalysis.Solvers
{
    public class IterativeSolveHandle:IDisposable
    {
        public DelegateDoStamp DoStamp { get; }
        public DelegateDoSolve DoSolve { get; }
        private Action _Dispose;
        public IterativeSolveHandle(DelegateDoStamp doStamp,
            DelegateDoSolve doSolve, Action dispose) { 
            DoStamp = doStamp;
            DoSolve = doSolve;
            _Dispose = dispose;
        }
        ~IterativeSolveHandle() {
            Dispose();
        }
        public void Dispose() {
            _Dispose();
        }
    }
}