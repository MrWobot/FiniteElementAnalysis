using FiniteElementAnalysis.SourceRegions;
using Core.Pool;
using Core.Maths.Matrices;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Mesh.Interfaces;
using FiniteElementAnalysis.Fields;

namespace FiniteElementAnalysis.Solvers.Bases
{
    public abstract class MultiComponentSolverBase<TSolverResult> : SolverBase<TSolverResult>
    {
        protected MultiComponentSolverBase(FieldDOFInfo fieldDOFInfo) : base(fieldDOFInfo)
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
                Volume volume = element.VolumeBelongsTo!;
                if (!typeof(MultiComponentVolume).IsAssignableFrom(volume.GetType()))
                {
                    throw new InvalidOperationException($"Only {nameof(MultiComponentVolume)} is supported");
                }
                MultiComponentVolume multiMaterialVolume = (MultiComponentVolume)volume;
                foreach (VolumeComponent component in multiMaterialVolume.Components)
                {
                    StampElementOntoGlobal(element, volume, rhs, stampOntoGlobal,
                        component.NFieldComponents, component.FieldOperationType, mesh.GetThickness);
                }
                updateProgress?.Invoke();
            }
            progressHandler?.Set(1);
        }
        private void ApplySourceRegions(
            DelegateApplySourceRegion2[]? applySourceRegion_s,
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
            foreach (DelegateApplySourceRegion2 applySourceRegion in applySourceRegion_s)
            {
                applySourceRegion(mesh, _FieldDOFInfo.NDegreesOfFreedom, K, rhs, operationIdentifier, progressHandler);
            }
        }
    }
}