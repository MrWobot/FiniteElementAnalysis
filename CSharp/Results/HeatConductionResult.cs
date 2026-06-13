using Core.Maths;
using Core.Maths.Tensors;
using Core.Pool;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Fields;
using FiniteElementAnalysis.Mesh.Tetrahedral;
using FiniteElementAnalysis.Solvers;
using System.Xml.Linq;

namespace FiniteElementAnalysis.Results
{
    public class HeatConductionResult : ScalarResultBase
    {
        public double[] NodalTemperatures => CoreResult.UnknownsVector;
        public HeatConductionResult(TetrahedralMesh mesh, CoreSolverResult coreResult) : base(mesh, coreResult)
        {

        }
    }
}