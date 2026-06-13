
using Core.Maths.Tensors;
using System;
using System.Collections.Generic;

namespace FiniteElementAnalysis.Boundaries
{
    public class FixedBoundaryCondition : Boundary
    {
        public FixedBoundaryCondition(List<Node> appliedNodes)
            : base(BoundaryConditionType.Fixed, appliedNodes)
        {
        }

        public override void Apply()
        {
            foreach (var node in AppliedNodes)
            {
                node.Displacement = new Vector3D(0, 0, 0);
            }
        }
    }
}