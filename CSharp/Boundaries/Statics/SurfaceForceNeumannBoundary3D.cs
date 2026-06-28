using Core.Maths.Tensors;

namespace FiniteElementAnalysis.Boundaries.Statics
{
    public class SurfaceForceNeumannBoundary3D : SurfaceForceNeumannBoundaryBase
    {
        public double Fx => Forces.X;
        public double Fy => Forces.Y;
        public double Fz => Forces.Z;
        public Vector3D Forces{ get; }

        public SurfaceForceNeumannBoundary3D(string name, Vector3D forces)
            : base(name)
        {
            Forces = forces;
        }
    }
}