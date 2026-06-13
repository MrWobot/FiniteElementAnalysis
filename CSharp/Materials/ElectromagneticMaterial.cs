namespace FiniteElementAnalysis.Materials
{
    /// <summary>
    /// For electromagnetic analysis, this class will include properties like Electrical Conductivity and Magnetic Permeability.
    /// </summary>
    public class ElectromagneticMaterial : Material
    {
        public double ElectricalConductivity { get; set; } // S/m
        public double MagneticPermeability { get; set; } // H/m (Henries per meter)

        // Constructor
        public ElectromagneticMaterial(
            string name = "Unknown",
            double electricalConductivity = 0,
            double magneticPermeability = 0)
            : base(name)
        {
            ElectricalConductivity = electricalConductivity;
            MagneticPermeability = magneticPermeability;
        }

        // Overridden method to include electromagnetic properties
        public override string GetMaterialDetails()
        {
            return $"{base.GetMaterialDetails()}\n" +
                   $"Electrical Conductivity: {ElectricalConductivity} S/m\n" +
                   $"Magnetic Permeability: {MagneticPermeability} H/m";
        }
    }
}