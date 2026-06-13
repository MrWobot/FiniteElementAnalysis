using Core.Maths.Tensors;

namespace FiniteElementAnalysis.Boundaries.Statics
{
    public class FixedNormalForceNeumannBoundary : Boundary
    {
        public double NormalForce { get; }

        public override bool IsNonLinear => false;

        public FixedNormalForceNeumannBoundary(string name, double normalForce)
            : base(BoundaryConditionType.FixedNormalForceNeumannBoundary, name, false)
        {
            NormalForce = normalForce;
        }
    }
}