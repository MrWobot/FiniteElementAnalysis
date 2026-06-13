
namespace FiniteElementAnalysis.Boundaries
{
    public class MultiComponentVolume : Volume
    {
        public VolumeComponent[] Components { get; }
        protected MultiComponentVolume(string name, double maximumTetrahedralVolumeConstraint,
            params VolumeComponent[] components)
        :base(name, maximumTetrahedralVolumeConstraint)
        {
            Components = components;
        }
    }
}