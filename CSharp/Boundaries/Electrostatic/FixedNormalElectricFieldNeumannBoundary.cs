
using System;
using System.Collections.Generic;
namespace FiniteElementAnalysis.Boundaries.Electrostatic
{
    public class FixedNormalElectricFieldNeumannBoundary : LinearBoundary
    {
        public double VoltsPerMeter{ get; }
        public FixedNormalElectricFieldNeumannBoundary(
            string name, double voltsPerMeter)
            : base(BoundaryConditionType.FixedNormalElectricFieldNeumannBoundary, name)
        {
            VoltsPerMeter = voltsPerMeter;
        }
    }
}