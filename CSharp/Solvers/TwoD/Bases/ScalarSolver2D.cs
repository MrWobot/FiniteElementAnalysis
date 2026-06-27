using Core.Maths;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Fields;

namespace FiniteElementAnalysis.Solvers.TwoD
{
    public abstract class ScalarSolver2D<TSolverResult> : SolverBaseSingleComponent2D<TSolverResult>
    {
        protected ScalarSolver2D(FieldOperationType fieldOperationType)
            : base(new FieldDOFInfo(1, 1, fieldOperationType))
        {

        }
        public abstract double GetK(Volume volume);
        protected override double[][] ScaleBTransposeByK(double[][] bTranspose, Volume volume)
        {
            double k = GetK(volume);
            var bTransposeScaledByK = MatrixHelper.Scale(bTranspose, k);
            return bTransposeScaledByK;
        }
    }
}