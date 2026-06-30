using Core.Maths;
using FiniteElementAnalysis.SourceRegions;
using FiniteElementAnalysis.Fields;
using Core.Pool;
using Core.Maths.Matrices;
using FiniteElementAnalysis.Mesh.Interfaces;

namespace FiniteElementAnalysis.Solvers.Bases
{
    public abstract class SolverBaseSingleComponent<TSolverResult> : SolverBase<TSolverResult>
    {
        protected SolverBaseSingleComponent(FieldDOFInfo fieldDOFInfo) : base(fieldDOFInfo)
        {

        }
        protected override void StampElementMatricesOntoGlobal(IBigMatrix K, double[] rhs, int size,
            IMesh mesh,
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
                Get_StampOntoGlobal(K, rhs, size, mesh.MapNodeIdentifierToGlobalIndex, mesh.NNodesPerElement);
            foreach (IElement element in mesh.Elements)
            {
                StampElementOntoGlobal(
                    element,
                    element.VolumeBelongsTo!,
                    rhs, stampOntoGlobal,
                    _FieldDOFInfo.NFieldComponents,
                    _FieldDOFInfo.FieldOperationType,
                    mesh.GetThickness
                );
                updateProgress?.Invoke();
            }
            progressHandler?.Set(1);
        }
        protected override void ApplySourceRegions(
            DelegateApplySourceRegion[]? applySourceRegion_s,
            IMesh mesh,
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