using Core.Graphics;

namespace FiniteElementAnalysis.MtlFiles
{

    public class Material
    {
        public string Name { get; }
        public RGBF? Ka { get; }
        public RGBF? Kd { get; }
        public RGBF? Ks { get; }
        public Material(string name, RGBF? ka, RGBF? kd, RGBF? ks)
        {
            Name = name;
            Ka = ka;
            Kd = kd;
            Ks = ks;
        }
    }
}
