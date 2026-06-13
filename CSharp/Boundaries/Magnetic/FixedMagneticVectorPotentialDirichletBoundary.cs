
using Core.Maths.Tensors;
using System;
using System.Collections.Generic;
namespace FiniteElementAnalysis.Boundaries
{
    public class FixedMagneticVectorPotentialDirichletBoundary : LinearBoundary
    {
        public Vector3D MagneticVectorPotential { get; }
        public FixedMagneticVectorPotentialDirichletBoundary(
            string name, Vector3D magneticVectorPotential)
            : base(BoundaryConditionType.FixedMagneticVectorPotentialDirichletBoundary, name, true)
        {
            MagneticVectorPotential = magneticVectorPotential;
        }
    }
}