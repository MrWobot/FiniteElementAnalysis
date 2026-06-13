using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Fields;
using FiniteElementAnalysis.Results;
using Core.FileSystem;
using FiniteElementAnalysis.SourceRegions;
using Core.Pool;
using FiniteElementAnalysis.Mesh.Tetrahedral;
using Core.Maths.Matrices;

namespace FiniteElementAnalysis.Solvers
{
    //https://www.comsol.com/multiphysics/finite-element-method
    //This one explains overlap of basis functions
    //https://www.researchgate.net/publication/382609774_Finite_element_solution_of_heat_conduction_in_complex_3D_geometries
    //The heat conduction equation, also known as heat diffusion equation or fouriers law.
    public class GRSolver : MultiComponentSolverBase<GRResult>
    {
        public GRSolver() : base(10)
        {

        }

        public override GRResult Solve(TetrahedralMesh mesh, WorkingDirectoryManager workingDirectoryManager, string operationIdentifier = "default", DelegateApplySourceRegion[]? applySourceRegion_s = null, SolverMethod solverMethod = SolverMethod.BlockMatrixInversionGpuOnly, CompositeProgressHandler? progressHandler = null, FileCachedItem<CoreSolverResult>? cachedSolverResult = null, bool useCachedSolverResults = false)
        {
            throw new NotImplementedException();
        }

        protected override void ApplyBoundaryToGlobal(Boundary boundary, TetrahedralMesh mesh, IBigMatrix K, double[] rhs, string operationIdentifier)
        {
            throw new NotImplementedException();
        }

        protected override void ApplySourceRegions(DelegateApplySourceRegion[]? applySourceRegion_s, TetrahedralMesh mesh, IBigMatrix K, double[] rhs, string operationIdentifier, CompositeProgressHandler parentProgressHandler)
        {
            throw new NotImplementedException();
        }

        protected override double[][] ScaleBTransposeByK(double[][] bTranspose, Volume volume)
        {
            throw new NotImplementedException();
        }
    }
}