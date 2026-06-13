
using System;
using System.Collections.Generic;
namespace FiniteElementAnalysis.Boundaries.Electrostatic
{
    public class FixedSurfaceChargeDensityNeumannBoundary : LinearBoundary
    {
        public double ChargeDensityCoulombsPerMeterSquared { get; }
        public FixedSurfaceChargeDensityNeumannBoundary(
            string name, double chargeDensityCoulombsPerMeterSquared)
            : base(BoundaryConditionType.FixedSurfaceChargeDensityNeumannBoundary, name)
        {
            ChargeDensityCoulombsPerMeterSquared = chargeDensityCoulombsPerMeterSquared;
        }
    }
}