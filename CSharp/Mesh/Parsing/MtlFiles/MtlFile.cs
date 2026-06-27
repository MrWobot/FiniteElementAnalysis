namespace FiniteElementAnalysis.Mesh.Parsing.MtlFiles
{

    public class MtlFile
    {
        public Material[] Materials { get; }
        public MtlFile(Material[] materials)
        {
            Materials = materials;
        }
    }
}
