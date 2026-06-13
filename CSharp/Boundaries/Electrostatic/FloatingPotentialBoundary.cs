
using System;
using System.Collections.Generic;
namespace FiniteElementAnalysis.Boundaries.Electrostatic
{
    public class FloatingPotentialBoundary : LinearBoundaryWithAdditionalRowsColumns
    {
        public double Potential{ get; }

        public FloatingPotentialBoundary(
            string name, double potential)
            : base(BoundaryConditionType.FloatingPotentialBoundary, name,
                  nAdditionalRowsColumns:1)
        {
            Potential = potential;
        }
    }
}