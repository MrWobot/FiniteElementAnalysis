
using System;
using System.Collections.Generic;
namespace FiniteElementAnalysis.Boundaries
{
    public abstract class Boundary
    {
        public BoundaryConditionType BoundaryConditionType { get; }
        public string Name { get; }
        public abstract bool IsNonLinear { get; }
        /// <summary>
        /// For a boundary on a face between two elements. For most types of boundary this should be false
        /// </summary>
        public bool MultipleElementsAllowed { get; }
        protected Boundary(BoundaryConditionType type, string name, bool twoElementsAllowed)
        {
            Name = name;
            BoundaryConditionType = type;
            MultipleElementsAllowed = twoElementsAllowed;
        }   
        protected Boundary(BoundaryConditionType type, string name)
        {
            Name = name;
            BoundaryConditionType = type;
            MultipleElementsAllowed = false;
        }   
    }
}