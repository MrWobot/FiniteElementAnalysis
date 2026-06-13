
using System;
using System.Collections.Generic;
namespace FiniteElementAnalysis.Boundaries
{
    public class MeasurementBoundary : Boundary
    {
        public override bool IsNonLinear => false;
        public MeasurementBoundary(
            string name)
            :base(BoundaryConditionType.MeasurementBoundary, name, twoElementsAllowed:true) {
        }
    }
}