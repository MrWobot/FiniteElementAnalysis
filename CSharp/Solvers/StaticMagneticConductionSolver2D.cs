using FiniteElementAnalysis.Fields;

namespace FiniteElementAnalysis.Solvers
{
    public class StaticMagneticConductionSolver2D : StaticMagneticConductionSolverBase
    {
        public StaticMagneticConductionSolver2D() : base(new FieldDOFInfo(1, 2, FieldOperationType.Gradient))
        {

        }
    }
}
