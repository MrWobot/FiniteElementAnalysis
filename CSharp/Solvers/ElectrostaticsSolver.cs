using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Fields;
using Core.FileSystem;
using FiniteElementAnalysis.SourceRegions;
using Core.Pool;
using Core.Maths.Matrices;
using FiniteElementAnalysis.Boundaries.Electrostatic;
using FiniteElementAnalysis.Results.ThreeD;
using FiniteElementAnalysis.Results;
using FiniteElementAnalysis.Solvers.Bases;
using FiniteElementAnalysis.Mesh.Interfaces;

namespace FiniteElementAnalysis.Solvers
{
    public class ElectrostaticsSolver : ScalarSolver<ElectrostaticsResult3D>
    {
        public ElectrostaticsSolver() : base(FieldOperationType.Gradient)
        {
        }

        public override double GetK(Volume volume)
        {
            return ((ElectrostaticsVolume)volume).TotalPermittivity;
        }

        protected override void ApplyBoundaryToGlobal(Boundary boundary, IMesh mesh,
            IBigMatrix K, double[] rhs, string operationIdentifier)
        {
            switch (boundary.BoundaryConditionType)
            {
                case BoundaryConditionType.FixedPotentialDirichletBoundary:
                    ApplyFixedPotentialDirichletBoundary((FixedPotentialDirichletBoundary)boundary, mesh, K, rhs);
                    break;
                case BoundaryConditionType.FixedNormalElectricFieldNeumannBoundary:
                    ApplyFixedNormalElectricFieldNeumannBoundary((FixedNormalElectricFieldNeumannBoundary)boundary, mesh, rhs);
                    break;
                case BoundaryConditionType.FixedSurfaceChargeDensityNeumannBoundary:
                    ApplyFixedSurfaceChargeDensityNeumannBoundary((FixedSurfaceChargeDensityNeumannBoundary)boundary, mesh, rhs);
                    break;
                case BoundaryConditionType.FloatingPotentialBoundary:
                    ApplyFloatingPotentialBoundary((FloatingPotentialBoundary)boundary, mesh, K, rhs);
                    break;
                case BoundaryConditionType.AdiabaticInsulatedBoundary:
                case BoundaryConditionType.MaterialBoundary:
                    break;
                default:
                    throw new NotImplementedException($"The boundary {Enum.GetName(typeof(BoundaryConditionType), boundary.BoundaryConditionType)} is not implemented");
            }
        }

        private static void ApplyFixedPotentialDirichletBoundary(
            FixedPotentialDirichletBoundary boundary,
            IMesh mesh,
            IBigMatrix K,
            double[] rhs
        )
        {
            Dictionary<int, int> mapNodeToGlobalIndex = mesh.MapNodeIndexToGlobalIndex;
            INode[]? nodes = mesh.GetPrimitivesForBoundary(boundary)?
                .SelectMany(p => p.Nodes)
                .GroupBy(n => n)
                .Select(g => g.First())
                .ToArray();
            if (nodes == null) throw new Exception($"Boundary '{boundary.Name}' has no associated faces or nodes.");
            foreach (INode node in nodes)
            {
                int nodeIndex = mapNodeToGlobalIndex[node.Index];
                FixValueInUnknowns(K, rhs, nodeIndex, boundary.Potential);
            }
        }

