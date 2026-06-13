namespace FiniteElementAnalysis.Boundaries.Statics
{
    public class FixedDisplacementDirichletBoundary : Boundary
    {

        public override bool IsNonLinear => false;

        public FixedDisplacementDirichletBoundary(
            string name, double[] translations, double[] rotations)
            : base(BoundaryConditionType.FixedDisplacementDirichletBoundary, 
                  name, false)
        {

        }
    }
}