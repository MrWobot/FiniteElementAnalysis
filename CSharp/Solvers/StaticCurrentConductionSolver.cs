using Core.Maths.Tensors;
using FiniteElementAnalysis.Boundaries;
using Core.Collections;
using FiniteElementAnalysis.Fields;
using FiniteElementAnalysis.SourceRegions;
using Core.FileSystem;
using Core.Pool;
using Core.Maths.Matrices;
using FiniteElementAnalysis.Results.ThreeD;
using FiniteElementAnalysis.Results;
using FiniteElementAnalysis.Solvers.Bases;
using FiniteElementAnalysis.Mesh.Interfaces;

namespace FiniteElementAnalysis.Solvers
{
    /// <summary>
    /// RHS vector = current NOT current density
    /// Unknowns = voltage
    /// </summary>
    public class StaticCurrentConductionSolver : ScalarSolver<StaticCurrentConductionResult3D>
    {
        public StaticCurrentConductionSolver() : base(FieldOperationType.Gradient)
        {
        }
        public override StaticCurrentConductionResult3D Solve(IMesh mesh, WorkingDirectoryManager workingDirectoryManager, string operationIdentifier = "default", DelegateApplySourceRegion[]? applySourceRegion_s = null, SolverMethod solverMethod = SolverMethod.BlockMatrixInversionGpuOnly, CompositeProgressHandler? progressHandler = null, FileCachedItem<CoreSolverResult>? cachedSolverResult = null, bool useCachedSolverResults = false)
        {
            CoreSolverResult basicResult = _Solve(mesh, workingDirectoryManager, operationIdentifier, applySourceRegion_s, solverMethod, progressHandler, cachedSolverResult, useCachedSolverResults);
            return new StaticCurrentConductionResult3D(mesh, basicResult);
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
            Dictionary<int, int> mapNodeToGlobalIndex = mesh.MapNodeIndexToGlobalIndex;
            // Get the faces on the boundary where the current is to be applied
            BoundaryFace[]? faces = mesh.GetFacesForBoundary(boundary);
            if (faces == null) return;

            // Calculate the total area of the boundary surface (sum of all triangle face areas)
            double totalFacesArea = faces.Select(f => f.Area).Sum();

            // Calculate the current density (A/m^2), which is the total current divided by the total surface area
            double currentDensity = boundary.Current / totalFacesArea;

            // Loop through each face to apply the current contribution to the nodes
            foreach (BoundaryFace face in faces)
            {
                double area = face.Area;

                // Ensure there's only one element associated with this boundary face
                if (face.Elements.Length > 1)
                {
                    throw new Exception($"Multiple elements found for a single face. This should not occur when applying {nameof(FixedCurrentBoundary)}.");
                }

                // For each node in the face, calculate the contribution of current (in Amps)
                // Current density remains the same across the surface, but the contribution
                // to each node depends on the area and the shape of the elements.

                // Each node in a triangular element receives 1/3 of the total current contribution for that element
                if (face.Nodes.Length != 3)
                {
                    throw new Exception("Invalid face: expected 3 nodes per face.");
                }

                foreach (Node faceNode in face.Nodes)
                {
                    int globalIndex = mapNodeToGlobalIndex[faceNode.Index];

                    // Each node gets 1/3 of the total current for the face
                    double nodalCurrentContribution = currentDensity * area / 3.0;

                    // Add the nodal current contribution to the RHS vector
                    rhs[globalIndex] += nodalCurrentContribution;
                }
            }
        }
    }
}