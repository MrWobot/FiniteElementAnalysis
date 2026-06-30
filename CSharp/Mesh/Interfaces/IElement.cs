using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Fields;

namespace FiniteElementAnalysis.Mesh.Interfaces
{
    public interface IElement
    {
        public int Identifier { get; }
        public IReadOnlyList<INode> Nodes { get; }
        public Volume VolumeBelongsTo { get; }
        /// <summary>
        /// This is the volume for a 3d element such as a tetrahedral. Or the area for a 2d element like a triangle
        /// </summary>
        public double Measure { get; }
        public double[] Centroid { get; }
        public bool IsPointInside(double[] point);
        public double[] InterpolateValueAtPoint(double[] point, int nDegreesFreedom);
        public abstract double[][] GetBMatrix(FieldDOFInfo fieldDOFInfo);
        public abstract double[][] GetBMatrix(int nFieldComponents, FieldOperationType fieldOperationType, int nDegreesOfFreedom);
        public abstract double[][] GetBMatrixTranspose(FieldDOFInfo fieldDOFInfo);
        public abstract double[][] GetBMatrixTranspose(int nFieldComponents, FieldOperationType fieldOperationType, int nDegreesOfFreedom);
    }
}
