namespace FiniteElementAnalysis.Boundaries
{
    public abstract class LinearBoundary : Boundary
    {
        public override bool IsNonLinear => false;
        protected LinearBoundary(BoundaryConditionType type, string name) : base(type, name)
        {

        }
        protected LinearBoundary(BoundaryConditionType type, string name, bool twoElementsAllowed) : base(type, name, twoElementsAllowed)
        {

        }
    }
}