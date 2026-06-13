using Core.Maths;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Boundaries.Thermal;
using FiniteElementAnalysis.Fields;
using FiniteElementAnalysis.Results;
using Core.FileSystem;
using FiniteElementAnalysis.SourceRegions;
using Core.Pool;
using FiniteElementAnalysis.Mesh.Tetrahedral;
using FiniteElementAnalysis.Mesh;
using Core.Maths.Matrices;

namespace FiniteElementAnalysis.Solvers
{
    //https://www.comsol.com/multiphysics/finite-element-method
    //This one explains overlap of basis functions
    //https://www.researchgate.net/publication/382609774_Finite_element_solution_of_heat_conduction_in_complex_3D_geometries
    //The heat conduction equation, also known as heat diffusion equation or fouriers law.
    public class HeatConductionSolver : ScalarSolver<HeatConductionResult>
    {
        public HeatConductionSolver() : base(FieldOperationType.Gradient)
        {
        }

        public override double GetK(Volume volume) {
            return ((StaticHeatVolume)volume).ThermalConductivity;
        }
        protected override void ApplyBoundaryToGlobal(Boundary boundary, TetrahedralMesh mesh,
            IBigMatrix K, double[] rhs, string operationIdentifier)
        {
            switch (boundary.BoundaryConditionType)
            {
                case BoundaryConditionType.FixedTemperatureDirichletBoundary:
                    ApplyFixedTemperatureBoundary((FixedTemperatureDirichletBoundary)boundary,
                        mesh, K, rhs);
                    break;
                case BoundaryConditionType.FixedHeatFluxNeumannBoundary:
                    ApplyFixedHeatFluxBoundary(
                        (FixedHeatFluxNeumannBoundary)boundary, mesh, rhs);
                    break;
                case BoundaryConditionType.AdiabaticInsulatedBoundary:
                    break;
                case BoundaryConditionType.ConvectiveOrMixedRobinBoundary:
                    ApplyConvectiveOrMixedRobinBoundary((ConvectiveOrMixedRobinBoundary)boundary,
                        mesh, K,
                        rhs);
                    break;
                case BoundaryConditionType.RadiationBoundary:
                    ApplyRadiationBoundaryCondition((RadiationBoundary)boundary,
                        mesh, K,
                        rhs);
                    break;
                case BoundaryConditionType.MaterialBoundary:
                    break;
                default:
                    throw new NotImplementedException($"The boundary {Enum.GetName(typeof(BoundaryConditionType), boundary.BoundaryConditionType)} is not implemented");
            }
        }
        private void ApplyConvectiveOrMixedRobinBoundary(
            ConvectiveOrMixedRobinBoundary boundary,
            TetrahedralMesh mesh,
            IBigMatrix K,
            double[] rhs)
        {

            Dictionary<int, int> mapNodeToGlobalIndex = mesh.MapNodeIdentifierToGlobalIndex;
            BoundaryFace[]? faces = mesh.GetFacesForBoundary(boundary);
            if (faces == null) return;

            double h = boundary.ConvectiveHeatTransferCoefficientH;
            double T_infinity = boundary.AmbientTemperature;

            foreach (BoundaryFace face in faces)
            {
                Node a = face.Nodes[0];
                Node b = face.Nodes[1];
                Node c = face.Nodes[2];
                double area = GeometryHelper.TriangleArea(a.X, a.Y, a.Z,
                    b.X, b.Y, b.Z, c.X, c.Y, c.Z);

                // Contribution to the global stiffness matrix K
                double contribution = (h * area) / 12.0;  // Divided by 12 because each pair of nodes contributes 1/6th

                // Update the stiffness matrix K for the nodes a, b, c
                int aIndex = mapNodeToGlobalIndex[a.Identifier];
                int bIndex = mapNodeToGlobalIndex[b.Identifier];
                int cIndex = mapNodeToGlobalIndex[c.Identifier];
                K[aIndex,aIndex] += 2 * contribution; // Diagonal term
                K[aIndex,bIndex] += contribution;
                K[aIndex,cIndex] += contribution;

                K[bIndex,aIndex] += contribution;
                K[bIndex,bIndex] += 2 * contribution; // Diagonal term
                K[bIndex,cIndex] += contribution;

                K[cIndex,aIndex] += contribution;
                K[cIndex,bIndex] += contribution;
                K[cIndex,cIndex] += 2 * contribution; // Diagonal term

                // Contribution to the global RHS vector
                double rhsContribution = (h * T_infinity * area) / 3.0; // Each node gets 1/3 of the total contribution

                rhs[aIndex] += rhsContribution;
                rhs[bIndex] += rhsContribution;
                rhs[cIndex] += rhsContribution;
            }
        }
        private static bool ApplyRadiationBoundaryCondition(
    RadiationBoundary boundary,
    TetrahedralMesh mesh,
    IBigMatrix K,
    double[] rhs)
        {
            Dictionary<int, int> mapNodeToGlobalIndex = mesh.MapNodeIdentifierToGlobalIndex;
            BoundaryFace[]? faces = mesh.GetFacesForBoundary(boundary);
            if (faces == null||faces.Length<1) return false;

            double epsilon = boundary.EmissivityOfSurface;
            double sigma = 5.67e-8; // Stefan-Boltzmann constant in W/(m^2*K^4)
            double T_infinity = boundary.AmbientTemperature;

            foreach (BoundaryFace face in faces)
            {
                Node a = face.Nodes[0];
                Node b = face.Nodes[1];
                Node c = face.Nodes[2];

                // Calculate the area of the triangular face
                double area = GeometryHelper.TriangleArea(a.X, a.Y, a.Z, b.X, b.Y, b.Z, c.X, c.Y, c.Z);

                // Approximation or linearization for the stiffness matrix contribution
                // Linearization might involve assuming a reference temperature T_ref
                double T_ref = (a.ScalarValue + b.ScalarValue + c.ScalarValue/*value is temperature*/) / 3.0; // Average temperature (simplified)
                double contribution = epsilon * sigma * area * Math.Pow(T_ref, 3) / 12.0;
                int aIndex = mapNodeToGlobalIndex[a.Identifier];
                int bIndex = mapNodeToGlobalIndex[b.Identifier];
                int cIndex = mapNodeToGlobalIndex[c.Identifier];
                // Update the stiffness matrix K for the nodes a, b, c
                K[aIndex,aIndex] += 2 * contribution; // Diagonal term
                K[aIndex,bIndex] += contribution;
                K[aIndex,cIndex] += contribution;

                K[bIndex,aIndex] += contribution;
                K[bIndex,bIndex] += 2 * contribution; // Diagonal term
                K[bIndex,cIndex] += contribution;

                K[cIndex,aIndex] += contribution;
                K[cIndex,bIndex] += contribution;
                K[cIndex,cIndex] += 2 * contribution; // Diagonal term

                // Contribution to the global RHS vector
                double rhsContribution = epsilon * sigma * T_infinity * T_infinity * T_infinity * T_infinity * area / 3.0; // Simplified

                rhs[aIndex] -= rhsContribution;
                rhs[bIndex] -= rhsContribution;
                rhs[cIndex] -= rhsContribution;
            }
            return true;
        }
        private static void ApplyFixedTemperatureBoundary(
            FixedTemperatureDirichletBoundary boundary, TetrahedralMesh mesh,
            IBigMatrix K, double[]rhs
        )
        {

            Dictionary<int, int> mapNodeToGlobalIndex = mesh.MapNodeIdentifierToGlobalIndex;
            Node[]? nodes = mesh.GetFacesForBoundary(boundary)
                ?.SelectMany(f => f.Nodes)
                .GroupBy(n => n)
                .Select(g => g.First())
                .ToArray();
            if (nodes == null) return;
            foreach (Node node in nodes)
            {
                int nodeIndex = mapNodeToGlobalIndex[node.Identifier];
                FixValueInUnknowns(K, rhs, nodeIndex, boundary.TemperatureK);
            }
        }
        private static void ApplyFixedHeatFluxBoundary(
            FixedHeatFluxNeumannBoundary boundary, 
            TetrahedralMesh mesh,
            double[] rhs)
        {

            Dictionary<int, int> mapNodeToGlobalIndex = mesh.MapNodeIdentifierToGlobalIndex;
            BoundaryFace[]? faces = mesh.GetFacesForBoundary(boundary);
            if(faces == null) return;
            foreach (BoundaryFace face in faces)
            {
                double area = face.Area;
                // Calculate the contribution to the RHS for each node in the face
                double nodeContribution = (area / 3) * boundary.HeatFluxWattsPerMeterSquare;
                foreach (Node node in face.Nodes)
                {
                    // If the heat flux convention in your system is positive for heat entering the domain,
                    // you might want to subtract the contribution instead.
                    int nodeIndex = mapNodeToGlobalIndex[node.Identifier];
                    rhs[nodeIndex] += nodeContribution;
                }
            }

        }

        public override HeatConductionResult Solve(TetrahedralMesh mesh, WorkingDirectoryManager workingDirectoryManager, string operationIdentifier = "default", DelegateApplySourceRegion[]? applySourceRegion_s = null, SolverMethod solverMethod = SolverMethod.BlockMatrixInversionGpuOnly, CompositeProgressHandler? progressHandler = null, FileCachedItem<CoreSolverResult>? cachedSolverResult = null, bool useCachedSolverResults = false)
        {
            CoreSolverResult coreResult = base._Solve(mesh, workingDirectoryManager, operationIdentifier, applySourceRegion_s,
                solverMethod, progressHandler, cachedSolverResult, useCachedSolverResults);
            return new HeatConductionResult(mesh, coreResult);

        }
    }
}