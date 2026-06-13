
using System;
using System.Collections.Generic;
namespace FiniteElementAnalysis.Boundaries
{
    public class MaterialBoundary : Boundary
    {
        public override bool IsNonLinear => false;
        public MaterialBoundary(
            string name)
            :base(BoundaryConditionType.MaterialBoundary, name, twoElementsAllowed:true) {
        }
    }
}