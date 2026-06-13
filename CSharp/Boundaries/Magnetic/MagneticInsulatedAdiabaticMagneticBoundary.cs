namespace FiniteElementAnalysis.Boundaries.Magnetic
{
    public class MagneticInsulatedAdiabaticMagneticBoundary : LinearBoundary
    {
        public MagneticInsulatedAdiabaticMagneticBoundary(
            string name)
            : base(BoundaryConditionType.MagneticInsulatedAdiabaticMagneticBoundary, name)
        {
        }
    }
}