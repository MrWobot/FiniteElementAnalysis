using Core.Maths;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Boundaries.Thermal;
using FiniteElementAnalysis.Fields;
using Core.FileSystem;
using FiniteElementAnalysis.SourceRegions;
using Core.Pool;
using Core.Maths.Matrices;
using FiniteElementAnalysis.Results.ThreeD;
using FiniteElementAnalysis.Results;
using FiniteElementAnalysis.Solvers.Bases;
using FiniteElementAnalysis.Mesh.Interfaces;
using Core.Exceptions;

namespace FiniteElementAnalysis.Solvers
{
    //https://www.comsol.com/multiphysics/finite-element-method
    //This one explains overlap of basis functions
    //https://www.researchgate.net/publication/382609774_Finite_element_solution_of_heat_conduction_in_complex_3D_geometries
    //The heat conduction equation, also known as heat diffusion equation or fouriers law.
    public class HeatConductionSolver : ScalarSolver<HeatConductionResult3D>
    {
        public HeatConductionSolver() : base(FieldOperationType.Gradient)
        {
        }

        public override double GetK(Volume volume)
        {
            return ((StaticHeatVolume)volume).ThermalConductivity;
        }
        protected override void ApplyBoundaryToGlobal(Boundary boundary, IMesh mesh,
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
        private static void ApplyConvectiveOrMixedRobinBoundary(
    ConvectiveOrMixedRobinBoundary boundary,
    IMesh mesh,
    IBigMatrix K,
    double[] rhs)
        {
            throw new NeverUsedOrDebuggedException();
            Dictionary<int, int> mapNodeToGlobalIndex = mesh.MapNodeIndexToGlobalIndex;
            IBoundaryPrimitive[]? primitives = mesh.GetPrimitivesForBoundary(boundary);
            if (primitives == null) return;
            double h = boundary.ConvectiveHeatTransferCoefficientH;
            double T_infinity = boundary.AmbientTemperature;
            foreach (IBoundaryPrimitive primitive in primitives)
            {
                double thicknessIntegral = primitive.Nodes
                    .Sum(n => mesh.GetThickness(n.Position)) / primitive.Nodes.Length;
                double measure = primitive.Measure * thicknessIntegral;
                int n = primitive.Nodes.Length;

                // Contribution to global stiffness matrix K
                // Diagonal: h * measure / 6, Off-diagonal: h * measure / 12
                // General form: h * measure * (1 + delta_ij) / (n * (n + 1))
                double offDiagonal = h * measure / (n * (n + 1));
                double diagonal = 2 * offDiagonal;

                for (int i = 0; i < n; i++)
                {
                    int iIndex = mapNodeToGlobalIndex[primitive.Nodes[i].Index];
                    for (int j = 0; j < n; j++)
                    {
                        int jIndex = mapNodeToGlobalIndex[primitive.Nodes[j].Index];
                        K[iIndex, jIndex] += i == j ? diagonal : offDiagonal;
                    }
                    // RHS contribution
                    rhs[iIndex] += h * T_infinity * measure / n;
                }
            }
        }
        private static bool ApplyRadiationBoundaryCondition(
    RadiationBoundary boundary,
    IMesh mesh,
    IBigMatrix K,
    double[] rhs)
        {
            throw new NeverUsedOrDebuggedException();
            Dictionary<int, int> mapNodeToGlobalIndex = mesh.MapNodeIndexToGlobalIndex;
            IBoundaryPrimitive[]? primitives = mesh.GetPrimitivesForBoundary(boundary);
            if (primitives == null || primitives.Length < 1) return false;

            double epsilon = boundary.EmissivityOfSurface;
            double sigma = 5.67e-8; // Stefan-Boltzmann constant W/(m²K⁴)
            double T_infinity = boundary.AmbientTemperature;

            foreach (IBoundaryPrimitive primitive in primitives)
            {
                double thicknessIntegral = primitive.Nodes
                    .Sum(n => mesh.GetThickness(n.Position)) / primitive.Nodes.Length;
                double measure = primitive.Measure * thicknessIntegral;
                int n = primitive.Nodes.Length;

                // Linearise T⁴ around reference temperature T_ref
                // T⁴ ≈ 4×T_ref³×T - 3×T_ref⁴
                // Effective h = ε × σ × 4 × T_ref³
                double T_ref = primitive.Nodes.Sum(node => node.Values[0]) / n;
                double T_ref3 = Math.Pow(T_ref, 3);
                double T_ref4 = T_ref3 * T_ref;
                double h_rad = epsilon * sigma * 4.0 * T_ref3;

                // K contribution — same consistent boundary mass matrix as Robin
                // Diagonal: h_rad × measure / 6
                // Off-diagonal: h_rad × measure / 12
                double offDiagonal = h_rad * measure / (n * (n + 1));
                double diagonal = 2.0 * offDiagonal;

                for (int i = 0; i < n; i++)
                {
                    int iIndex = mapNodeToGlobalIndex[primitive.Nodes[i].Index];
                    for (int j = 0; j < n; j++)
                    {
                        int jIndex = mapNodeToGlobalIndex[primitive.Nodes[j].Index];
                        K[iIndex, jIndex] += i == j ? diagonal : offDiagonal;
                    }
                    // RHS — complete linearisation includes both T∞⁴ and -3×T_ref⁴ terms
                    // q_rad = ε × σ × (T∞⁴ - T⁴) ≈ ε × σ × (T∞⁴ - 4×T_ref³×T + 3×T_ref⁴)
                    // The 4×T_ref³×T term goes into K, remainder into RHS
                    double rhsContribution = epsilon * sigma
                        * (T_infinity * T_infinity * T_infinity * T_infinity
                        + 3.0 * T_ref4)
                        * measure / n;
                    rhs[iIndex] -= rhsContribution;
                }
            }
            return true;
        }
        private static void ApplyFixedTemperatureBoundary(
            FixedTemperatureDirichletBoundary boundary, IMesh mesh,
            IBigMatrix K, double[] rhs
        )
        {

            Dictionary<int, int> mapNodeToGlobalIndex = mesh.MapNodeIndexToGlobalIndex;
            INode[]? nodes = mesh.GetPrimitivesForBoundary(boundary)
                ?.SelectMany(f => f.Nodes)
                .GroupBy(n => n)
                .Select(g => g.First())
                .ToArray();
            if (nodes == null) return;
            foreach (INode node in nodes)
            {
                int nodeIndex = mapNodeToGlobalIndex[node.Index];
                FixValueInUnknowns(K, rhs, nodeIndex, boundary.TemperatureK);
            }
        }
        private static void ApplyFixedHeatFluxBoundary(
            FixedHeatFluxNeumannBoundary boundary,
            IMesh mesh,
            double[] rhs)
        {

            Dictionary<int, int> mapNodeToGlobalIndex = mesh.MapNodeIndexToGlobalIndex;
            IBoundaryPrimitive[]? faces = mesh.GetPrimitivesForBoundary(boundary);
            if (faces == null) return;
            foreach (IBoundaryPrimitive face in faces)
            {
                // Calculate the contribution to the RHS for each node in the face
                double thicknessIntegral = face.Nodes
                    .Sum(n => mesh.GetThickness(n.Position)) / face.Nodes.Length;
                double nodeContribution = face.Measure * thicknessIntegral / face.Nodes.Length
                    * boundary.HeatFluxWattsPerMeterSquare; 
                foreach (INode node in face.Nodes)
                {
                    // If the heat flux convention in your system is positive for heat entering the domain,
                    // you might want to subtract the contribution instead.
                    int nodeIndex = mapNodeToGlobalIndex[node.Index];
                    rhs[nodeIndex] += nodeContribution;
                }
            }

        }

        public override HeatConductionResult3D Solve(IMesh mesh, WorkingDirectoryManager workingDirectoryManager, string operationIdentifier = "default", DelegateApplySourceRegion[]? applySourceRegion_s = null, SolverMethod solverMethod = SolverMethod.BlockMatrixInversionGpuOnly, CompositeProgressHandler? progressHandler = null, FileCachedItem<CoreSolverResult>? cachedSolverResult = null, bool useCachedSolverResults = false)
        {
            CoreSolverResult coreResult = _Solve(mesh, workingDirectoryManager, operationIdentifier, applySourceRegion_s,
                solverMethod, progressHandler, cachedSolverResult, useCachedSolverResults);
            return new HeatConductionResult3D(mesh, coreResult);

        }
    }
}