using Core.Maths.Tensors;
using FiniteElementAnalysis.Boundaries;
using Core.Collections;
using FiniteElementAnalysis.Fields;
using FiniteElementAnalysis.SourceRegions;
using Core.FileSystem;
using Core.Pool;
using Core.Maths.Matrices;
using FiniteElementAnalysis.Solvers.Bases;
using FiniteElementAnalysis.Mesh.Interfaces;
using FiniteElementAnalysis.Results.Bases;
using FiniteElementAnalysis.Results;

namespace FiniteElementAnalysis.Solvers
{
    /// <summary>
    /// RHS vector = current NOT current density
    /// Unknowns = voltage
    /// </summary>
    public class StaticCurrentConductionSolver : ScalarSolver<StaticCurrentConductionResult>
    {
        public StaticCurrentConductionSolver() : base(FieldOperationType.Gradient)
        {
        }
        public override StaticCurrentConductionResult Solve(IMesh mesh, WorkingDirectoryManager workingDirectoryManager, string operationIdentifier = "default", DelegateApplySourceRegion[]? applySourceRegion_s = null, SolverMethod solverMethod = SolverMethod.BlockMatrixInversionGpuOnly, CompositeProgressHandler? progressHandler = null, FileCachedItem<CoreSolverResult>? cachedSolverResult = null, bool useCachedSolverResults = false)
        {
            CoreSolverResult basicResult = _Solve(mesh, workingDirectoryManager, operationIdentifier, applySourceRegion_s, solverMethod, progressHandler, cachedSolverResult, useCachedSolverResults);
            return new StaticCurrentConductionResult(mesh, basicResult);
        }

        public override double GetK(Volume volume)
        {
            return ((StaticCurrentVolume)volume).Conductivity;
        }
        protected override void ApplyBoundaryToGlobal(Boundary boundary, IMesh mesh,
            IBigMatrix K, double[] rhs, string operationIdentifier)
        {
            switch (boundary.BoundaryConditionType)
            {
                case BoundaryConditionType.AdiabaticInsulatedBoundary:
                    break;
                case BoundaryConditionType.FixedCurrentBoundary:
                    ApplyFixedCurrentBoundary((FixedCurrentBoundary)boundary,
                        mesh, K, rhs, operationIdentifier);
                    break;
                case BoundaryConditionType.FixedVoltageDirichletBoundary:
                    ApplyDirichletBoundary(boundary, mesh, K, rhs,
                        ((FixedVoltageDirichletBoundary)boundary).Voltage);
                    break;
                case BoundaryConditionType.MaterialBoundary:
                    break;
                case BoundaryConditionType.MeasurementBoundary:
                    break;
                default:
                    throw new NotImplementedException($"The boundary {Enum.GetName(typeof(BoundaryConditionType), boundary.BoundaryConditionType)} is not implemented");
            }
        }
        private static void ApplyFixedCurrentBoundary(
            FixedCurrentBoundary boundary, IMesh mesh, IBigMatrix K, double[] rhs, string operationIdentifier)
        {
            Dictionary<int, int> mapNodeToGlobalIndex = mesh.MapNodeIdentifierToGlobalIndex;
            IBoundaryPrimitive[]? primitives = mesh.GetPrimitivesForBoundary(boundary);
            if (primitives == null) return;

            // Total effective area including thickness
            double totalArea = primitives.Sum(p =>
            {
                double thicknessIntegral = p.Nodes
                    .Sum(n => mesh.GetThickness(n.Position)) / p.Nodes.Length;
                return p.Measure * thicknessIntegral;
            });

            // Current density = total current / total area
            double currentDensity = boundary.Current / totalArea;

            foreach (IBoundaryPrimitive primitive in primitives)
            {
                if (primitive.Elements.Length > 1)
                    throw new Exception($"Multiple elements found for a single primitive. This should not occur when applying {nameof(FixedCurrentBoundary)}.");

                double thicknessIntegral = primitive.Nodes
                    .Sum(n => mesh.GetThickness(n.Position)) / primitive.Nodes.Length;
                double measure = primitive.Measure * thicknessIntegral;
                int n = primitive.Nodes.Length;

                foreach (INode node in primitive.Nodes)
                {
                    int globalIndex = mapNodeToGlobalIndex[node.Identifier];
                    rhs[globalIndex] += currentDensity * measure / n;
                }
            }
        }
    }
}