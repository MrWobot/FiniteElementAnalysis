namespace FiniteElementAnalysis.Mesh.Parsing.Planar.PlanarDomainFromObjMtlHelper_Internal
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
