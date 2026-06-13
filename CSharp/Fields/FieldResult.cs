namespace FiniteElementAnalysis.Fields
{
    public abstract class FieldResult
    {
        public string Name { get; }

        public double[] Values { get; } 

        public int NComponents { get; }
        public int NNodes { get; }
        protected FieldResultProperty[] _FieldResultProperties;
        public FieldResultProperty[] FieldResultProperties => _FieldResultProperties;
        public FieldResult(string name, double[] values, int nValuesPerNode)
        {
            if (values.Length % nValuesPerNode != 0)
                throw new ArgumentException($"{nameof(values)} must be a multiple of {nameof(nValuesPerNode)}");
            Name = name;
            Values = values;
            NComponents = nValuesPerNode;
            NNodes = values.Length / nValuesPerNode;
        }
    }
}