
namespace FiniteElementAnalysis.Boundaries
{
    public class TemperatureBoundaryCondition : Boundary
    {
        public double Temperature { get; set; }

        public TemperatureBoundaryCondition(
            double temperature,
            List<Node> appliedNodes)
            : base(BoundaryConditionType.Temperature, appliedNodes)
        {
            Temperature = temperature;
        }

        public override void Apply()
        {
            foreach (var node in AppliedNodes)
            {
                throw new NotImplementedException();
                // Assuming node has a temperature property
                // node.Temperature = Temperature;
            }
        }
    }
}