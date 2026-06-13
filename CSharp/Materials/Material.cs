namespace FiniteElementAnalysis.Materials
{
    public class Material
    {
        public string Name { get; set; }

        // Constructor
        public Material(string name = "Unknown")
        {
            Name = name;
        }

        // Optional: Method to display material details
        public virtual string GetMaterialDetails()
        {
            return $"Material: {Name}";
        }
    }
}