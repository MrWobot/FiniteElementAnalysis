namespace FiniteElementAnalysis.Boundaries.Statics
{
    public class PrescribedDisplacementDirichletBoundary : Boundary
    {
        public double[] Translations { get; }

        public override bool IsNonLinear => false;

        public PrescribedDisplacementDirichletBoundary(string name, double[] translations)
            : base(BoundaryConditionType.PrescribedDisplacementDirichletBoundary, 
                  name, false)
        {
            Translations = translations;
        }
    }
}