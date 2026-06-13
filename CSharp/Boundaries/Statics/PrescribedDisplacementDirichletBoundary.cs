namespace FiniteElementAnalysis.Boundaries.Statics
{
    public class PrescribedDisplacementDirichletBoundary : Boundary
    {
        public double[] Translations { get; }
        public double[] Rotations { get; }

        public override bool IsNonLinear => false;

        public PrescribedDisplacementDirichletBoundary(string name, double[] translations, double[] rotations)
            : base(BoundaryConditionType.PrescribedDisplacementDirichletBoundary, 
                  name, false)
        {
            Translations = translations;
            Rotations = rotations;
        }
    }
}