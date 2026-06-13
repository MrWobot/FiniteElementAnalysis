
using Core.Maths.Tensors;
using System;
using System.Collections.Generic;
namespace FiniteElementAnalysis.Boundaries
{
    public class FixedVoltageDirichletBoundary : LinearBoundary
    {
        public double Voltage { get; }
        public FixedVoltageDirichletBoundary(
            string name, double voltage)
            : base(BoundaryConditionType.FixedVoltageDirichletBoundary, name, true)
        {
            Voltage = voltage;
        }
    }
}