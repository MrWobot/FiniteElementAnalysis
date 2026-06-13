namespace FiniteElementAnalysis.Boundaries
{
    public class StaticLinearElasticVolume : Volume
    {
        public double YoungsModulus { get; }
        public double PoissonsRatio { get; }

        public StaticLinearElasticVolume(string name, double youngsModulus, double poissonsRatio, double maximumTetrahedralVolumeConstraint = -1)
            : base(name, maximumTetrahedralVolumeConstraint)
        {
            YoungsModulus = youngsModulus;
            PoissonsRatio = poissonsRatio;
        }
        private double[][] _ElasticityMatrix;
        public double[][] ElasticityMatrix { get {
                if (_ElasticityMatrix == null) {
                    _ElasticityMatrix = ComputeElasticityMatrix(this);
                }
                return _ElasticityMatrix;
            } }
        protected double[][] ComputeElasticityMatrix(StaticLinearElasticVolume volume)
        {
            double E = volume.YoungsModulus;
            double ν = volume.PoissonsRatio;
            double factor = E / ((1 + ν) * (1 - 2 * ν));

            double a = factor * (1 - ν);
            double b = factor * ν;
            double c = factor * (1 - 2 * ν) / 2;

            return new double[][]
            {
                new double[] { a, b, b, 0, 0, 0 },
                new double[] { b, a, b, 0, 0, 0 },
                new double[] { b, b, a, 0, 0, 0 },
                new double[] { 0, 0, 0, c, 0, 0 },
                new double[] { 0, 0, 0, 0, c, 0 },
                new double[] { 0, 0, 0, 0, 0, c }
            };
        }
    }
}
