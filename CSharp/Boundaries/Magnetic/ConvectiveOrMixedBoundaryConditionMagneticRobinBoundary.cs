namespace FiniteElementAnalysis.Boundaries.Magnetic
{
    public class ConvectiveOrMixedBoundaryConditionMagneticRobinBoundary : LinearBoundary
    {
        public double TemperatureK { get; }
        public ConvectiveOrMixedBoundaryConditionMagneticRobinBoundary(
            string name, double temperatureK)
            : base(BoundaryConditionType.ConvectiveOrMixedBoundaryConditionMagneticRobinBoundary, name)
        {
            TemperatureK = temperatureK;
        }
    }
}