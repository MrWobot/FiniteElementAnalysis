namespace FiniteElementAnalysis.Fields
{
    public sealed class VectorFieldResult : FieldResult
    {
        public VectorFieldResult(string name, double[] values, bool includeMagnitude = false) : base(name, values, 3)
        {
            if (values.Length % 3 != 0) {
                throw new ArgumentException($"{nameof(values)} length was not a multiple of 3");
            }
            int componentLength = values.Length / 3;
            double[] xS = new double[componentLength];
            double[] yS = new double[componentLength];
            double[] zS = new double[componentLength];
            double[]? magnitudes = includeMagnitude ? new double[componentLength] : null;
            int j = 0;
            for (int i = 0; i < componentLength; i++)
            {
                double x = values[j++];
                xS[i] = x;
                double y = values[j++];
                yS[i] = y;
                double z = values[j++];
                zS[i] = z;
                if (includeMagnitude)
                {
                    double magnitude = Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2) + Math.Pow(z, 2));
                    magnitudes![i] = magnitude;
                }
            }
            List<FieldResultProperty> fieldResultProperties = new List<FieldResultProperty> {
                new FieldResultProperty($"{name}_0", xS),
                new FieldResultProperty($"{name}_1", yS),
                new FieldResultProperty($"{name}_2", zS)
            };
            if (includeMagnitude) {
                fieldResultProperties.Add(
                    new FieldResultProperty($"{name}_magnitude", magnitudes!)
                );
            }
            _FieldResultProperties = fieldResultProperties.ToArray();
        }
    }
}