using Core.Graphics;
using System.Globalization;
using System.Text;

namespace FiniteElementAnalysis.Mesh.Parsing.MtlFiles
{

    public class MtlFileParser
    {
        private static readonly CultureInfo CULTURE_FLOAT = CultureInfo.InvariantCulture;

        public static MtlFile Read(string filePath)
        {
            string content = File.ReadAllText(filePath);
            return ReadFromContent(content);
        }
        public static MtlFile Read(byte[] bytes)
        {

            string content = Encoding.ASCII.GetString(bytes);
            return ReadFromContent(content);
        }
        public static MtlFile ReadFromContent(string content)
        {
            string[] lines = content.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            string? currentName = null;
            RGBF? currentKa = null;
            RGBF? currentKd = null;
            RGBF? currentKs = null;
            var materials = new List<Material>();
            var addCurrentMaterialIfHas = () =>
            {
                if (currentName != null)
                {
                    materials.Add(new Material(currentName, currentKa, currentKd, currentKs));
                }
            };
            foreach (var line in lines)
            {
                string[] words = line.Split(" ");
                switch (words[0].ToLower())
                {
                    case "newmtl":
                        addCurrentMaterialIfHas();
                        currentName = words[1];
                        break;
                    case "ka":
                        currentKa = ParseMtlColor(words);
                        break;
                    case "kd":
                        currentKd = ParseMtlColor(words);
                        break;
                    case "ks":
                        currentKs = ParseMtlColor(words);
                        break;
                    default:
                        break;
                }
            }
            addCurrentMaterialIfHas();
            return new MtlFile(materials.ToArray());
        }
        private static RGBF ParseMtlColor(string[] words)
        {
            return new RGBF(float.Parse(words[1], CULTURE_FLOAT), float.Parse(words[2], CULTURE_FLOAT), float.Parse(words[3], CULTURE_FLOAT));
        }
    }
}
