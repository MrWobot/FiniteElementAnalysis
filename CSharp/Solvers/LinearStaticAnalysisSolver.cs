using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Fields;
using FiniteElementAnalysis.Results;
using Core.FileSystem;
using FiniteElementAnalysis.SourceRegions;
using Core.Pool;
using FiniteElementAnalysis.Mesh.Tetrahedral;
using FiniteElementAnalysis.Mesh;
using Core.Maths.Matrices;
using Core.Maths;
using Logging;
using FiniteElementAnalysis.Boundaries.Statics;
using Core.Maths.Tensors;
using Core.Maths.Vectors;
using Core.Maths.IterativeSolvers.NewtonRaphson;
using System.Threading;
using Core.Maths.Tolerances;

namespace FiniteElementAnalysis.Solvers
{
    public class LinearStaticAnalysisSolver : SolverBaseSingleComponent<LinearStaticAnalysisResult>
    {
        public LinearStaticAnalysisSolver() : base(new FieldDOFInfo(3, 6, FieldOperationType.StrainDisplacement))
        {
        }

        public override LinearStaticAnalysisResult Solve(
            TetrahedralMesh mesh, 
            WorkingDirectoryManager workingDirectoryManager, 
            string operationIdentifier = "default", 
            DelegateApplySourceRegion[]? applySourceRegion_s = null, 
            SolverMethod solverMethod = SolverMethod.BlockMatrixInversionGpuOnly, 
            CompositeProgressHandler? progressHandler = null, 
            FileCachedItem<CoreSolverResult>? cachedSolverResult = null, 
            bool useCachedSolverResults = false)
        {
            CoreSolverResult coreResult = base._Solve(mesh, workingDirectoryManager, operationIdentifier, applySourceRegion_s, solverMethod,
                progressHandler, cachedSolverResult, useCachedSolverResults);
            return new LinearStaticAnalysisResult(mesh, coreResult);
        }
        /// <returns></returns>
        public LinearStaticAnalysisResult SolveNonLinearIterative(
            TetrahedralMesh mesh,
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
            if (progressHandler != null) {
                standardProgressHandler = new StandardProgressHandler();
                progressHandler.AddChild(standardProgressHandler);
            }
            using (IterativeSolveHandle iterativeSolveHandle =
                base.SolveNonLinearIterativeNoTensorReuse(mesh, workingDirectoryManager,
                operationIdentifier, applySourceRegion_s,
                solverMethod, createProgressHandlers: true))
            {

                iterativeSolveHandle.DoStamp(out double[] rhs, out IBigMatrix K);
                double[] f_init = rhs;//Force vector from the last iteration
                LinearStaticAnalysisResult? currentResult = null;
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
                        currentResult = new LinearStaticAnalysisResult(mesh, coreSolverResult);
                        currentResult.DisplaceMesh();
                        xAtEndOfIteration = coreSolverResult!.UnknownsVector;
                        //Do next stamp to retrieve f_ext early
                        iterativeSolveHandle.DoStamp(out double[] rhs, out IBigMatrix K);
                        double[] f_ext = rhs;//Forces calculated from displacements.
                        residual = VectorHelper.Subtract(f_ext, f_init);

                        f_init = f_ext;
                        if (standardProgressHandler != null)
                        {
                            double proportionComplete = 1.0 - Math.Exp(-0.3 * (++nIteration));
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
        private void UpdateNodesFromDisplacements(TetrahedralMesh mesh, double[] displacements) {
            
        }
        // Apply boundary conditions to the global matrix K and rhs vector
        protected override void ApplyBoundaryToGlobal(Boundary boundary, TetrahedralMesh mesh,
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
                        double[] boundaryNodalScalarValue = new double[3];
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
                        double[] boundaryNodalScalarValue = new double[6];
                        Array.Copy(fixedDisplacementDirichletBoundary.Translations, 0, boundaryNodalScalarValue, 0, 3);
                        Array.Copy(fixedDisplacementDirichletBoundary.Rotations, 0, boundaryNodalScalarValue, 3, 3);
                        ApplyDirichletBoundary(boundary, mesh, K, rhs, boundaryNodalScalarValue);
                        break;
                    }
                case BoundaryConditionType.FixedNormalForceNeumannBoundary:
                    ApplyFixedNormalForceNeumannBoundary((FixedNormalForceNeumannBoundary)boundary, mesh, rhs);
                    break;
                case BoundaryConditionType.FixedDirectionalForceNeumannBoundary:
                    throw new Exception("Not verified this is correct");
                    ApplySurfaceForceNeumannBoundary((SurfaceForceNeumannBoundary) boundary, mesh, rhs);
                case BoundaryConditionType.SurfaceTractionNeumannBoundary:
                    throw new Exception("Not verified this is correct");
                    ApplySurfaceTractionNeumannBoundary((SurfaceTractionNeumannBoundary)boundary, mesh, rhs);
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
            PressureNeumannBoundary boundary, TetrahedralMesh mesh, double[] rhs)
        {
            BoundaryFace[] faces = mesh.GetFacesForBoundary(boundary)!;

            foreach (var face in faces)
            {
                double area = face.Area;
                Vector3D unitNormal = face.Normal.Normalize();

                // Compute force per node (Pressure * Area / 3)
                Vector3D forcePerNode = unitNormal.Scale(boundary.Pressure * area / 3.0);

                // Distribute the normal force to each node
                foreach (Node node in face.Nodes)
                {
                    int globalNodeIndex = mesh.MapNodeIdentifierToGlobalIndex[node.Identifier];

                    // Apply the force in the normal direction only
                    rhs[globalNodeIndex * _FieldDOFInfo.NDegreesOfFreedom + 0] += forcePerNode.X;
                    rhs[globalNodeIndex * _FieldDOFInfo.NDegreesOfFreedom + 1] += forcePerNode.Y;
                    rhs[globalNodeIndex * _FieldDOFInfo.NDegreesOfFreedom + 2] += forcePerNode.Z;
                }
            }
        }
        private void ApplyFixedNormalForceNeumannBoundary(
            FixedNormalForceNeumannBoundary boundary, TetrahedralMesh mesh, double[] rhs)
        {
            BoundaryFace[] faces = mesh.GetFacesForBoundary(boundary)!;
            double totalArea = faces.Sum(f=>f.Area);
            double pressureMagnitude = boundary.NormalForce / totalArea;
            foreach (var face in faces)
            {

                Vector3D normal = face.Normal;
                double area = face.Area;


                Vector3D unitNormal = normal.Normalize();

                double[] effectivePressureOntoFace = unitNormal.Scale(pressureMagnitude).ToArray();

                foreach (Node node in face.Nodes)
                {

                    int globalNodeIndex = mesh.MapNodeIdentifierToGlobalIndex[node.Identifier];
                    // Distribute traction forces to the nodes
                    for (int j = 0; j < 3; j++) // Loop over translation DOFs (X, Y, Z)
                    {
                        int dofIndex = globalNodeIndex * _FieldDOFInfo.NDegreesOfFreedom + j;
                        rhs[dofIndex] += (effectivePressureOntoFace[j] * area / 3.0);
                    }
                }
            }
        }
        private void ApplySurfaceTractionNeumannBoundary(
    SurfaceTractionNeumannBoundary boundary, TetrahedralMesh mesh, double[] rhs)
        {
            throw new Exception("Not verified this is correct");
            BoundaryFace[] faces = mesh.GetFacesForBoundary(boundary)!;

            Vector3D tractions = boundary.Tractions; // Traction vector in global coordinates
            foreach (var face in faces)
            {

                Vector3D normal = face.Normal;
                double area = face.Area; // Triangle area


                // Compute force contribution per node
                Vector3D forceContribution = tractions * area / 3.0;
                double[] forceContributionVector = forceContribution.ToArray();
                // Compute face centroid
                Vector3D centroid = face.CenterPoint;

                // Apply traction force and compute moments for each node
                foreach (Node node in face.Nodes)
                {
                    int globalNodeIndex = mesh.MapNodeIdentifierToGlobalIndex[node.Identifier];

                    // Compute moment contribution using r × F
                    Vector3D r = node - centroid;
                    double[] momentContribution = r.Cross(forceContribution).ToArray();

                    // Apply force contributions to the RHS vector
                    for (int j = 0; j < 3; j++) // X, Y, Z force DOFs
                    {
                        int forceDofIndex = globalNodeIndex * _FieldDOFInfo.NDegreesOfFreedom + j;
                        rhs[forceDofIndex] += forceContributionVector[j];
                    }
                    for (int j = 0; j < 3; j++) // X, Y, Z moment DOFs
                    {
                        int momentDofIndex = globalNodeIndex * _FieldDOFInfo.NDegreesOfFreedom + 3 + j;
                        rhs[momentDofIndex] += momentContribution[j];
                    }
                }
            }
        }
        private void ApplySurfaceForceNeumannBoundary(
            SurfaceForceNeumannBoundary boundary, TetrahedralMesh mesh, double[] rhs)
        {
            throw new Exception("Not verified this is correct");
            BoundaryFace[] faces = mesh.GetFacesForBoundary(boundary)!;

            // Compute total surface area
            double totalArea = faces.Sum(face => face.Area);

            if (totalArea <= 0)
                throw new InvalidOperationException("Total surface area must be greater than zero.");

            foreach (var face in faces)
            {
                double areaFraction = face.Area / totalArea;  // Fraction of total force applied to this face
                Vector3D forcePerFace = boundary.Forces * areaFraction;  // Proportional force

                Vector3D centroid = face.CenterPoint;  // Get centroid of the face

                // Distribute force among nodes
                Vector3D forcePerNode = forcePerFace / 3.0;

                foreach (Node node in face.Nodes)
                {
                    int globalNodeIndex = mesh.MapNodeIdentifierToGlobalIndex[node.Identifier];

                    // Apply force contributions to translational DOFs
                    rhs[globalNodeIndex * _FieldDOFInfo.NDegreesOfFreedom + 0] += forcePerNode.X;
                    rhs[globalNodeIndex * _FieldDOFInfo.NDegreesOfFreedom + 1] += forcePerNode.Y;
                    rhs[globalNodeIndex * _FieldDOFInfo.NDegreesOfFreedom + 2] += forcePerNode.Z;

                    // Compute moment contribution using r × F (lever arm cross force)
                    Vector3D r = node - centroid;
                    Vector3D momentContribution = r.Cross(forcePerNode);

                    // Apply moments to rotational DOFs
                    rhs[globalNodeIndex * _FieldDOFInfo.NDegreesOfFreedom + 3] += momentContribution.X;
                    rhs[globalNodeIndex * _FieldDOFInfo.NDegreesOfFreedom + 4] += momentContribution.Y;
                    rhs[globalNodeIndex * _FieldDOFInfo.NDegreesOfFreedom + 5] += momentContribution.Z;
                }
            }
        }

    }
}