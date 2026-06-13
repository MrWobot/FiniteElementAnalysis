namespace FiniteElementAnalysis.Boundaries
{
    public class StaticMagneticConductionVolume : Volume
    {
        public double Permeability { get; }
        public StaticMagneticConductionVolume(string name, double permeability, double maximumTetrahedralVolumeConstraint = -1) 
            :base(name, maximumTetrahedralVolumeConstraint)
        {
            Permeability = permeability;
        }
    }
}