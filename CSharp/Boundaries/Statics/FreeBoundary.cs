using Core.Maths.Tensors;

namespace FiniteElementAnalysis.Boundaries.Statics
{
    public class FreeBoundary : Boundary
    {

        public override bool IsNonLinear => false;

        public FreeBoundary(string name)
            : base(BoundaryConditionType.FreeBoundary, name)
        {

        }
    }
}