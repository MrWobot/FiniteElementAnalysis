using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Fields;
using System;
using System.Collections.Generic;
using FiniteElementAnalysis.Boundaries.Magnetic;
using Core.FileSystem;
using FiniteElementAnalysis.SourceRegions;
using Core.Pool;
using FiniteElementAnalysis.Mesh.Tetrahedral;
using Core.Maths.Matrices;
using Core.Maths;
using FiniteElementAnalysis.Solvers.Bases;
using FiniteElementAnalysis.Mesh.Interfaces;
using FiniteElementAnalysis.Results.Bases;
using FiniteElementAnalysis.Results;

namespace FiniteElementAnalysis.Solvers
{
    public abstract class StaticMagneticConductionSolverBase : SolverBaseSingleComponent<StaticMagneticConductionResult>
    {
        public StaticMagneticConductionSolverBase(FieldDOFInfo fieldDOFInfo) : base(fieldDOFInfo)
        {
        }

        // Get the permeability for the given volume
        private double GetK(Volume volume)
        {
            return 1d / ((StaticMagneticConductionVolume)volume).Permeability;
        }
        protected override double[][] ScaleBTransposeByK(double[][] bTranspose, Volume volume)
        {
            double k = GetK(volume);
            var bTransposeScaledByK = MatrixHelper.Scale(bTranspose, k);
            return bTransposeScaledByK;
        }

        public override StaticMagneticConductionResult Solve(IMesh mesh, WorkingDirectoryManager workingDirectoryManager, string operationIdentifier = "default", DelegateApplySourceRegion[]? applySourceRegion_s = null, SolverMethod solverMethod = SolverMethod.BlockMatrixInversionGpuOnly, CompositeProgressHandler? progressHandler = null, FileCachedItem<CoreSolverResult>? cachedSolverResult = null, bool useCachedSolverResults = false)
        {
            CoreSolverResult coreResult = _Solve(mesh, workingDirectoryManager, operationIdentifier, applySourceRegion_s, solverMethod,
                progressHandler, cachedSolverResult, useCachedSolverResults);
            return new StaticMagneticConductionResult(mesh, coreResult);
        }

        // Apply boundary conditions to the global matrix K and rhs vector
        protected override void ApplyBoundaryToGlobal(Boundary boundary, IMesh mesh,
            IBigMatrix K, double[] rhs, string operationIdentifier)
        {
            switch (boundary.BoundaryConditionType)
            {
                case BoundaryConditionType.AdiabaticInsulatedBoundary:
                    // Handle adiabatic insulated boundary
                    break;

                case BoundaryConditionType.FixedMagneticVectorPotentialDirichletBoundary:
                    ApplyDirichletBoundary(boundary, mesh, K, rhs,
                        ((FixedMagneticVectorPotentialDirichletBoundary)boundary).MagneticVectorPotential.ToArray());
                    break;

                case BoundaryConditionType.InfinityBoundary: // New case for infinity boundary
                    ApplyInfinityBoundary((InfinityBoundary)boundary, mesh, K, rhs);
                    break;

                case BoundaryConditionType.MaterialBoundary:
                    // Handle material boundary (for different materials)
                    break;
                case BoundaryConditionType.MeasurementBoundary:
                    // Handle material boundary (for different materials)
                    break;

                default:
                    throw new NotImplementedException($"The boundary {Enum.GetName(typeof(BoundaryConditionType), boundary.BoundaryConditionType)} is not implemented");
            }
        }
        // Method to apply the infinity boundary condition
        private void ApplyInfinityBoundary(InfinityBoundary boundary, IMesh mesh,
            IBigMatrix K, double[] rhs)
        {
            // Get unique nodes associated with the boundary faces
            INode[]? nodes = mesh.GetPrimitivesForBoundary(boundary)
                ?.SelectMany(f => f.Nodes)      // Get all nodes from the boundary faces
                .GroupBy(n => n)                // Group to remove duplicates
                .Select(g => g.First())         // Select the unique nodes
                .ToArray();

            if (nodes == null) return;

            // Log the start of the infinity boundary application
            Console.WriteLine("Applying infinity boundary condition...");

            foreach (INode node in nodes)
            {
                // Multiply global index by the number of degrees of freedom (DOF)
                int globalIndex = mesh.GetGlobalIndexForNode(node.Identifier) * _FieldDOFInfo.NDegreesOfFreedom;

                // Modify the global matrix (K) and the right-hand side vector (rhs)
                // for the infinity boundary to allow natural decay of the magnetic vector potential
                for (int i = 0; i < _FieldDOFInfo.NDegreesOfFreedom; i++)  // Assuming 3 DOF per node (x, y, z)
                {
                    // Modify the global stiffness matrix K by adding a small value to the diagonal
                    K[globalIndex + i, globalIndex + i] += boundary.SmallValue;

                    // Ensure the RHS contribution for this boundary is zero
                    rhs[globalIndex + i] = 0;
                }
            }
        }

    }
}
