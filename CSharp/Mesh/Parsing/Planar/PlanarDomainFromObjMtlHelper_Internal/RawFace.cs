namespace FiniteElementAnalysis.Mesh.Parsing.Planar.PlanarDomainFromObjMtlHelper_Internal
{
    internal class RawFace
    {
        public RawVertex[] Vertices { get; }
        public string? MaterialName { get; }
        public RawFace(RawVertex[] vertices, string? materialName)
        {
            Vertices = vertices;
            MaterialName = materialName;
        }
    }
}
