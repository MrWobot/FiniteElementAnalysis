using FiniteElementAnalysis.Mesh.Tetrahedral;
using FiniteElementAnalysis.Solvers;

namespace FiniteElementAnalysis.Results
{
    public class ElectrostaticsResult : ScalarResultBase
    {
        public double[] Potentials => CoreResult.UnknownsVector;
        public ElectrostaticsResult(TetrahedralMesh mesh, CoreSolverResult coreResult) : base(mesh, coreResult)
        {

        }
    }
}