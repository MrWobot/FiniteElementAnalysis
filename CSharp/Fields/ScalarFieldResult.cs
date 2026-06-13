namespace FiniteElementAnalysis.Fields
{
    public sealed class ScalarFieldResult : FieldResult
    {
        public ScalarFieldResult(string name, double[] values) : base(name, values, 1)
        {
            _FieldResultProperties = new FieldResultProperty[] { new FieldResultProperty(name, values) };
        }
        public static ScalarFieldResult MagnitudeFromVectorField(string name, double[] vectorValues)
        {
            int vectorsLength = vectorValues.Length;
            if (vectorsLength% 3 != 0) {
                throw new Exception($"{vectorValues} length was not a multiple of 3");
            }
            double[] scalars = new double[vectorsLength / 3];
            int i = 0;
            int j = 0;
            while(i < vectorsLength)
            {
                double a = vectorValues[i++];
                double b = vectorValues[i++];
                double c = vectorValues[i++];
                double magnitude = Math.Sqrt(Math.Pow(a, 2) + Math.Pow(b, 2) + Math.Pow(c, 2));
                scalars[j++] = magnitude;
            }
            return new ScalarFieldResult(name, scalars);
        }
    }
}