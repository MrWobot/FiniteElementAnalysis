using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Fields;
using FiniteElementAnalysis.Results;
using Core.FileSystem;
using FiniteElementAnalysis.SourceRegions;
using Core.Pool;
using FiniteElementAnalysis.Mesh.Tetrahedral;
using FiniteElementAnalysis.Mesh;
using Core.Maths.Matrices;
using FiniteElementAnalysis.Boundaries.Electrostatic;

namespace FiniteElementAnalysis.Solvers
{
    public class ElectrostaticsSolver : ScalarSolver<ElectrostaticsResult>
    {
        public ElectrostaticsSolver() : base(FieldOperationType.Gradient)
        {
        }

        public override double GetK(Volume volume)
        {
            return ((ElectrostaticsVolume)volume).TotalPermittivity;
        }

        protected override void ApplyBoundaryToGlobal(Boundary boundary, TetrahedralMesh mesh,
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

        private static void ApplyFixedPotentialDirichletBoundary(FixedPotentialDirichletBoundary boundary, TetrahedralMesh mesh,
            IBigMatrix K, double[] rhs)
        {
            Dictionary<int, int> mapNodeToGlobalIndex = mesh.MapNodeIdentifierToGlobalIndex;
            Node[]? nodes = mesh.GetFacesForBoundary(boundary)?.SelectMany(f => f.Nodes).GroupBy(n => n).Select(g => g.First()).ToArray();
            if (nodes == null) throw new Exception($"Boundary '{boundary.Name}' has no associated faces or nodes.");
            foreach (Node node in nodes)
            {
                int nodeIndex = mapNodeToGlobalIndex[node.Identifier];
                FixValueInUnknowns(K, rhs, nodeIndex, boundary.Potential);
            }
        }

        private static void ApplyFixedNormalElectricFieldNeumannBoundary(FixedNormalElectricFieldNeumannBoundary boundary,
            TetrahedralMesh mesh, double[] rhs)
        {
            Dictionary<int, int> mapNodeToGlobalIndex = mesh.MapNodeIdentifierToGlobalIndex;
            BoundaryFace[]? faces = mesh.GetFacesForBoundary(boundary);
            if (faces == null) throw new Exception($"Boundary '{boundary.Name}' has no associated faces.");
            foreach (BoundaryFace face in faces)
            {
                if (face.Elements.Length > 1) throw new Exception("Face with Normal Electric Field Neumann Boundary cannot belong to multiple elements");
                double area = face.Area;
                double nodeContribution = (area / 3.0) * boundary.VoltsPerMeter
                    * ((ElectrostaticsVolume)face.Elements[0].VolumeIsAPartOf).TotalPermittivity;
                foreach (Node node in face.Nodes)
                {
                    int nodeIndex = mapNodeToGlobalIndex[node.Identifier];
                    rhs[nodeIndex] += nodeContribution;
                }
            }
        }

        private static void ApplyFixedSurfaceChargeDensityNeumannBoundary(FixedSurfaceChargeDensityNeumannBoundary boundary,
            TetrahedralMesh mesh, double[] rhs)
        {
            Dictionary<int, int> mapNodeToGlobalIndex = mesh.MapNodeIdentifierToGlobalIndex;
            BoundaryFace[]? faces = mesh.GetFacesForBoundary(boundary);
            if (faces == null) throw new Exception($"Boundary '{boundary.Name}' has no associated faces.");
            foreach (BoundaryFace face in faces)
            {
                double area = face.Area;
                double nodeContribution = (area / 3.0) * boundary.ChargeDensityCoulombsPerMeterSquared;
                foreach (Node node in face.Nodes)
                {
                    int nodeIndex = mapNodeToGlobalIndex[node.Identifier];
                    rhs[nodeIndex] += nodeContribution;
                }
            }
        }


        private static void ApplyFloatingPotentialBoundary(FloatingPotentialBoundary boundary,
            TetrahedralMesh mesh, IBigMatrix K, double[] rhs)
        {
            if (!boundary.IndicesHaveBeenAssigned || boundary.IndicesAssigned!.Length != 1)
                throw new InvalidOperationException("FloatingPotentialBoundary must have one assigned index before application.");

            int lambdaIndex = boundary.IndicesAssigned[0];
            var mapNodeToGlobalIndex = mesh.MapNodeIdentifierToGlobalIndex;
            var faces = mesh.GetFacesForBoundary(boundary);
            if (faces == null) throw new Exception($"Boundary '{boundary.Name}' has no associated faces.");

            var boundaryNodes = faces.SelectMany(f => f.Nodes).Distinct().ToArray();
            if (boundaryNodes.Length == 0) throw new Exception($"Boundary '{boundary.Name}' has no associated nodes.");

            double coeff = 1.0 / boundaryNodes.Length;
            foreach (Node node in boundaryNodes)
            {
                int nodeIndex = mapNodeToGlobalIndex[node.Identifier];
                K[nodeIndex, lambdaIndex] += coeff;
                K[lambdaIndex, nodeIndex] += coeff;
            }

            rhs[lambdaIndex] = boundary.Potential;
        }


        public override ElectrostaticsResult Solve(TetrahedralMesh mesh, WorkingDirectoryManager workingDirectoryManager,
            string operationIdentifier = "default", DelegateApplySourceRegion[]? applySourceRegion_s = null,
            SolverMethod solverMethod = SolverMethod.BlockMatrixInversionGpuOnly,
            CompositeProgressHandler? progressHandler = null,
            FileCachedItem<CoreSolverResult>? cachedSolverResult = null,
            bool useCachedSolverResults = false)
        {
            CoreSolverResult coreResult = base._Solve(mesh, workingDirectoryManager, operationIdentifier, applySourceRegion_s,
                solverMethod, progressHandler, cachedSolverResult, useCachedSolverResults);
            return new ElectrostaticsResult(mesh, coreResult);
        }
    }
}
