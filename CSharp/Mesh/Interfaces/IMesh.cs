using FiniteElementAnalysis.Boundaries;

namespace FiniteElementAnalysis.Mesh.Interfaces
{
    public interface IMesh
    {
        public int NNodesPerElement { get; }
        public int NodePositionLength { get; }
        public BoundariesCollection Boundaries { get; }
        public VolumesCollection Volumes { get; }
        public IReadOnlySet<INode> Nodes { get; }
        public IReadOnlySet<IElement> Elements { get; }
        public int GetGlobalIndexForNode(int nodeIdentifier);
        public IReadOnlySet<IElement> GetElementsThatNodeBelongsTo(int nodeIdentifier);
        public bool IsPartOfResult { get; set; }
        public double GetThickness(double[] position);
        public abstract bool HasNonLinearBoundaries();
        /// <summary>
        /// Primitive is the standard FEM term for the boundary element one dimension lower than the domain element — face for 3D, edge for 2D.
        /// </summary>
        /// <param name="boundary"></param>
        /// <returns></returns>
        public abstract IReadOnlyList<IBoundaryPrimitive> GetPrimitivesForBoundary(Boundary boundary);
        public abstract IMesh Clone();

        public IEnumerable<IElement> GetElementsContainingPoint(double[] point);
    }
}
