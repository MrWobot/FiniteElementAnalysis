namespace FiniteElementAnalysis.Materials
{
    /// <summary>
    /// For thermal analysis, this class will include properties like Thermal Conductivity and Specific Heat Capacity.
    /// </summary>
    public class ThermalMaterial : Material
    {
        public double ThermalConductivity { get; set; } // W/(m·K)
        public double SpecificHeatCapacity { get; set; } // J/(kg·K)
        public double ThermalExpansionCoefficient { get; set; } // 1/K

        // Constructor
        public ThermalMaterial(
            string name = "Unknown",
            double thermalConductivity = 0,
            double specificHeatCapacity = 0,
            double thermalExpansionCoefficient = 0)
            : base(name)
        {
            ThermalConductivity = thermalConductivity;
            SpecificHeatCapacity = specificHeatCapacity;
            ThermalExpansionCoefficient = thermalExpansionCoefficient;
        }

        // Overridden method to include thermal properties
        public override string GetMaterialDetails()
        {
            return $"{base.GetMaterialDetails()}\n" +
                   $"Thermal Conductivity: {ThermalConductivity} W/(m·K)\n" +
                   $"Specific Heat Capacity: {SpecificHeatCapacity} J/(kg·K)\n" +
                   $"Thermal Expansion Coefficient: {ThermalExpansionCoefficient} 1/K";
        }
    }
}