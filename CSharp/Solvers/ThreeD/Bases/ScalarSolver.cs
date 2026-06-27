using Core.Maths;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Fields;

namespace FiniteElementAnalysis.Solvers.ThreeD
{
    public abstract class ScalarSolver3D<TSolverResult> : SolverBaseSingleComponent3D<TSolverResult>
    {
        protected ScalarSolver3D(FieldOperationType fieldOperationType)
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