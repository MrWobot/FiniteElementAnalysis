using Core.Maths.Tensors;

namespace FiniteElementAnalysis.Boundaries.Statics
{
    public class FixedDirectionalForceNeumannBoundary : Boundary
    {
        public Vector3D Force { get; }

        public override bool IsNonLinear => false;

        public FixedDirectionalForceNeumannBoundary(string name, Vector3D force)
            : base(BoundaryConditionType.FixedDirectionalForceNeumannBoundary, name)
        {
            Force = force;
        }
    }
}