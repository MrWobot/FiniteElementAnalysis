using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Mesh.Tetrahedral;

namespace FiniteElementAnalysis.Mesh.Interfaces
{
    public interface IMesh
    {
        public int NNodesPerElement { get; }
        public int NodePositionLength { get; }
        public BoundariesCollection Boundaries { get; }
        public VolumesCollection Volumes { get; }
        public INode[] Nodes { get; }
        public IElement[] Elements { get; }
        public Dictionary<int, int> MapNodeIdentifierToGlobalIndex { get; }
        public Dictionary<int, List<IElement>> MapNodeToElementsBelongsTo { get; }
        public bool IsPartOfResult { get; set; }
        public double GetThickness(double[] position);
        public abstract bool HasNonLinearBoundaries();
        /// <summary>
        /// Primitive is the standard FEM term for the boundary element one dimension lower than the domain element — face for 3D, edge for 2D.
        /// </summary>
        /// <param name="boundary"></param>
        /// <returns></returns>
        public abstract IBoundaryPrimitive[] GetPrimitivesForBoundary(Boundary boundary);
        public abstract IMesh Clone();

        public IEnumerable<IElement> GetElementsContainingPoint(double[] point);
    }
}
