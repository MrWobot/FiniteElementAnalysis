
using Core.FileSystem;
using FiniteElementAnalysis;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Boundaries.Thermal;
using FiniteElementAnalysis.Plotting;
using FiniteElementAnalysis.Polyhedrals;
using FiniteElementAnalysis.Solvers;
using System.Reflection;
using Core.Enums;
using Core.Timing;
using Core.Maths;
using GMRES;
using System;
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
namespace StaticHeatConductionExperimentation
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
                new FixedTemperatureDirichletBoundary("Boundary2", temperatureK: 100),
                new FixedTemperatureDirichletBoundary("Boundary3", 0),
                new AdiabaticBoundaryInsulated("Boundary1")
                //new MaterialBoundary("MaterialBoundary")
            );
            VolumesCollection volumes = new VolumesCollection(
                //new StaticHeatVolume("VolumeA", thermalConductivity: 401)
               new StaticHeatVolume("VolumeB", thermalConductivity: 401)
            );
            PolyhedralDomain domain = ObjFileToPoly.Read(
                MeshesResource.BeamWithHeatSource, volumes, boundaries,
                out Dictionary<int, Boundary> mapMarkerToBoundary, Units.Meters, maxDistanceNodeMergeMeters:0.000001);
            using (TemporaryDirectory temporaryDirectory = new TemporaryDirectory())
            {
                using (TemporaryWorkingDirectoryManager workingDirectoryManager = new TemporaryWorkingDirectoryManager())
                {
                    string projectDirectory = DirectoryHelper.GetProjectDirectory();
                    string outputDirectory = Path.Combine(projectDirectory, "output");
                    Console.WriteLine($"Output to: \"{outputDirectory}\"");
                    DirectoryHelper.DeleteRecursively(outputDirectory, throwOnError: false);
                    string polyFilePath = Path.Combine(temporaryDirectory.AbsolutePath, "mesh.poly");
                    PolyFileGenerator.Generate(polyFilePath, domain);
                    using (Tetgen tetgen = new Tetgen())
                    {
                        TetgenGenerateMeshResult generateMeshResult = tetgen.GenerateTetrahedralMesh(polyFilePath,
                            new TetgenParameters
                            {
                                RefineMesh = true,
                                MaximumTetrahedralVolumeConstraint = 0.000001,
                                CheckConsistencyOfFinalMesh = true
                            });
                        if (generateMeshResult.ExitCode != 0)
                        {
                            throw new Exception(generateMeshResult.Output);
                        }
                        TetrahedralMesh mesh = generateMeshResult.ToMesh(boundaries, volumes, mapMarkerToBoundary);
                        long startTimeSolve = TimeHelper.MillisecondsNow;
                        HeatConductionResult solverResult = new HeatConductionSolver().Solve(mesh, workingDirectoryManager,
                            solverMethod: SolverMethod.BlockMatrixInversionGpuOnly);
                        long timeTaken = TimeHelper.MillisecondsNow - startTimeSolve;

                        solverResult.Print();
                        ContourPlotHelper.Plot(mesh, 100, outputDirectory, "plot", PlotPlaneType.Z);
                        double valueTFrom = mesh.ElementsBVHTree.QueryBVH(new Core.Maths.Tensors.Vector3D(0.01, 0, 0)).First().NodeA.ScalarValue;
                        double valueMiddle = mesh.ElementsBVHTree.QueryBVH(new Core.Maths.Tensors.Vector3D(0.205, 0, 0)).First().NodeA.ScalarValue;
                        double valueTo = mesh.ElementsBVHTree.QueryBVH(new Core.Maths.Tensors.Vector3D(0.4, 0, 0)).First().NodeA.ScalarValue;
                        string plyFilePath =
                            Path.Combine(outputDirectory, "temperatures.ply");
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
                    }
                    Tetgen.CopyTetViewToDirectory(temporaryDirectory.AbsolutePath);
                    DirectoryHelper.CopyRecurively(temporaryDirectory.AbsolutePath, outputDirectory);
                }
            }
        }
    }
}