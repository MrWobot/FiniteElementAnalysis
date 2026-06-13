using Core.Maths.Tensors;
namespace FiniteElementAnalysis.Plotting
{
    internal class VectorAtPoint
    {
        public Vector2D Vector { get; }
        public double X { get; }
        public double Y { get; }
        public VectorAtPoint(Vector2D direction, double x, double y)
        {
            Vector = direction;
            X = x;
            Y = y;
        }
    }
}