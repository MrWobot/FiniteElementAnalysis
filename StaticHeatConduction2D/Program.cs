
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
using FiniteElementAnalysis.Results;
using FiniteElementAnalysis.Mesh.Tetrahedral;
using FiniteElementAnalysis.Mesh.Generation;
using FiniteElementAnalysis.Setup;
namespace StaticHeatConduction2D
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
            BeamWithHeatSource();
        }
        private static void BeamWithHeatSource()
        {
            BoundariesCollection boundaries = new BoundariesCollection(
                new AdiabaticBoundaryInsulated("Boundary1"),
                new FixedTemperatureDirichletBoundary("Boundary2", temperatureK: 100),
                new FixedTemperatureDirichletBoundary("Boundary3", 0),
                new FixedTemperatureDirichletBoundary("Boundary4", 0)
            //new MaterialBoundary("MaterialBoundary")
            );
            VolumesCollection volumes = new VolumesCollection(
               new StaticHeatVolume("VolumeA", thermalConductivity: 401),
               new StaticHeatVolume("VolumeB", thermalConductivity: 401)
            );
            using (Setup2D setup2D = SetupHelper.Setup2D(
                File.ReadAllBytes("C:\\repos\\FiniteElementAnalysis\\StaticHeatConduction2D\\Meshes\\ExampleStaticHeatConduction2D.obj"),
                File.ReadAllBytes("C:\\repos\\FiniteElementAnalysis\\StaticHeatConduction2D\\Meshes\\ExampleStaticHeatConduction2D.mtl"),
                boundaries,
                volumes,
                maxDistanceNodeMergeMeters: 0.000001,
                units: Units.Meters))
            {
                /*
                var mesh = setup2D.Mesh;
                HeatConductionResult solverResult = new HeatConductionSolver().Solve(mesh, setup3D.WorkingDirectoryManager,
                    solverMethod: SolverMethod.BlockMatrixInversionGpuOnly);
                solverResult.Print();
                ContourPlotHelper.Plot(mesh, 100, setup3D.OutputDirectory, "plot", PlotPlaneType.Z);
                double valueTFrom = mesh.ElementsBVHTree.QueryBVH(new Core.Maths.Tensors.Vector3D(0.01, 0, 0)).First().NodeA.ScalarValue;
                double valueMiddle = mesh.ElementsBVHTree.QueryBVH(new Core.Maths.Tensors.Vector3D(0.205, 0, 0)).First().NodeA.ScalarValue;
                double valueTo = mesh.ElementsBVHTree.QueryBVH(new Core.Maths.Tensors.Vector3D(0.4, 0, 0)).First().NodeA.ScalarValue;
                string plyFilePath =
                    Path.Combine(setup3D.OutputDirectory, "temperatures.ply");
                PlyWriter.WritePlyFile(
                    plyFilePath,
                    mesh.Nodes,
                    mesh.BoundaryFaces,
                    new ScalarFieldResult(
                        "temperature",
                        solverResult.NodalTemperatures
                    )
                );
                CloudCompareHelper.Open(plyFilePath);
                Tetgen.CopyTetViewToDirectory(setup3D.TemporaryDirectory.AbsolutePath);
                DirectoryHelper.CopyRecurively(setup3D.TemporaryDirectory.AbsolutePath, setup3D.OutputDirectory);
                */
            }
        }
    }
}