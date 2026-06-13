using Core.Maths.Tensors;
using System;
using System.Collections.Generic;
namespace FiniteElementAnalysis.Boundaries
{
    public class DisplacementBoundaryCondition : Boundary
    {
        public Vector3D Displacement { get; set; }

        public DisplacementBoundaryCondition(Vector3D displacement, List<Node> appliedNodes)
            : base(BoundaryConditionType.Temperature, appliedNodes)
        {
            Displacement = displacement;
        }

        public override void Apply()
        {
            foreach (var node in AppliedNodes)
            {
                node.Displacement = Vector3D.Add(node.Displacement, Displacement);
            }
        }
    }
}