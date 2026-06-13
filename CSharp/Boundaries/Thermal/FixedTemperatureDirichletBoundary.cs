
using System;
using System.Collections.Generic;
namespace FiniteElementAnalysis.Boundaries.Thermal
{
    public class FixedTemperatureDirichletBoundary : LinearBoundary
    {
        public double TemperatureK { get; }
        public FixedTemperatureDirichletBoundary(
            string name, double temperatureK)
            : base(BoundaryConditionType.FixedTemperatureDirichletBoundary, name)
        {
            TemperatureK = temperatureK;
        }
    }
}