using Core.Maths.Tensors;
using System.Text.RegularExpressions;
namespace FiniteElementAnalysis.Boundaries
{
    public abstract class Volume
    {
        public string Name { get; }
        public Vector3D[] VolumeMarkerPoints { get; set; }
        public int Region { get; set; }
        public double MaximumTetrahedralVolumeConstraint { get; }
        protected Volume(string name, double maximumTetrahedralVolumeConstraint) {
            Name = name;
            MaximumTetrahedralVolumeConstraint = maximumTetrahedralVolumeConstraint;
        }
    }
}