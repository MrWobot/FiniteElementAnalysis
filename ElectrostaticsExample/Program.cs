
using Core.FileSystem;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Boundaries.Thermal;
using FiniteElementAnalysis.Plotting;
using FiniteElementAnalysis.Polyhedrals;
using FiniteElementAnalysis.Solvers;
using Core.Enums;
using Core.Timing;
using FiniteElementAnalysis.Fields;
using InfernoDispatcher;
using Core.MemoryManagement;
using Shutdown;
using Logging;
using FiniteElementAnalysis.Ply;
using FiniteElementAnalysis.CloudCompare;
using FiniteElementAnalysis.Mesh.Tetrahedral;
using FiniteElementAnalysis.Setup;
using FiniteElementAnalysis.Boundaries.Electrostatic;
using FiniteElementAnalysis.Mesh.Refinement.Tetrahedral.Tetgen;
using FiniteElementAnalysis.Results.ThreeD;
namespace ElectrostaticsExample
{
    // Example usage:
    public class Program
    {
        /*
         * https://wias-berlin.de/software/tetgen/fformats.html
         * */
        public static void Main(string[] args)
        {
            ShutdownManager.Initialize(Environment.Exit, () => Logs.Default);
            GpuMemoryInfoNVML.Initialize();
            Dispatcher.InitializeWithNative(Console.WriteLine);

            BoundariesCollection boundaries = new BoundariesCollection(
                new FixedPotentialDirichletBoundary("Boundary2", 160000),
                new FixedPotentialDirichletBoundary("Boundary3", 0),
                new AdiabaticBoundaryInsulated("Boundary1"),
                //,
                new MaterialBoundary("MaterialBoundary")
            );
            VolumesCollection volumes = new VolumesCollection(
               ElectrostaticsVolume.ForRelativePermittivity("VolumeA", relativePermittivity:1.0006d , maximumTetrahedralVolumeConstraint:1e-9),
               ElectrostaticsVolume.ForRelativePermittivity("VolumeB", relativePermittivity: 1.0006d, maximumTetrahedralVolumeConstraint: 1e-11)
            );
            using (Setup3D setup3D = SetupHelper.Setup3D(
                File.ReadAllBytes("C:\\repos\\FiniteElementAnalysis\\ElectrostaticsExample\\Meshes\\SharpPointedElectrode.obj"),
                boundaries,
                volumes,
                maxDistanceNodeMergeMeters: 0.00000000001,
                globalMaximumTetrahedralVolumeConstraintMeters:1e-5,
                units: Units.Millimeters))
            {
                var mesh = setup3D.Mesh;
                ElectrostaticsResult3D solverResult = new ElectrostaticsSolver()
                    .Solve(mesh, setup3D.WorkingDirectoryManager,
                    solverMethod: SolverMethod.BlockMatrixInversionGpuOnly);
                solverResult.Print();
                string plyFilePath =
                    Path.Combine(setup3D.OutputDirectory, "potentials.ply");
                PlyWriter.Write(
                    plyFilePath,
                    mesh,
                    new ScalarFieldResult(
                        "potentials",
                        solverResult.Potentials
                    ),
                    new ScalarFieldResult(
                        "scalar_electric_field_intensities",
                        solverResult.GetNodalScalarElectricFieldIntensities()
                    )
                );
                CloudCompareHelper.Open(plyFilePath);
                Tetgen.CopyTetViewToDirectory(setup3D.TemporaryDirectory.AbsolutePath);
                DirectoryHelper.CopyRecurively(setup3D.TemporaryDirectory.AbsolutePath, setup3D.OutputDirectory);
            }
        }
    }
}