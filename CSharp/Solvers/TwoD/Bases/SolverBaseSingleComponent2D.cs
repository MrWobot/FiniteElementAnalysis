using Core.Maths;
using FiniteElementAnalysis.SourceRegions;
using FiniteElementAnalysis.Fields;
using Core.Pool;
using FiniteElementAnalysis.Mesh.Tetrahedral;
using FiniteElementAnalysis.Mesh;
using Core.Maths.Matrices;

namespace FiniteElementAnalysis.Solvers.TwoD
{
    public abstract class SolverBaseSingleComponent2D<TSolverResult> : SolverBase2D<TSolverResult>
    {
        protected FieldDOFInfo _FieldDOFInfo;
        protected SolverBaseSingleComponent2D(FieldDOFInfo fieldDOFInfo) : base(fieldDOFInfo.NDegreesOfFreedom)
        {
            _FieldDOFInfo = fieldDOFInfo;
        }
        protected override void StampElementMatricesOntoGlobal(IBigMatrix K, double[] rhs, int size,
            TetrahedralMesh mesh,
            CompositeProgressHandler? parentProgressHandler)
        {
            StandardProgressHandler? progressHandler = null;
            Action? updateProgress = null;
            if (parentProgressHandler != null)
            {
                progressHandler = new StandardProgressHandler();
                parentProgressHandler.AddChild(progressHandler);
                updateProgress = progressHandler?.GetUpdateProgress(mesh.Elements.Length, 20);
            }
            DelegateStampOntoGlobal stampOntoGlobal =
                Get_StampOntoGlobal(K, rhs, size, mesh.MapNodeIdentifierToGlobalIndex);
            foreach (TetrahedronElement element in mesh.Elements)
            {
                StampElementOntoGlobal(
                    element,
                    element.VolumeIsAPartOf!,
                    rhs, stampOntoGlobal,
                    _FieldDOFInfo.NFieldComponents,
                    _FieldDOFInfo.FieldOperationType
                );
                updateProgress?.Invoke();
            }
            progressHandler?.Set(1);
        }
        protected override void ApplySourceRegions(
            DelegateApplySourceRegion[]? applySourceRegion_s,
            TetrahedralMesh mesh,
            IBigMatrix K,
            double[] rhs,
            string operationIdentifier,
            CompositeProgressHandler parentProgressHandler)
        {
            bool[] rhsIndexSets = new bool[rhs.Length];
            if (applySourceRegion_s == null || applySourceRegion_s.Length <= 0)
            {
                StandardProgressHandler progressHandlerForNone = new StandardProgressHandler();
                parentProgressHandler.AddChild(progressHandlerForNone);
                progressHandlerForNone.Set(1);
                return;

            }
            CompositeProgressHandler progressHandler = new CompositeProgressHandler(applySourceRegion_s.Length);
            parentProgressHandler.AddChild(progressHandler);
            foreach (DelegateApplySourceRegion applySourceRegion in applySourceRegion_s)
            {
                applySourceRegion(mesh, _FieldDOFInfo, K, rhs, operationIdentifier, progressHandler);
            }
        }
    }
}