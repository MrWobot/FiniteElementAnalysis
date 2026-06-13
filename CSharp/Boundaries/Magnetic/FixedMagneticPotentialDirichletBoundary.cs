namespace FiniteElementAnalysis.Boundaries.Magnetic
{
    public class FixedMagneticPotentialDirichletBoundary : LinearBoundary
    {
        public double TemperatureK { get; }
        public FixedMagneticPotentialDirichletBoundary(
            string name, double temperatureK)
            : base(BoundaryConditionType.FixedMagneticPotentialDirichletBoundary, name)
        {
            TemperatureK = temperatureK;
        }
    }
}