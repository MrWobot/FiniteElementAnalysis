namespace FiniteElementAnalysis.Boundaries
{
    public class StaticCurrentVolume:Volume
    {
        public double Conductivity { get; }
        public StaticCurrentVolume(string name, double conductivity, double maximumTetrahedronVolumeConstraint = -1) 
            : base(name, maximumTetrahedronVolumeConstraint) {
            Conductivity = conductivity;
        }
    }
}