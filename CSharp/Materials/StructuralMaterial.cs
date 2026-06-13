namespace FiniteElementAnalysis.Materials
{
    /// <summary>
    ///For structural analysis, this class will include properties like Young's Modulus and Poisson's Ratio.
    /// </summary>
    public class StructuralMaterial : Material
    {
        public double YoungsModulus { get; set; } // Pa (Pascals)
        public double PoissonsRatio { get; set; }

        // Constructor
        public StructuralMaterial(
            string name = "Unknown",
            double youngsModulus = 0,
            double poissonsRatio = 0)
            : base(name)
        {
            YoungsModulus = youngsModulus;
            PoissonsRatio = poissonsRatio;
        }

        // Overridden method to include structural properties
        public override string GetMaterialDetails()
        {
            return $"{base.GetMaterialDetails()}\n" +
                   $"Young's Modulus: {YoungsModulus} Pa\n" +
                   $"Poisson's Ratio: {PoissonsRatio}";
        }
    }
}