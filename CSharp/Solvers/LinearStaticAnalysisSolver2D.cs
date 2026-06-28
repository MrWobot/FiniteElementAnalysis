using FiniteElementAnalysis.Fields;

namespace FiniteElementAnalysis.Solvers
{
    public class LinearStaticAnalysisSolver2D : LinearStaticAnalysisSolverBase
    {
        public LinearStaticAnalysisSolver2D() : base(new FieldDOFInfo(2, 3, FieldOperationType.StrainDisplacement))
        {
        }
    }
}