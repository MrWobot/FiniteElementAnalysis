using Core.Maths;
using Core.Maths.Tensors;
using Core.Pool;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Fields;
using FiniteElementAnalysis.Mesh.Tetrahedral;
using System.Xml.Linq;

namespace FiniteElementAnalysis.Results.ThreeD
{
    public class HeatConductionResult3D : ScalarResultBase3D
    {
        public double[] NodalTemperatures => CoreResult.UnknownsVector;
        public HeatConductionResult3D(TetrahedralMesh mesh, CoreSolverResult coreResult) : base(mesh, coreResult)
        {

        }
    }
}