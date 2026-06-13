using Core.Maths;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Fields;

namespace FiniteElementAnalysis.Solvers
{
    public abstract class ScalarSolver<TSolverResult>:SolverBaseSingleComponent<TSolverResult>
    {
        protected ScalarSolver(FieldOperationType fieldOperationType) 
            :base(new FieldDOFInfo(1, 1, fieldOperationType))
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