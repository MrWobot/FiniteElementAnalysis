namespace FiniteElementAnalysis.Mesh.Planar.Thickness
{

    public class UniformThicknessSource: PlanarThicknessSourceBase
    {
        public double Thickness { get; }
        public UniformThicknessSource(double thickness) {
            Thickness = thickness;
        }
        public override double GetThickness(double[] position) {
            return Thickness;
        }
    }
}