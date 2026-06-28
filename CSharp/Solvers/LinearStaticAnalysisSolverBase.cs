using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Fields;
using Core.FileSystem;
using FiniteElementAnalysis.SourceRegions;
using Core.Pool;
using Core.Maths.Matrices;
using Core.Maths;
using Logging;
using FiniteElementAnalysis.Boundaries.Statics;
using Core.Maths.Tensors;
using Core.Maths.Vectors;
using Core.Maths.IterativeSolvers.NewtonRaphson;
using System.Threading;
using Core.Maths.Tolerances;
using FiniteElementAnalysis.Results.ThreeD;
using FiniteElementAnalysis.Results;
using FiniteElementAnalysis.Solvers.Bases;
using FiniteElementAnalysis.Mesh.Interfaces;
using Core.Exceptions;

namespace FiniteElementAnalysis.Solvers
{
    public abstract class LinearStaticAnalysisSolverBase : SolverBaseSingleComponent<LinearStaticAnalysisResult3D>
    {
        protected LinearStaticAnalysisSolverBase(FieldDOFInfo fieldDOFInfo) : base(fieldDOFInfo)
        {
        }

        public override LinearStaticAnalysisResult3D Solve(
            IMesh mesh,
            WorkingDirectoryManager workingDirectoryManager,
            string operationIdentifier = "default",
            DelegateApplySourceRegion[]? applySourceRegion_s = null,
            SolverMethod solverMethod = SolverMethod.BlockMatrixInversionGpuOnly,
            CompositeProgressHandler? progressHandler = null,
            FileCachedItem<CoreSolverResult>? cachedSolverResult = null,
            bool useCachedSolverResults = false)
        {
            CoreSolverResult coreResult = _Solve(mesh, workingDirectoryManager, operationIdentifier, applySourceRegion_s, solverMethod,
                progressHandler, cachedSolverResult, useCachedSolverResults);
            return new LinearStaticAnalysisResult3D(mesh, coreResult);
        }
        /// <returns></returns>
        public LinearStaticAnalysisResult3D SolveNonLinearIterative(
            IMesh mesh,
            WorkingDirectoryManager workingDirectoryManager,
            NewtonRaphsonStoppingParametersMatrixContextualized newtonRaphsonStoppingParameters,
            AbsoluteTolerancesVector absoluteTolerances,
            CancellationToken cancellationToken,
            out NewtonRaphsonMatrixSolutionWithEvaluatedTolerances? nrSolution,
            SolverMethod solverMethod = SolverMethod.BlockMatrixInversionGpuOnly,
            string operationIdentifier = "default",
            DelegateApplySourceRegion[]? applySourceRegion_s = null,
            CompositeProgressHandler? progressHandler = null,
            FileCachedItem<CoreSolverResult>? cachedSolverResult = null,
            bool useCachedSolverResults = false)
        {
            StandardProgressHandler? standardProgressHandler = null;
            if (progressHandler != null)
            {
                standardProgressHandler = new StandardProgressHandler();
                progressHandler.AddChild(standardProgressHandler);
            }
            using (IterativeSolveHandle iterativeSolveHandle =
                SolveNonLinearIterativeNoTensorReuse(mesh, workingDirectoryManager,
                operationIdentifier, applySourceRegion_s,
                solverMethod, createProgressHandlers: true))
            {

                iterativeSolveHandle.DoStamp(out double[] rhs, out IBigMatrix K);
                double[] f_init = rhs;//Force vector from the last iteration
                LinearStaticAnalysisResult3D? currentResult = null;
                int nIteration = 0;
                nrSolution =
                    NewtonRaphsonMatrixSolver.Solve(f_init,
                    (
                    out double[] residual,
                    out double[] xAtEndOfIteration,
                    CancellationToken cancellationToken
                    ) =>
                    {
                        CoreSolverResult coreSolverResult = iterativeSolveHandle.DoSolve()!;
                        currentResult = new LinearStaticAnalysisResult3D(mesh, coreSolverResult);
                        currentResult.DisplaceMesh();
                        xAtEndOfIteration = coreSolverResult!.UnknownsVector;
                        //Do next stamp to retrieve f_ext early
                        iterativeSolveHandle.DoStamp(out double[] rhs, out IBigMatrix K);
                        double[] f_ext = rhs;//Forces calculated from displacements.
                        residual = VectorHelper.Subtract(f_ext, f_init);

                        f_init = f_ext;
                        if (standardProgressHandler != null)
                        {
                            double proportionComplete = 1.0 - Math.Exp(-0.3 * ++nIteration);
                            standardProgressHandler.Set(proportionComplete);
                        }
                    },
                    newtonRaphsonStoppingParameters,
                    absoluteTolerances,
                    cancellationToken
                );
                if (nrSolution == null)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        throw new Exception("Something went very wrong");
                    }
                    throw new OperationCanceledException();
                }
                if (currentResult == null)
                {
                    throw new Exception("Something went very wrong");
                }
                if (standardProgressHandler != null)
                {
                    standardProgressHandler.Set(1);
                }
                return currentResult;
            }
        }
        // Apply boundary conditions to the global matrix K and rhs vector
        protected override void ApplyBoundaryToGlobal(Boundary boundary, IMesh mesh,
            IBigMatrix K, double[] rhs, string operationIdentifier)
        {
            switch (boundary.BoundaryConditionType)
            {
                case BoundaryConditionType.FreeBoundary:
                    break;

                case BoundaryConditionType.FixedDisplacementDirichletBoundary:
                    {
                        //Nodes are fixed entirely (no translation or rotation), or partially fixed (certain degrees of freedom restricted).
                        //Example: A fixed support where displacement is zero.
                        double[] boundaryNodalScalarValue = new double[_FieldDOFInfo.NDegreesOfFreedom];
                        ApplyDirichletBoundary(boundary, mesh, K, rhs, boundaryNodalScalarValue);
                        break;
                    }
                case BoundaryConditionType.FixedDisplacementSpecificDirection:
                    //For rollers
                    throw new NotImplementedException();
                case BoundaryConditionType.PrescribedDisplacementDirichletBoundary:
                    {
                        //Specific, known displacements applied at certain nodes.
                        //Example: Applying a known displacement to simulate a controlled deformation.
                        var fixedDisplacementDirichletBoundary = (PrescribedDisplacementDirichletBoundary)boundary;
                        ApplyDirichletBoundary(boundary, mesh, K, rhs, fixedDisplacementDirichletBoundary.Translations);
                        break;
                    }
                case BoundaryConditionType.FixedNormalForceNeumannBoundary:
                    ApplyFixedNormalForceNeumannBoundary((FixedNormalForceNeumannBoundary)boundary, mesh, rhs);
                    break;
                case BoundaryConditionType.FixedDirectionalForceNeumannBoundary:
                    ApplySurfaceForceNeumannBoundary((SurfaceForceNeumannBoundaryBase)boundary, mesh, rhs);
                    break;
                case BoundaryConditionType.SurfaceTractionNeumannBoundary:
                    ApplySurfaceTractionNeumannBoundary((SurfaceTractionNeumannBoundaryBase)boundary, mesh, rhs);
                    break;

                case BoundaryConditionType.PressureNeumannBoundary:
                    //Specified forces or pressures applied directly to element faces.
                    //Example: Applying uniform or non-uniform pressures to simulate external loads.
                    ApplyPressureNeumannBoundary((PressureNeumannBoundary)boundary, mesh, rhs);
                    break;

                case BoundaryConditionType.BodyForceNeumannBoundary:
                    //Forces applied through the volume of the material.
                    //Example: Gravitational acceleration applied uniformly to the entire mesh.
                    throw new NotImplementedException();

                default:
                    throw new NotImplementedException($"The boundary {Enum.GetName(typeof(BoundaryConditionType), boundary.BoundaryConditionType)} is not implemented");
            }
        }
        protected override double[][] ScaleBTransposeByK(double[][] bTranspose, Volume volume)
        {
            double[][] D = ((StaticLinearElasticVolume)volume).ElasticityMatrix;
            return MatrixHelper.Multiply(bTranspose, D);
        }
        private void ApplyPressureNeumannBoundary(
    PressureNeumannBoundary boundary, IMesh mesh, double[] rhs)
        {
            IBoundaryPrimitive[]? primitives = mesh.GetPrimitivesForBoundary(boundary);
            if (primitives == null || primitives.Length == 0)
                throw new InvalidOperationException("No primitives found for boundary.");

            foreach (IBoundaryPrimitive primitive in primitives)
            {
                double thicknessIntegral = primitive.Nodes
                    .Sum(n => mesh.GetThickness(n.Position)) / primitive.Nodes.Length;
                double measure = primitive.Measure * thicknessIntegral;
                double[] unitNormal = primitive.UnitNormal;
                int n = primitive.Nodes.Length;

                foreach (INode node in primitive.Nodes)
                {
                    int globalNodeIndex = mesh.MapNodeIndexToGlobalIndex[node.Index];
                    for (int dof = 0; dof < _FieldDOFInfo.NDegreesOfFreedom; dof++)
                    {
                        rhs[globalNodeIndex * _FieldDOFInfo.NDegreesOfFreedom + dof]
                            += boundary.Pressure * unitNormal[dof] * measure / n;
                    }
                }
            }
        }
        private void ApplyFixedNormalForceNeumannBoundary(
            FixedNormalForceNeumannBoundary boundary, IMesh mesh, double[] rhs)
        {
            IBoundaryPrimitive[]? primitives = mesh.GetPrimitivesForBoundary(boundary);
            if (primitives == null || primitives.Length == 0)
                throw new InvalidOperationException("No primitives found for boundary.");

            // Total effective area including thickness
            double totalArea = primitives.Sum(p =>
            {
                double thicknessIntegral = p.Nodes
                    .Sum(n => mesh.GetThickness(n.Position)) / p.Nodes.Length;
                return p.Measure * thicknessIntegral;
            });

            if (totalArea <= 0)
                throw new InvalidOperationException("Total area must be greater than zero.");

            // Convert total normal force to pressure magnitude
            double pressureMagnitude = boundary.NormalForce / totalArea;

            foreach (IBoundaryPrimitive primitive in primitives)
            {
                double thicknessIntegral = primitive.Nodes
                    .Sum(n => mesh.GetThickness(n.Position)) / primitive.Nodes.Length;
                double measure = primitive.Measure * thicknessIntegral;

                // Unit normal from primitive — dimension agnostic
                double[] unitNormal = primitive.UnitNormal;
                int n = primitive.Nodes.Length;

                foreach (INode node in primitive.Nodes)
                {
                    int globalNodeIndex = mesh.MapNodeIndexToGlobalIndex[node.Index];
                    for (int dof = 0; dof < _FieldDOFInfo.NDegreesOfFreedom; dof++)
                    {
                        rhs[globalNodeIndex * _FieldDOFInfo.NDegreesOfFreedom + dof]
                            += pressureMagnitude * unitNormal[dof] * measure / n;
                    }
                }
            }
        }
        protected abstract void ApplySurfaceTractionNeumannBoundary(
            SurfaceTractionNeumannBoundaryBase boundary, IMesh mesh, double[] rhs);
        protected abstract void ApplySurfaceForceNeumannBoundary(
            SurfaceForceNeumannBoundaryBase boundary, IMesh mesh, double[] rhs);

    }
}