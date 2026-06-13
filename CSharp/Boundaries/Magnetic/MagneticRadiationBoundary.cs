namespace FiniteElementAnalysis.Boundaries.Magnetic
{
    public class MagneticRadiationBoundary : LinearBoundary
    {
        public MagneticRadiationBoundary(
            string name)
            : base(BoundaryConditionType.MagneticRadiationBoundary, name)
        {
        }
    }
}