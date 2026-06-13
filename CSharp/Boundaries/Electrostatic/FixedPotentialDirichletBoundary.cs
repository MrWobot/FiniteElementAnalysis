
using System;
using System.Collections.Generic;
namespace FiniteElementAnalysis.Boundaries.Electrostatic
{
    public class FixedPotentialDirichletBoundary : LinearBoundary
    {
        public double Potential { get; }
        public FixedPotentialDirichletBoundary(
            string name, double potential)
            : base(BoundaryConditionType.FixedPotentialDirichletBoundary, name)
        {
            Potential = potential;
        }
    }
}