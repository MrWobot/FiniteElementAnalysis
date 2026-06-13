using Core.Maths.Tensors;
using System;
using System.Collections.Generic;
namespace FiniteElementAnalysis.Boundaries
{
    public class ForceBoundaryCondition : Boundary
    {
        public Vector3D Force { get; set; }

        public ForceBoundaryCondition(Vector3D force, List<Node> appliedNodes)
            : base(BoundaryConditionType.Force, appliedNodes)
        {
            Force = force;
        }

        public override void Apply()
        {
            foreach (var node in AppliedNodes)
            {
                node.Force = Vector3D.Add(node.Force, Force);
            }
        }
    }
}