using Core.Maths.Tensors;
namespace FiniteElementAnalysis.Integration
{
    public class IntegrationPoint
    {
        public Vector3D Position { get; }
        public double Weight { get; }

        public IntegrationPoint(Vector3D position, double weight)
        {
            Position = position;
            Weight = weight;
        }
    }
}