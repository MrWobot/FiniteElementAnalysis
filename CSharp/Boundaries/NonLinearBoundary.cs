
using System;
using System.Collections.Generic;
namespace FiniteElementAnalysis.Boundaries
{
    public abstract class NonLinearBoundary : Boundary
    {
        public override bool IsNonLinear => true;
        protected NonLinearBoundary(BoundaryConditionType type, string name):base(type, name)
        {

        }   
    }
}