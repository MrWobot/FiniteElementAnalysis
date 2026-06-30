using FiniteElementAnalysis.Mesh.Interfaces;
using FiniteElementAnalysis.Results.Bases;

namespace FiniteElementAnalysis.Results
{
    public class HeatConductionResult : ScalarResultBase
    {
        public double[] NodalTemperatures => CoreResult.UnknownsVector;
        public HeatConductionResult(IMesh mesh, CoreSolverResult coreResult) : base(mesh, coreResult)
        {

        }
    }
}