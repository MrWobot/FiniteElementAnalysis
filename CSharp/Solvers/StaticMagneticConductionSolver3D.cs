using FiniteElementAnalysis.Fields;

namespace FiniteElementAnalysis.Solvers
{
    public class StaticMagneticConductionSolver3D : StaticMagneticConductionSolverBase
    {
        public StaticMagneticConductionSolver3D() : base(new FieldDOFInfo(3, 3, FieldOperationType.Curl))
        {
        }
    }
}
