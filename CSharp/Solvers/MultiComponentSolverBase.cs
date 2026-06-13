using FiniteElementAnalysis.SourceRegions;
using Core.Pool;
using FiniteElementAnalysis.Mesh.Tetrahedral;
using Core.Maths.Matrices;
using FiniteElementAnalysis.Boundaries;

namespace FiniteElementAnalysis.Solvers
{
    public abstract class MultiComponentSolverBase<TSolverResult>: SolverBase<TSolverResult>
    {
        protected MultiComponentSolverBase(int nDegreesOfFreedom) : base(nDegreesOfFreedom)
        {
        }

        protected override void StampElementMatricesOntoGlobal(IBigMatrix K, double[] rhs, int size, 
            TetrahedralMesh mesh,
            CompositeProgressHandler? parentProgressHandler) {
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
                Volume volume = element.VolumeIsAPartOf!;
                if (!typeof(MultiComponentVolume).IsAssignableFrom(volume.GetType())) {
                    throw new InvalidOperationException($"Only {nameof(MultiComponentVolume)} is supported");
                }
                MultiComponentVolume multiMaterialVolume = (MultiComponentVolume)volume;
                foreach (VolumeComponent component in multiMaterialVolume.Components)
                {
                    StampElementOntoGlobal(element, volume, rhs, stampOntoGlobal,
                        component.NFieldComponents, component.FieldOperationType);
                }
                updateProgress?.Invoke();
            }
            progressHandler?.Set(1);
        }
        private void ApplySourceRegions(
            DelegateApplySourceRegion2[]? applySourceRegion_s,
            TetrahedralMesh mesh,
            IBigMatrix K, 
            double[] rhs,
            string operationIdentifier, 
            CompositeProgressHandler parentProgressHandler) {
            bool[] rhsIndexSets = new bool[rhs.Length];
            if (applySourceRegion_s == null||applySourceRegion_s.Length <= 0)
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
                applySourceRegion(mesh, _NDegreesOfFreedom, K, rhs, operationIdentifier, progressHandler);
            }
        }
    }
}