using FiniteElementAnalysis.Fields;

namespace FiniteElementAnalysis.Solvers
{
    public class LinearStaticAnalysisSolver3D:LinearStaticAnalysisSolverBase
    {
        public LinearStaticAnalysisSolver3D() : base(new FieldDOFInfo(3, 6, FieldOperationType.StrainDisplacement))
        {
        }
    }
}