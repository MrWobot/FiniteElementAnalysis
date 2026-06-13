using Core.FileSystem;
using FiniteElementAnalysis.MeshGeneration;
using Logging;
using System.Diagnostics;
using System.Text;

namespace FiniteElementAnalysis.Mesh.Generation
{
    public class TetgenParameters
    {
        public bool RefineMesh { get; set; }
        public double? MaximumTetrahedralVolumeConstraint { get; set; }
        public bool CheckConsistencyOfFinalMesh { get; set; }
    }
}