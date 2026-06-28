using Core.Maths;
using Core.Maths.Tensors;
using Core.Pool;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Fields;
using FiniteElementAnalysis.Mesh.Interfaces;
using FiniteElementAnalysis.Mesh.Tetrahedral;
using System.Xml.Linq;

namespace FiniteElementAnalysis.Results.ThreeD
{
    public class HeatConductionResult3D : ScalarResultBase
    {
        public double[] NodalTemperatures => CoreResult.UnknownsVector;
        public HeatConductionResult3D(IMesh mesh, CoreSolverResult coreResult) : base(mesh, coreResult)
        {

        }
    }
}