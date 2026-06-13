using Core.Maths.Matrices;
using Core.Pool;
using FiniteElementAnalysis.Fields;
using FiniteElementAnalysis.Mesh.Tetrahedral;

namespace FiniteElementAnalysis.SourceRegions
{
    public delegate void DelegateApplySourceRegion(
            TetrahedralMesh mesh,
            FieldDOFInfo fieldDOFInfo,
            IBigMatrix K,
            double[] rhs, 
            string operationIdentifier,
            CompositeProgressHandler? parentProgressHandler);
}