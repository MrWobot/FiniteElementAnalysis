using MathNet.Numerics;
using ScottPlot.AxisRules;

namespace FiniteElementAnalysis.Boundaries
{
    public class ElectrostaticsVolume : Volume
    {
        public double TotalPermittivity { get; }
        public ElectrostaticsVolume(string groupIdentiferRegularExpression
            , double totalPermittivity, double maximumTetrahedralVolumeConstraint = -1) 
            :base(groupIdentiferRegularExpression, maximumTetrahedralVolumeConstraint)
        {
            TotalPermittivity = totalPermittivity;
        }
        public static ElectrostaticsVolume ForRelativePermittivity(string groupIdentiferRegularExpression
            , double relativePermittivity, double maximumTetrahedralVolumeConstraint = -1)
        {
            return new ElectrostaticsVolume(groupIdentiferRegularExpression,
                relativePermittivity * Constants.ElectricPermittivity,
                maximumTetrahedralVolumeConstraint);
        }
        public static ElectrostaticsVolume ForTotalPermittivity(string groupIdentiferRegularExpression
            , double totalPermittivity, double maximumTetrahedralVolumeConstraint = -1) 
        {
            return new ElectrostaticsVolume(groupIdentiferRegularExpression,
                totalPermittivity,
                maximumTetrahedralVolumeConstraint);
        }
    }
}