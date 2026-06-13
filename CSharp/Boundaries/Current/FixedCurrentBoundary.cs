
using Core.Maths.Tensors;
using System;
using System.Collections.Generic;
namespace FiniteElementAnalysis.Boundaries
{
    public class FixedCurrentBoundary : LinearBoundary
    {
        public double Current { get; }
        public FixedCurrentBoundary(
            string name, double current)
            : base(BoundaryConditionType.FixedCurrentBoundary, name, true)
        {
            Current = current;
        }
    }
}