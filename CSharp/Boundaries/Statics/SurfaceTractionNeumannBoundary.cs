using Core.Maths.Tensors;

namespace FiniteElementAnalysis.Boundaries.Statics
{
    public class SurfaceTractionNeumannBoundary : Boundary
    {
        public double Tx { get; }
        public double Ty { get; }
        public double Tz { get; }
        public Vector3D Tractions { get; }  // [Fx, Fy, Fz]

        public override bool IsNonLinear => false;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="tX">Force per unit area in the x direction</param>
        /// <param name="tY">Force per unit area in the y direction</param>
        /// <param name="tZ">Force per unit area in the z direction</param>
        public SurfaceTractionNeumannBoundary(string name, double tX, double tY, double tZ)
            : base(BoundaryConditionType.SurfaceTractionNeumannBoundary, 
                  name, false)
        {
            Tx = tX;
            Ty = tY;
            Tz = tZ;
            Tractions = new Vector3D(tX, tY, tZ);
        }
    }
}