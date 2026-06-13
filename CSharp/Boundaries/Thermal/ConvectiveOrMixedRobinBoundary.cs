
using System;
using System.Collections.Generic;
namespace FiniteElementAnalysis.Boundaries.Thermal
{
    public class ConvectiveOrMixedRobinBoundary : LinearBoundary
    {
        public double HeatFluxWattsPerMeterSquare { get; }
        public double ConvectiveHeatTransferCoefficientH { get; }
        public double AmbientTemperature { get; }
        public ConvectiveOrMixedRobinBoundary(
            string name, double convectiveHeatTransferCoefficientH, double ambientTemperature)
            : base(BoundaryConditionType.ConvectiveOrMixedRobinBoundary, name)
        {
            ConvectiveHeatTransferCoefficientH = convectiveHeatTransferCoefficientH;
            AmbientTemperature = ambientTemperature;
        }
    }
}