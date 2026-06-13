using Core.Maths.Tensors;

namespace FiniteElementAnalysis.Boundaries.Statics
{
    public class SurfaceForceNeumannBoundary : Boundary
    {
        public double Fx { get; }
        public double Fy { get; }
        public double Fz { get; }
        public Vector3D Forces{ get; }  // [Fx, Fy, Fz]

        public override bool IsNonLinear => false;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="fX">Force in the x direction</param>
        /// <param name="fY">Force in the y direction</param>
        /// <param name="fZ">Force in the z direction</param>
        public SurfaceForceNeumannBoundary(string name, double fX, double fY, double fZ)
            : base(BoundaryConditionType.SurfaceTractionNeumannBoundary, 
                  name, false)
        {
            Fx = fX;
            Fy = fY;
            Fz = fZ;
            Forces = new Vector3D(fX, fY, fZ);
        }
    }
}