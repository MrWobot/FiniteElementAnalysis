using Core.Maths.Tensors;

namespace FiniteElementAnalysis.Boundaries.Statics
{
    public class SurfaceForceNeumannBoundary2D : SurfaceForceNeumannBoundaryBase
    {
        public double Fx => Forces.X;
        public double Fy => Forces.Y;
        public Vector2D Forces { get; }

        public SurfaceForceNeumannBoundary2D(string name, Vector2D forces)
            : base(name)
        {
            Forces = forces;
        }
    }
}