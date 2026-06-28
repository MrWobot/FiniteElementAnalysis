using Core.Maths.Tensors;

namespace FiniteElementAnalysis.Boundaries.Statics
{
    public class SurfaceTractionNeumannBoundary2D : SurfaceTractionNeumannBoundaryBase
    {
        public double Tx => Tractions.X;
        public double Ty => Tractions.Y;
        public Vector2D Tractions { get; }
        public SurfaceTractionNeumannBoundary2D(string name, Vector2D tractions)
            : base(name)
        {
            Tractions = tractions;
        }
    }
}