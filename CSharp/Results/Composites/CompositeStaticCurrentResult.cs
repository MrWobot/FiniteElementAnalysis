using Core.Maths.Matrices;
using Core.Maths.Tensors;
using Core.Pool;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Fields;
using FiniteElementAnalysis.Mesh.Tetrahedral;
using FiniteElementAnalysis.Results.ThreeD;
using FiniteElementAnalysis.Solvers;
using System.Xml.Linq;

namespace FiniteElementAnalysis.Results.Composites
{
    public class CompositeStaticCurrentResult
    {
        private StaticCurrentConductionResult3D[] _Results;
        public CompositeStaticCurrentResult(params StaticCurrentConductionResult3D[] results)
        {
            _Results = results;
        }
        public void ApplyVolumetricCurrentDensities(
            TetrahedralMesh mesh,
            FieldDOFInfo fieldDOFInfo,
            IBigMatrix K,
            double[] rhs,
            string operationIdentifier,
            CompositeProgressHandler? parentProgressHandler)
        {
            CompositeProgressHandler? progressHandler = parentProgressHandler==null
                ?null
                :new CompositeProgressHandler(_Results.Length);
            foreach (StaticCurrentConductionResult3D result in _Results) {
                result.ApplyVolumeCurrentDensities(
                    mesh, fieldDOFInfo, K, 
                    rhs, operationIdentifier, 
                    progressHandler);
            }
        }
    }
}