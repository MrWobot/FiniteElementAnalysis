using FiniteElementAnalysis.Mesh.Tetrahedral;
using FiniteElementAnalysis.Results.Bases;

namespace FiniteElementAnalysis.Results
{
    public class GRResult3D : VectorResultBase
    {
        public GRResult3D(TetrahedralMesh mesh, CoreSolverResult coreResult) : base(mesh, coreResult)
        {

        }
    }
}