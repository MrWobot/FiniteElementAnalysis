using Core.Maths;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.SourceRegions;
using FiniteElementAnalysis.Fields;
using Core.Timing;
using Core.Maths.BlockOperationMatrices;
using Core.FileSystem;
using InfernoDispatcher.Locking;
using Core.MemoryManagement;
using Core.Maths.CUBLAS;
using Core.Pool;
using Core.Cleanup;
using FiniteElementAnalysis.Mesh.Tetrahedral;
using FiniteElementAnalysis.Mesh;
using Core.Maths.Matrices;
using FiniteElementAnalysis.Boundaries.Electrostatic;
using FiniteElementAnalysis.Materials;

namespace FiniteElementAnalysis.Solvers
{
    public abstract class SolverBase<TSolverResult>
    {
        private const double MAX_PROPORTION_MEMORY_CAN_USE = 0.8d;
        private const double MAX_PROPORTION_GPU_MEMORY_CAN_USE = 0.8d;
        private const int N_GPU_CUDA_THREADS = 30;
        protected int _NDegreesOfFreedom;
        protected SolverBase(int nDegreesOfFreedom)
        {
            _NDegreesOfFreedom = nDegreesOfFreedom;
        }
        public abstract TSolverResult Solve(
            TetrahedralMesh mesh,
            WorkingDirectoryManager workingDirectoryManager,
            string operationIdentifier = "default",
            DelegateApplySourceRegion[]? applySourceRegion_s = null,
            SolverMethod solverMethod = SolverMethod.BlockMatrixInversionGpuOnly,
            CompositeProgressHandler? progressHandler = null,
            FileCachedItem<CoreSolverResult>? cachedSolverResult = null,
            bool useCachedSolverResults = false);
        protected CoreSolverResult _Solve(
            TetrahedralMesh mesh,
            WorkingDirectoryManager workingDirectoryManager,
            string operationIdentifier = "default",
            DelegateApplySourceRegion[]? applySourceRegion_s = null,
            SolverMethod solverMethod = SolverMethod.BlockMatrixInversionGpuOnly,
            CompositeProgressHandler? progressHandler = null,
            FileCachedItem<CoreSolverResult>? cachedSolverResult = null,
            bool useCachedSolverResults = false)
        {
            CoreSolverResult? solverResultFromCache = _Solve_Initialize(
                mesh, 
                cachedSolverResult, 
                useCachedSolverResults, 
                out int size,
                out long startTime,
                out InfernoFiniteResourceSemaphore? memoryAllocationLock,
                out int validationHash);
            if(solverResultFromCache != null) {
                return solverResultFromCache;
            }
            IBigMatrix K;
            StandardProgressHandler createBlockMatrixProgressHandler = new StandardProgressHandler();
            Console.WriteLine($"K matrix size: {size}X{size}");
            using (createBlockMatrixProgressHandler.RegisterPrintPercentSameLine("Initializing BlockMatrix: "))
            {
                K = new BlockMatrix(size, size,
                        workingDirectoryManager.NewBinFile(), 
                        progressHandler: createBlockMatrixProgressHandler);
            }
            using (K)
            {
                double[] rhs = new double[size];
                CompositeProgressHandler stampProgressHandler = new CompositeProgressHandler(3);
                using (stampProgressHandler.RegisterPrintPercentSameLine("Stamp progress: "))
                {
                    _Stamp(K, rhs, size, mesh, stampProgressHandler, applySourceRegion_s, operationIdentifier);
                }
                double proportionOfKInMemoryAfterStamp = K.ProportionOfMatrixInMemory;
                double proportionOfMaxCacheSizeUsedAfterStamp = K.ProportionOfMaxCacheSizeUsed;
                long timeTakenToStamp = TimeHelper.MillisecondsNow - startTime;
                startTime = TimeHelper.MillisecondsNow;


                DoActualSolve(mesh, solverMethod, memoryAllocationLock, K, workingDirectoryManager,
                    rhs, progressHandler,
                    out double[]? unknowns);
                long timeTakenToSolve = TimeHelper.MillisecondsNow - startTime;
                var solverResult = new CoreSolverResult(
                    operationIdentifier,
                    timeTakenToStamp,
                    timeTakenToSolve,
                    proportionOfKInMemoryAfterStamp,
                    proportionOfMaxCacheSizeUsedAfterStamp,
                    proportionOfKInMemoryAfterSolve: K.ProportionOfMatrixInMemory,
                    proportionOfMaxCacheSizeUsedAfterSolve: K.ProportionOfMaxCacheSizeUsed,
                    unknowns,
                    rhs,
                    validationHash
                );
                cachedSolverResult?.Set(solverResult);
                mesh.IsPartOfResult = true;
                return solverResult;
            }
        }
        private void DoActualSolve(
            TetrahedralMesh mesh,
            SolverMethod solverMethod,
            InfernoFiniteResourceSemaphore? memoryAllocationLock,
            IBigMatrix K, 
            WorkingDirectoryManager workingDirectoryManager,
            double[] rhs, 
            CompositeProgressHandler? progressHandler,
            out double[]? unknowns) {

            unknowns = null;
            switch (solverMethod)
            {
                case SolverMethod.BlockMatrixInversionGpuOnly:
                    SolveUsingBlockMatrixInversion(ref unknowns, K, workingDirectoryManager,
                        memoryAllocationLock, rhs, MathsRunningMode.GpuOnly, progressHandler);
                    break;
                case SolverMethod.BlockMatrixInversionCpuOnly:
                    SolveUsingBlockMatrixInversion(ref unknowns, K, workingDirectoryManager,
                        memoryAllocationLock, rhs, MathsRunningMode.CpuOnly, progressHandler);
                    break;
                case SolverMethod.BlockMatrixInversionWhateverHardware:
                    SolveUsingBlockMatrixInversion(ref unknowns, K, workingDirectoryManager,
                        memoryAllocationLock, rhs, MathsRunningMode.Whatever, progressHandler);
                    break;
                case SolverMethod.SimpleInMemoryMatrixInversion:
                    double[][] KInverse = MatrixHelper.Invert(K);
                    unknowns = MatrixHelper.MatrixMultiplyByVector(
                        KInverse, rhs);
                    break;
                /*case SolverMethod.GMRES:
                    GmresResult result = GMRES.GmresSolver.Solve(K, rhs, maxOuterIterations: 10000);
                    if (!result.IsConverged)
                        throw new Exception("Did not converge");
                    unknowns = result.XArray;
                    break;*/
                default:
                    throw new NotImplementedException($"Not implemented for {Enum.GetName(typeof(SolverMethod), solverMethod)}");
            }
            SetValueOnNodes(unknowns, mesh.Nodes);
        }
        private void _Stamp(
            IBigMatrix K,
            double[] rhs,
            int size,
            TetrahedralMesh mesh,
            CompositeProgressHandler? progressHandler,
            DelegateApplySourceRegion[]? applySourceRegion_s,
            string operationIdentifier) {

            StampElementMatricesOntoGlobal(K, rhs, size, mesh, progressHandler);
            ApplySourceRegions(applySourceRegion_s, mesh, K, rhs, operationIdentifier, progressHandler);
            ApplyBoundariesToGlobal(mesh, K, rhs, operationIdentifier, progressHandler);
        }
        public IterativeSolveHandle SolveNonLinearIterativeNoTensorReuse(
            TetrahedralMesh mesh,
            WorkingDirectoryManager workingDirectoryManager,
            string operationIdentifier = "default",
            DelegateApplySourceRegion[]? applySourceRegion_s = null,
            SolverMethod solverMethod = SolverMethod.BlockMatrixInversionGpuOnly,
            bool createProgressHandlers = false
        )
        {
            CoreSolverResult? solverResultFromCache = _Solve_Initialize(
                mesh,
                cachedSolverResult:null,
                useCachedSolverResults:false,
                out int size,
                out long startTime,
                out InfernoFiniteResourceSemaphore? memoryAllocationLock,
                out int validationHash);
            int nIteration = 0;
            StandardProgressHandler? createBlockMatrixProgressHandler = null;
            CompositeProgressHandler? stampProgressHandler = null;
            CompositeProgressHandler? solveProgressHandler = null;
            if (createProgressHandlers) {

                createBlockMatrixProgressHandler = new StandardProgressHandler();
                solveProgressHandler = new CompositeProgressHandler();
                stampProgressHandler = new CompositeProgressHandler();

            }
            IBigMatrix? K = null;
            double[]? rhs = null;
            long timeTakenToStamp = -1;
            double proportionOfKInMemoryAfterStamp = -1,
                proportionOfMaxCacheSizeUsedAfterStamp = -1;
            List<IDisposable> disposables = new List<IDisposable>();
            bool doneStamp = false;
            return new IterativeSolveHandle(
                doStamp: (out double[] rhsOut, out IBigMatrix KOut) => {
                    if (doneStamp) {
                        throw new Exception("Do not call stamp twice in a row without a followup call to solve!");
                    }
                    startTime = TimeHelper.MillisecondsNow;
                    K?.Dispose();
                    K = new BlockMatrix(size, size,
                       workingDirectoryManager.NewBinFile(),
                       progressHandler: createBlockMatrixProgressHandler);
                     rhs = new double[size];
                    _Stamp(K, rhs, size, mesh, stampProgressHandler, applySourceRegion_s, operationIdentifier);
                    proportionOfKInMemoryAfterStamp = K.ProportionOfMatrixInMemory;
                    proportionOfMaxCacheSizeUsedAfterStamp = K.ProportionOfMaxCacheSizeUsed;
                    timeTakenToStamp = TimeHelper.MillisecondsNow - startTime;
                    rhsOut = rhs;
                    KOut = K;
                    doneStamp = true;
                },
                doSolve: () => {
                    if (!doneStamp)
                    {
                        throw new Exception("Not stamped!");
                    }
                    startTime = TimeHelper.MillisecondsNow;
                        DoActualSolve(mesh, solverMethod, memoryAllocationLock, K, workingDirectoryManager,
                            rhs!, solveProgressHandler,
                            out double[]? unknowns);
                        long timeTakenToSolve = TimeHelper.MillisecondsNow - startTime;
                        var solverResult = new CoreSolverResult(
                            operationIdentifier,
                            timeTakenToStamp,
                            timeTakenToSolve,
                            proportionOfKInMemoryAfterStamp,
                            proportionOfMaxCacheSizeUsedAfterStamp,
                            proportionOfKInMemoryAfterSolve: K.ProportionOfMatrixInMemory,
                            proportionOfMaxCacheSizeUsedAfterSolve: K.ProportionOfMaxCacheSizeUsed,
                            unknowns,
                            rhs,
                            validationHash
                        );
                        mesh.IsPartOfResult = true;
                    doneStamp = false;
                        return solverResult;
                },dispose: () => {
                    K?.Dispose();
                });
        }
        private CoreSolverResult? _Solve_Initialize(
            TetrahedralMesh mesh,
            FileCachedItem<CoreSolverResult>? cachedSolverResult,
            bool useCachedSolverResults,
            out int size,
            out long startTime,
            out InfernoFiniteResourceSemaphore? memoryAllocationLock,
            out int validationHash)
        {

            if (mesh.IsPartOfResult)
                throw new ArgumentException($"{nameof(mesh)} is part of another solver result");
            startTime = TimeHelper.MillisecondsNow;
            if (!mesh.Boundaries.HasEntries)
                throw new Exception($"Mesh had no boundaries");
            if (mesh.Volumes.HasMultipleOperationEntries)
                throw new Exception($"Mesh contains {nameof(MultipleOperationVolume)}'s");
            if (mesh.Boundaries.HasMultipleOperationEntries)
                throw new Exception($"Mesh contains {nameof(MultipleOperationBoundary)}'s");
            bool hasNonLinearities = mesh.HasNonLinearBoundaries();
            if (hasNonLinearities) throw new NotImplementedException("Not implemented for non linear boundaries yet");
            int sizeForNodes = _NDegreesOfFreedom * mesh.Nodes.Length;
            var systemMatrixModifyingBoundaries = mesh.Boundaries.SystemMatrixModifyingBoundaries;
            int sizeForSystemMatrixModifyingBoundaries = systemMatrixModifyingBoundaries.Sum(b=>b.NAdditionalRowsColumnsRequired);
            size = sizeForNodes + sizeForSystemMatrixModifyingBoundaries;
            validationHash = size;
            if (sizeForSystemMatrixModifyingBoundaries > 0)
            {
                AssignIndicesToSystemMatrixModifyingBoundaries(sizeForNodes, systemMatrixModifyingBoundaries);
            }
            Node[] nodes = mesh.Nodes;
            if (cachedSolverResult != null && useCachedSolverResults)
            {
                Console.WriteLine("Attempting to read cached solver result...");
                if (ReadAndValidateSolverResult(cachedSolverResult, validationHash, out CoreSolverResult? solverResult))
                {
                    Console.WriteLine("Successfully read cached solver result");
                    SetValueOnNodes(solverResult!.UnknownsVector, nodes);
                    memoryAllocationLock = null;
                    return solverResult!;
                }
                Console.WriteLine("Failed to read cached solver result...");
            }

            MemoryMetrics memoryMetrics = MemoryHelper.GetMemoryMetricsNow();
            memoryAllocationLock = new InfernoFiniteResourceSemaphore((long)(memoryMetrics.Free * MAX_PROPORTION_MEMORY_CAN_USE));
            return null;
        }
        private void AssignIndicesToSystemMatrixModifyingBoundaries(int startIndex, ISystemMatrixModifyingBoundary[] systemMatrixModifyingBoundaries)
        {
            int index = startIndex;
            int sizeForSystemMatrixModifyingBoundaries = systemMatrixModifyingBoundaries.Sum(b => b.NAdditionalRowsColumnsRequired);
            foreach(ISystemMatrixModifyingBoundary boundary in systemMatrixModifyingBoundaries)
            {
                int[] indicesToAssign = new int[boundary.NAdditionalRowsColumnsRequired];
                for (int i = 0; i < boundary.NAdditionalRowsColumnsRequired; i++) {
                    indicesToAssign[i] = index++;
                }
                boundary.IndicesAssigned = indicesToAssign;
            }
        }
        protected abstract void StampElementMatricesOntoGlobal(IBigMatrix K, double[] rhs, int size,
            TetrahedralMesh mesh,
            CompositeProgressHandler? parentProgressHandler);
        protected void StampElementOntoGlobal(
            TetrahedronElement element,
            Volume volume,
            double[] rhs,
            DelegateStampOntoGlobal stampOntoGlobal,

            int nFieldComponents, FieldOperationType fieldOperationType)
        {
            double[][] B = element.GetBMatrix(nFieldComponents, fieldOperationType, _NDegreesOfFreedom);
            double[][] BTranspose = element.GetBMatrixTranspose(nFieldComponents, fieldOperationType, _NDegreesOfFreedom);
            var bTransposeScaledByK = ScaleBTransposeByK(BTranspose, volume);
            var dotProduct = MatrixHelper.Multiply(
                bTransposeScaledByK, B);
            double[][] Ke = MatrixHelper.Scale(dotProduct, element.ElementVolume);
            double[] rhsForElement = new double[4 * _NDegreesOfFreedom];
            stampOntoGlobal(element.Nodes, Ke, rhs);
        }
        protected abstract double[][] ScaleBTransposeByK(double[][] bTranspose, Volume volume);
        private void SolveUsingBlockMatrixInversion(
            ref double[]? unknowns,
            IBigMatrix K,
            WorkingDirectoryManager workingDirectoryManager,
            InfernoFiniteResourceSemaphore? memoryAllocationSemaphore,
            double[] rhs,
            MathsRunningMode runningMode,
            CompositeProgressHandler? parentProgressHandler)
        {
            Console.WriteLine("WorkingSet right at the start of inversion was: " + Environment.WorkingSet);
            InfernoFiniteResourceSemaphore gpuMemoryAllocationLock = new InfernoFiniteResourceSemaphore(
                (long)(MemoryHelper.GetGPUMemoryMetrics().Free * MAX_PROPORTION_GPU_MEMORY_CAN_USE));
            using (CudaContextAssignedThreadPool cudaContextAssignedThreadPool = new CudaContextAssignedThreadPool(10))
            {
                GPUMathsParameters gpuMathsParameters = new GPUMathsParameters(
                    cudaContextAssignedThreadPool,
                    MAX_PROPORTION_GPU_MEMORY_CAN_USE, 
                    gpuMemoryAllocationLock);
                using (var blockOperationMatrix =
                    BlockOperationMatrixFactory.FromForInversion(
                        K, 0.8, workingDirectoryManager,
                        out int padding, out int nPartitions, gpuMathsParameters,
                        runningMode, null))
                {
                    K.DumpCached(false);
                    CompositeProgressHandler progressHandler = new CompositeProgressHandler();
                    using (progressHandler.RegisterPrintPercentSameLineWithEstimatedCompletionTime("Inverting progress: "))
                    {
                        using (var cleanupHandler = new CleanupHandler())
                        {
                            using (var inverse = blockOperationMatrix.InvertOnNewThread(
                                cleanupHandler, memoryAllocationSemaphore, gpuMathsParameters,
                                runningMode, progressHandler).Wait())
                            {
                                // mapNGPUContextToTimeTaken.Add(i, timeTaken);
                                double[] rhsPadded = new double[rhs.Length + padding];
                                Array.Copy(rhs, 0, rhsPadded, 0, rhs.Length);
                                double[] unknownsPadded = inverse.MultiplyByVectorOnNewThread(rhsPadded, 0, memoryAllocationSemaphore).Wait();
                                unknowns = new double[rhs.Length];
                                Array.Copy(unknownsPadded, 0, unknowns, 0, unknowns.Length);
                            }
                        }
                    }
                }
            }
        }
        protected abstract void ApplySourceRegions(
            DelegateApplySourceRegion[]? applySourceRegion_s,
            TetrahedralMesh mesh,
            IBigMatrix K,
            double[] rhs,
            string operationIdentifier,
            CompositeProgressHandler parentProgressHandler);
        private void SetValueOnNodes(double[] unknowns, Node[] nodes)
        {
            int dof = _NDegreesOfFreedom;
            for (int i = 0; i < nodes.Length; i++)
            {
                Node node = nodes[i];
                double[] values = new double[dof];
                Array.Copy(unknowns, (i * dof), values, 0, dof);
                node.Values = values;
            }
        }
        protected void ApplyBoundariesToGlobal(TetrahedralMesh mesh,
            IBigMatrix K, double[] rhs, string operationIdentifier,
            CompositeProgressHandler parentProgressHandler)
        {
            StandardProgressHandler progressHandler = new StandardProgressHandler();
            parentProgressHandler.AddChild(progressHandler);
            Action updateProgress = progressHandler.GetUpdateProgress(mesh.Boundaries.Entries.Length, mesh.Boundaries.Entries.Length);
            foreach (Boundary boundary in mesh.Boundaries.Entries)
            {
                if (boundary.BoundaryConditionType.Equals(BoundaryConditionType.OperationSpecific))
                {
                    throw new Exception($"Attempted to parse a {nameof(MultipleOperationBoundary)} named \"{boundary.Name}\" into {nameof(Solve)}");
                }
                ApplyBoundaryToGlobal(boundary, mesh, K, rhs, operationIdentifier);
                updateProgress();
            }
            progressHandler.Set(1);
        }
        protected abstract void ApplyBoundaryToGlobal(Boundary boundary, TetrahedralMesh mesh,
            IBigMatrix K, double[] rhs, string operationIdentifier);
        protected DelegateStampOntoGlobal Get_StampOntoGlobal(IBigMatrix K, double[] rhs, int size,
    Dictionary<int, int> mapNodeToGlobalIndex)
        {
            int dof = _NDegreesOfFreedom;
            return (nodes, Ke, rhsE) =>
            {
                for (int row = 0; row < 4; row++)  // Loop over the 4 nodes of the element
                {
                    Node nodeRow = nodes[row];
                    int globalNodeRowIndex = mapNodeToGlobalIndex[nodeRow.Identifier];  // Get global row index
                    for (int dofRow = 0; dofRow < dof; dofRow++)  // Loop over DOFs for this row
                    {
                        int globalRowIndex = (dof * globalNodeRowIndex) + dofRow;
                        for (int column = 0; column < 4; column++)  // Loop over the columns
                        {
                            Node nodeColumn = nodes[column];
                            int globalNodeColumnIndex = mapNodeToGlobalIndex[nodeColumn.Identifier];  // Get global column index
                            for (int dofColumn = 0; dofColumn < dof; dofColumn++)  // Loop over DOFs for the column
                            {
                                int globalColumnIndex = (dof * globalNodeColumnIndex) + dofColumn;

                                // Correctly stamp the element matrix Ke onto the global matrix K
                                double value = K[globalRowIndex, globalColumnIndex];
                                value += Ke[(row * dof) + dofRow][(column * dof) + dofColumn];
                                K[globalRowIndex, globalColumnIndex] = value;
                            }
                        }
                        // Correctly stamp the element RHS vector rhsE onto the global RHS vector rhs
                        rhs[globalRowIndex] += rhsE[(row * dof) + dofRow];
                    }
                }
            };
        }
        public static void FixValueInUnknowns(
            IBigMatrix  A, double[] C, int fixedIndex, double fixedValue)
        {
            int n = A.NRows; // Number of rows/columns in the matrix

            // Step 1: Modify the matrix A at row fixedIndex
            for (int col = 0; col < n; col++)
            {
                A[fixedIndex,col] = 0; // Zero out the entire row
                if (A[fixedIndex,col] != 0) {
                    A[fixedIndex,col] = 0;
                }
            }
            A[fixedIndex,fixedIndex] = 1; // Set the diagonal element to 1
            // Step 2: Modify the RHS vector C
            C[fixedIndex] = fixedValue;

            // Step 3: Adjust other rows in C to account for the fixed value
            for (int rowIndex = 0; rowIndex < n; rowIndex++)
            {
                if (rowIndex == fixedIndex) continue; // Skip the row that corresponds to the fixed value

                // Get the value from A[rowIndex][fixedIndex]
                double aValue = A[rowIndex,fixedIndex];

                // Zero out the element in A (rowIndex, fixedIndex) to decouple the fixed value from the system
                A[rowIndex,fixedIndex] = 0;

                // Modify the corresponding entry in C based on the fixed value
                C[rowIndex] -= aValue * fixedValue;
            }
        }
        private static bool ReadAndValidateSolverResult(FileCachedItem<CoreSolverResult> fileCachedItem, int validationHash, out CoreSolverResult? solverResult) {
            if (!fileCachedItem.TryGet(out solverResult)) {
                return false;
            }
            return solverResult.ValidationHash == validationHash;
        }
        protected void ApplyDirichletBoundary(
    Boundary boundary, TetrahedralMesh mesh, IBigMatrix K, double[] rhs,
    double boundaryNodalScalarValue)
        {
            ApplyDirichletBoundary(boundary, mesh, K, rhs, new double[] { boundaryNodalScalarValue });
        }
        protected void ApplyDirichletBoundary(
    Boundary boundary, TetrahedralMesh mesh, IBigMatrix K, double[] rhs,
    double[] boundaryNodalVectorValues)
        {
            Dictionary<int, int> mapNodeToGlobalIndex = mesh.MapNodeIdentifierToGlobalIndex;
            if (boundaryNodalVectorValues.Length != _NDegreesOfFreedom)
            {
                throw new ArgumentException("The length of the boundaryNodalVectorValues must match the degrees of freedom.");
            }
            Node[] nodes = mesh.GetFacesForBoundary(boundary)
                ?.SelectMany(f => f.Nodes)
                .GroupBy(n => n)
                .Select(g => g.First())
                .ToArray();

            if (nodes == null) return;
            HashSet<int> rowsToBeProcessed = new HashSet<int>();
            foreach (Node node in nodes)
            {
                int nodeIndex = mapNodeToGlobalIndex[node.Identifier];
                for (int iDof = 0; iDof < _NDegreesOfFreedom; iDof++)
                {
                    int dofIndex = nodeIndex * _NDegreesOfFreedom + iDof;
                    rowsToBeProcessed.Add(dofIndex);
                    rhs[dofIndex] = boundaryNodalVectorValues[iDof];
                }
            }
            foreach (int rowIndex in rowsToBeProcessed)
            {
                double[] row = K.ReadRow(rowIndex);
                foreach (Node node in nodes)
                {
                    int nodeIndex = mapNodeToGlobalIndex[node.Identifier];
                    for (int iDof = 0; iDof < _NDegreesOfFreedom; iDof++)
                    {
                        int dofIndex = nodeIndex * _NDegreesOfFreedom + iDof;
                        if (rowIndex == dofIndex)
                        {
                            for (int colIndex = 0; colIndex < row.Length; colIndex++)
                            {
                                row[colIndex] = (colIndex == dofIndex) ? 1 : 0;
                            }
                        }
                        else
                        {
                            double kValueToShiftToRhs = row[dofIndex];
                            if (kValueToShiftToRhs == 0) {
                                continue;
                            }
                            if(boundaryNodalVectorValues[iDof] != 0)
                            {
                                rhs[rowIndex] -= kValueToShiftToRhs * boundaryNodalVectorValues[iDof];
                            }
                            row[dofIndex] = 0;
                        }
                    }
                }
                K.WriteRow(rowIndex, row);
            }
        }
        /*
        protected void ApplyDirichletBoundary(
    Boundary boundary, TetrahedralMesh mesh, IBigMatrix K, double[] rhs,
    double[] boundaryNodalVectorValue)
        {
            Dictionary<int, int> mapNodeToGlobalIndex = mesh.MapNodeIdentifierToGlobalIndex;
            // Validate that the boundary vector length matches the degrees of freedom (which should be 3 for a vector field)
            if (boundaryNodalVectorValue.Length != _FieldDOFInfo.NDegreesOfFreedom)
            {
                throw new ArgumentException("The length of the boundaryNodalVectorValue must match the degrees of freedom.");
            }

            Node[]? nodes = mesh.GetFacesForBoundary(boundary)
                ?.SelectMany(f => f.Nodes)
                .GroupBy(n => n)
                .Select(g => g.First())
                .ToArray();
            if (nodes == null) return;

            foreach (Node node in nodes)
            {
                int nodeIndex = mapNodeToGlobalIndex[node.Identifier];

                // Loop through each degree of freedom for this node (Ax, Ay, Az)
                for (int iDof = 0; iDof < _FieldDOFInfo.NDegreesOfFreedom; iDof++)
                {
                    // Get the index for the current degree of freedom
                    int dofIndex = nodeIndex * _FieldDOFInfo.NDegreesOfFreedom + iDof;

                    // Create a zeroed-out row with a single 1 for the Dirichlet condition
                    double[] zeroedOutWithSingleOneRowForBoundary = new double[K.NColumns];
                    VectorHelper.FillWithZeros(zeroedOutWithSingleOneRowForBoundary);
                    zeroedOutWithSingleOneRowForBoundary[dofIndex] = 1;

                    // Set the RHS value for this degree of freedom to the boundary value
                    rhs[dofIndex] = boundaryNodalVectorValue[iDof];

                    // For each row in the global matrix, modify the entries related to this node's degrees of freedom
                    for (int rowIndex = 0; rowIndex < K.NRows; rowIndex++)
                    {
                        if (rowIndex == dofIndex) continue; // Skip the row corresponding to this DoF

                        // Get the value in the matrix to be shifted to the RHS
                        double kValueToShiftToRhs = K[rowIndex, dofIndex];
                        K[rowIndex, dofIndex] = 0; // Set matrix entry to 0

                        // Shift the RHS based on the matrix value and boundary value
                        if (kValueToShiftToRhs != 0 && boundaryNodalVectorValue[iDof] != 0)
                        {
                            rhs[rowIndex] -= kValueToShiftToRhs * boundaryNodalVectorValue[iDof];
                        }
                    }

                    // Write the modified row back into the matrix for the current degree of freedom
                    K.WriteRow(dofIndex, zeroedOutWithSingleOneRowForBoundary);
                }
            }
        }*/
    }
}