        private static void ApplyFixedNormalElectricFieldNeumannBoundary(FixedNormalElectricFieldNeumannBoundary boundary,
            IMesh mesh, double[] rhs)
        {
            Dictionary<int, int> mapNodeToGlobalIndex = mesh.MapNodeIndexToGlobalIndex;
            IBoundaryPrimitive[]? faces = mesh.GetPrimitivesForBoundary(boundary);
            if (faces == null) throw new Exception($"Boundary '{boundary.Name}' has no associated faces.");
            foreach (IBoundaryPrimitive primitive in faces)
            {
                if (primitive.Elements.Length > 1) throw new Exception("Face with Normal Electric Field Neumann Boundary cannot belong to multiple elements");
                double thicknessIntegral = primitive.Nodes
                 .Sum(n => mesh.GetThickness(n.Position)) / primitive.Nodes.Length;
                double nodeContribution = primitive.Measure * thicknessIntegral / primitive.Nodes.Length * boundary.VoltsPerMeter
                    * ((ElectrostaticsVolume)primitive.Elements[0].VolumeBelongsTo).TotalPermittivity;
                foreach (INode node in primitive.Nodes)
                {
                    int nodeIndex = mapNodeToGlobalIndex[node.Index];
                    rhs[nodeIndex] += nodeContribution;
                }
            }
        }

        private static void ApplyFixedSurfaceChargeDensityNeumannBoundary(FixedSurfaceChargeDensityNeumannBoundary boundary,
            IMesh mesh, double[] rhs)
        {
            Dictionary<int, int> mapNodeToGlobalIndex = mesh.MapNodeIndexToGlobalIndex;
            IBoundaryPrimitive[]? faces = mesh.GetPrimitivesForBoundary(boundary);
            if (faces == null) throw new Exception($"Boundary '{boundary.Name}' has no associated faces.");
            foreach (IBoundaryPrimitive primitive in faces)
            {
                double thicknessIntegral = primitive.Nodes
                 .Sum(n => mesh.GetThickness(n.Position)) / primitive.Nodes.Length;
                double nodeContribution = primitive.Measure * thicknessIntegral / primitive.Nodes.Length * boundary.ChargeDensityCoulombsPerMeterSquared;
                foreach (INode node in primitive.Nodes)
                {
                    int nodeIndex = mapNodeToGlobalIndex[node.Index];
                    rhs[nodeIndex] += nodeContribution;
                }
            }
        }


        private static void ApplyFloatingPotentialBoundary(FloatingPotentialBoundary boundary,
            IMesh mesh, IBigMatrix K, double[] rhs)
        {
            if (!boundary.IndicesHaveBeenAssigned || boundary.IndicesAssigned!.Length != 1)
                throw new InvalidOperationException("FloatingPotentialBoundary must have one assigned index before application.");

            int lambdaIndex = boundary.IndicesAssigned[0];
            var mapNodeToGlobalIndex = mesh.MapNodeIndexToGlobalIndex;
            var faces = mesh.GetPrimitivesForBoundary(boundary);
            if (faces == null) throw new Exception($"Boundary '{boundary.Name}' has no associated faces.");

            var boundaryNodes = faces.SelectMany(f => f.Nodes).Distinct().ToArray();
            if (boundaryNodes.Length == 0) throw new Exception($"Boundary '{boundary.Name}' has no associated nodes.");

            double coeff = 1.0 / boundaryNodes.Length;
            foreach (INode node in boundaryNodes)
            {
                int nodeIndex = mapNodeToGlobalIndex[node.Index];
                K[nodeIndex, lambdaIndex] += coeff;
                K[lambdaIndex, nodeIndex] += coeff;
            }

            rhs[lambdaIndex] = boundary.Potential;
        }


        public override ElectrostaticsResult3D Solve(IMesh mesh, WorkingDirectoryManager workingDirectoryManager,
            string operationIdentifier = "default", DelegateApplySourceRegion[]? applySourceRegion_s = null,
            SolverMethod solverMethod = SolverMethod.BlockMatrixInversionGpuOnly,
            CompositeProgressHandler? progressHandler = null,
            FileCachedItem<CoreSolverResult>? cachedSolverResult = null,
            bool useCachedSolverResults = false)
        {
            CoreSolverResult coreResult = _Solve(mesh, workingDirectoryManager, operationIdentifier, applySourceRegion_s,
                solverMethod, progressHandler, cachedSolverResult, useCachedSolverResults);
            return new ElectrostaticsResult3D(mesh, coreResult);
        }
    }
}
