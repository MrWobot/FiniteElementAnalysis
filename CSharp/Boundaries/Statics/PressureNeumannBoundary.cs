using Core.Maths.Tensors;

namespace FiniteElementAnalysis.Boundaries.Statics
{
    public class PressureNeumannBoundary : Boundary
    {
        public double Pressure { get; }

        public override bool IsNonLinear => false;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="tX">Force per unit area in the x direction</param>
        /// <param name="tY">Force per unit area in the y direction</param>
        /// <param name="tZ">Force per unit area in the z direction</param>
        public PressureNeumannBoundary(string name, double pressure)
            : base(BoundaryConditionType.PressureNeumannBoundary, 
                  name, false)
        {
            Pressure = pressure;
        }
    }
}