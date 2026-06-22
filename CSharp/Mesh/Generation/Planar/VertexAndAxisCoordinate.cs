namespace FiniteElementAnalysis.Mesh.Generation.Planar
{
    internal struct VertexAndAxisCoordinate
    {
        public RawVertex Vertex { get; }
        public double AxisCoordinate { get; }
        public VertexAndAxisCoordinate(RawVertex vertex, double axisCoordinate)
        {
            Vertex = vertex;
            AxisCoordinate = axisCoordinate;
        }
    }
}
