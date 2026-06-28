using Core.Maths.Tensors;

namespace FiniteElementAnalysis.Boundaries.Statics
{
    public class SurfaceTractionNeumannBoundary3D : SurfaceTractionNeumannBoundaryBase
    {
        public double Tx => Tractions.X;
        public double Ty => Tractions.Y;
        public double Tz  =>Tractions.Z;
        public Vector3D Tractions { get; }
        public SurfaceTractionNeumannBoundary3D(string name, Vector3D tractions)
            : base(name)
        {
            Tractions = tractions;
        }
    }
}