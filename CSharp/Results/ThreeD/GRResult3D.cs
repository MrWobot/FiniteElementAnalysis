using FiniteElementAnalysis.Mesh.Tetrahedral;

namespace FiniteElementAnalysis.Results.ThreeD
{
    public class GRResult3D : VectorResultBase
    {
        public GRResult3D(TetrahedralMesh mesh, CoreSolverResult coreResult) : base(mesh, coreResult)
        {

        }
    }
}