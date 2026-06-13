namespace FiniteElementAnalysis.Fields
{
    public enum FieldOperationType
    {
        Gradient,       // For scalar fields like heat conduction, electrostatics, etc.
        Curl,           // For vector fields like magnetostatics, electromagnetics, etc.
        Divergence,     // For fluid dynamics, electrostatics (Gauss's law), compressible flow
        Laplacian,      // For diffusion problems, heat conduction, wave propagation
        Jacobian,       // For nonlinear FEA and large deformation problems
        StrainDisplacement // For structural mechanics, elasticity (small and large strain)
    }
}