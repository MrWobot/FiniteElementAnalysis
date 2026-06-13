namespace FiniteElementAnalysis.Boundaries.Magnetic
{
    public class PerfectMagneticConductorBoundary : LinearBoundary
    {
        public PerfectMagneticConductorBoundary(
            string name)
            : base(BoundaryConditionType.PerfectMagneticConductorBoundary, name)
        {
        }
    }
}