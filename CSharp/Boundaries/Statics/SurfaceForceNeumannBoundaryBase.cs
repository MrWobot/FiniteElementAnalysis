namespace FiniteElementAnalysis.Boundaries.Statics
{
    public abstract class SurfaceForceNeumannBoundaryBase : Boundary
    {

        public override bool IsNonLinear => false;
        public SurfaceForceNeumannBoundaryBase(string name)
            : base(BoundaryConditionType.SurfaceTractionNeumannBoundary, 
                  name, false)
        {

        }
    }
}