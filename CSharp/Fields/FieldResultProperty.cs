namespace FiniteElementAnalysis.Fields
{
    public class FieldResultProperty
    {
        public string Name { get; }
        public double[] Values { get; }
        public FieldResultProperty(string name, double[] values) {
            Name = name;
            Values = values;
        }
    }
}