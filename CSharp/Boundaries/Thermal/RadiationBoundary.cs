
using System;
using System.Collections.Generic;
namespace FiniteElementAnalysis.Boundaries.Thermal
{
    public class RadiationBoundary : NonLinearBoundary
    {
        public double HeatFluxWattsPerMeterSquare { get; }
        public double EmissivityOfSurface { get; }
        public double AmbientTemperature { get; }
        public RadiationBoundary(
            string name,
            double emissivityOfSurface,
            double surroundingTemperature)
            : base(BoundaryConditionType.RadiationBoundary, name)
        {
            EmissivityOfSurface = emissivityOfSurface;
            AmbientTemperature = surroundingTemperature;
        }
    }
}