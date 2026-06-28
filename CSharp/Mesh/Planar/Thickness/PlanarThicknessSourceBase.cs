namespace FiniteElementAnalysis.Mesh.Planar.Thickness
{

    public abstract class PlanarThicknessSourceBase
    {
        public abstract double GetThickness(double[] position);
    }
}