namespace FiniteElementAnalysis.Boundaries.Magnetic
{
    public class FixedMagneticFluxMagneticNeumannBoundary : LinearBoundary
    {
        public FixedMagneticFluxMagneticNeumannBoundary(
            string name)
            : base(BoundaryConditionType.FixedMagneticFluxMagneticNeumannBoundary, name)
        {
        }
    }
}