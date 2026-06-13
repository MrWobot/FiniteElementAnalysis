namespace FiniteElementAnalysis.Fields
{
    public enum SolverMethod
    {
        SimpleInMemoryMatrixInversion,
        BlockMatrixInversionWhateverHardware,
        BlockMatrixInversionCpuOnly,
        BlockMatrixInversionGpuOnly,
        GMRES,       // Generalized Minimal Residual Method
        Direct,      // Direct solver (e.g., LU decomposition)
        Iterative,   // Iterative solver (e.g., Conjugate Gradient)
        Multigrid,   // Multigrid solver
        PreconditionedCG, // Preconditioned Conjugate Gradient
        SparseLU,    // Sparse LU solver
        Cholesky    // Cholesky decomposition for symmetric matrices
    }
}