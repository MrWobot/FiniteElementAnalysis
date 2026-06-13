
using System;
using System.Collections.Generic;
namespace FiniteElementAnalysis.Boundaries
{
    public class AdiabaticBoundaryInsulated : LinearBoundary
    {
        public double HeatFluxWattsPerMeterSquare { get;}
        public AdiabaticBoundaryInsulated(
            string name)
            :base(BoundaryConditionType.AdiabaticInsulatedBoundary, name) {
        }
    }
}