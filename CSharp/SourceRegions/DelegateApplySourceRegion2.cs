using Core.Maths.Matrices;
using Core.Pool;
using FiniteElementAnalysis.Fields;
using FiniteElementAnalysis.Mesh.Tetrahedral;

namespace FiniteElementAnalysis.SourceRegions
{
    public delegate void DelegateApplySourceRegion2(
            TetrahedralMesh mesh,
            int nDegreesOfFreedom,
            IBigMatrix K,
            double[] rhs, 
            string operationIdentifier,
            CompositeProgressHandler? parentProgressHandler);
}