namespace FiniteElementAnalysis.Boundaries.Magnetic
{
    public class InfinityBoundary : LinearBoundary
    {
        public double SmallValue { get; }
        public InfinityBoundary(
            string name, double smallValue = 1e-10)
            : base(BoundaryConditionType.InfinityBoundary, name)
        {
            SmallValue = smallValue;
        }
    }
}