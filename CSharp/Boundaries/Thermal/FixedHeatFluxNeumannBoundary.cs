
using System;
using System.Collections.Generic;
namespace FiniteElementAnalysis.Boundaries.Thermal
{
    public class FixedHeatFluxNeumannBoundary : LinearBoundary
    {
        public double HeatFluxWattsPerMeterSquare { get; }
        public FixedHeatFluxNeumannBoundary(
            string name, double heatFluxWattsPerMeterSquare)
            : base(BoundaryConditionType.FixedHeatFluxNeumannBoundary, name)
        {
            HeatFluxWattsPerMeterSquare = heatFluxWattsPerMeterSquare;
        }
    }
}