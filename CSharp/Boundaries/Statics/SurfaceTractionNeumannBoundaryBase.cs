using Core.Maths.Tensors;

namespace FiniteElementAnalysis.Boundaries.Statics
{
    public abstract class SurfaceTractionNeumannBoundaryBase : Boundary
    {

        public override bool IsNonLinear => false;

        public SurfaceTractionNeumannBoundaryBase(string name)
            : base(BoundaryConditionType.SurfaceTractionNeumannBoundary, 
                  name, false)
        {

        }
    }
}