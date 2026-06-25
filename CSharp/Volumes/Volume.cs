using Core.Graphics;
using Core.Maths.Tensors;
using System.Drawing;
using System.Text.RegularExpressions;
namespace FiniteElementAnalysis.Boundaries
{
    public abstract class Volume
    {
        public string Name { get; }
        public Vector3D[] VolumeMarkerPoints { get; set; }
        public int Region { get; set; }
        public double MaximumVolumeConstraint { get; }
        public RGBF Color { get; set; } = RGBF.Black();
        protected Volume(string name, double maximumTetrahedralVolumeConstraint) {
            Name = name;
            MaximumVolumeConstraint = maximumTetrahedralVolumeConstraint;
        }
    }
}