namespace FiniteElementAnalysis.Boundaries
{
    public class StaticHeatVolume : Volume
    {
        public double ThermalConductivity { get; }
        public StaticHeatVolume(string groupIdentiferRegularExpression
            , double thermalConductivity, double maximumTetrahedralVolumeConstraint = -1) 
            :base(groupIdentiferRegularExpression, maximumTetrahedralVolumeConstraint)
        {
            ThermalConductivity = thermalConductivity;
        }
    }
}