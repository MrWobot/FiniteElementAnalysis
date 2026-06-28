using Core.Maths.Matrices;
using Core.Pool;
using FiniteElementAnalysis.Mesh.Interfaces;

namespace FiniteElementAnalysis.SourceRegions
{
    public delegate void DelegateApplySourceRegion2(
            IMesh mesh,
            int nDegreesOfFreedom,
            IBigMatrix K,
            double[] rhs, 
            string operationIdentifier,
            CompositeProgressHandler? parentProgressHandler);
}