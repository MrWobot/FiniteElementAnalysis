namespace FiniteElementAnalysis.MtlFiles
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
