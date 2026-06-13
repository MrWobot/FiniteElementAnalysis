using Core.FileSystem;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.MeshGeneration;
using FiniteElementAnalysis.Polyhedrals;
using FiniteElementAnalysis;
using FiniteElementAnalysis.Solvers;
using Core.Enums;
using FiniteElementAnalysis.Fields;
using InfernoDispatcher;
using Core.MemoryManagement;
using Shutdown;
using Logging;
using FiniteElementAnalysis.Ply;
using FiniteElementAnalysis.CloudCompare;
using SimpleCurrentDensityExample;
using FiniteElementAnalysis.Results;
using FiniteElementAnalysis.Mesh.Tetrahedral;
using FiniteElementAnalysis.Mesh.Generation;

namespace VoltageMultiplier
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
            //secondaryCurrent / 
            //  (8/*bobin segments*/* 0.005d * 0.005d);
            const string OPERATION_WINDING_CURRENT = "windingCurrent",
                WINDING_CURRENT_FIRST_BOUNDARY = "WindingCurrentFirstBoundary",
                WINDING_CURRENT_SECOND_BOUNDARY = "WindingCurrentSecondBoundary",
                WINDING_VOLUME = "WindingVolume";
            const double PERMEABILITY_FREE_SPACE = 4 * Math.PI * 1e-7,
                COPPER_WIRE_PERMEABILITY = PERMEABILITY_FREE_SPACE,
                THREED_PRINTED_RESIN_PERMEABILITY = PERMEABILITY_FREE_SPACE,
                PC40_PERMEABILITY = 2000 * PERMEABILITY_FREE_SPACE;
            BoundariesCollection boundaries = new BoundariesCollection(
                new MultipleOperationBoundary(
                    WINDING_CURRENT_FIRST_BOUNDARY,
                    OPERATION_WINDING_CURRENT,
                        new FixedVoltageDirichletBoundary(
                            WINDING_CURRENT_FIRST_BOUNDARY,
                            10
                        )
                    ),
                new MultipleOperationBoundary(
                    WINDING_CURRENT_SECOND_BOUNDARY,
                    OPERATION_WINDING_CURRENT,
                        new FixedCurrentBoundary(
                            WINDING_CURRENT_SECOND_BOUNDARY, 10
                        )
                    ),
                new AdiabaticBoundaryInsulated("AdiabaticBoundary")
            );
            double windingMaximumTetrahedralVolumeConstraint = 1e-9;/*1.25mm cubed*/
            VolumesCollection volumes = new VolumesCollection(
                new MultipleOperationVolume(
                    WINDING_VOLUME,
                    OPERATION_WINDING_CURRENT,
                        new StaticCurrentVolume(WINDING_VOLUME,
                            Conductivities.Copper),
                    maximumTetrahedralVolumeConstraint: windingMaximumTetrahedralVolumeConstraint
                )
            );
            PolyhedralDomain domain = ObjFileToPoly.Read(
                //File.ReadAllBytes("C:\\repos\\snippets\\CircuitAnalysis\\VoltageMultiplier\\Meshes\\TestWindings.obj")
                File.ReadAllBytes("C:\\repos\\snippets\\CircuitAnalysis\\SimpleCurrentDensityExample\\Meshes\\CheckingCurrentDensity.obj"), volumes, boundaries,
                out Dictionary<int, Boundary> mapMarkerToBoundary, Units.Millimeters,
                0.0001d);
            string tempDirectoryPath = "D:\\temp\\";
            using (TemporaryDirectory temporaryDirectory = TemporaryDirectory
                .InCustomTempDirectory(tempDirectoryPath))
            {
                using (TemporaryWorkingDirectoryManager workingDirectoryManager = new TemporaryWorkingDirectoryManager(
                        Path.Combine(temporaryDirectory.AbsolutePath, "workings")))
                {
                    string outputDirectory = Path.Combine(DirectoryHelper.GetProjectDirectory(), "output");
                    Console.WriteLine($"Output to: \"{outputDirectory}\"");
                    DirectoryHelper.DeleteRecursively(outputDirectory, throwOnError: false);
                    string polyFilePath = Path.Combine(temporaryDirectory.AbsolutePath, "mesh.poly");
                    PolyFileGenerator.Generate(polyFilePath, domain);
                    Tetgen.CopyTetViewToDirectory(temporaryDirectory.AbsolutePath);
                    using (Tetgen tetgen = new Tetgen())
                    {
                        TetgenGenerateMeshResult generateMeshResult = tetgen.GenerateTetrahedralMesh(
                            polyFilePath,
                            new TetgenParameters
                            {
                                RefineMesh = true,
                                MaximumTetrahedralVolumeConstraint = 0.0001,
                                CheckConsistencyOfFinalMesh = true
                            });
                        Console.WriteLine(generateMeshResult.Output);
                        if (generateMeshResult.ExitCode != 0)
                        {
                            string moreExceptionInfo = generateMeshResult.GetMoreExceptionInfo();
                            throw new Exception(generateMeshResult.Output);
                        }
                        TetrahedralMesh mesh = generateMeshResult.ToMesh(boundaries, volumes, mapMarkerToBoundary);
                        var volumeElements = mesh.Elements.GroupBy(e => e.VolumeName).Select(g => g.ToArray()).ToArray();
                        StaticCurrentConductionSolver staticCurrentSolver = new StaticCurrentConductionSolver();
                        TetrahedralMesh firstHalfWindingMesh = mesh.ToOperationSpecificMesh(
                            OPERATION_WINDING_CURRENT);

                        StaticCurrentConductionResult firstHalfWindingResult
                         = staticCurrentSolver.Solve(
                            firstHalfWindingMesh,
                            workingDirectoryManager,
                            OPERATION_WINDING_CURRENT,
                            cachedSolverResult: null);

                        firstHalfWindingResult.Print();

                        string voltagesPlyFilePath = Path.Combine(outputDirectory, "coilVoltags.ply");
                        PlyWriter.Write(
                            voltagesPlyFilePath,
                            firstHalfWindingMesh
                        );
                        //CloudCompareHelper.Open(voltagesPlyFilePath);
                        string currentDensitiesPlyFilePath = Path.Combine(outputDirectory, "coilCurrentDensities.ply");
                        PlyWriter.Write(
                            currentDensitiesPlyFilePath,
                            firstHalfWindingMesh,
                            firstHalfWindingResult.GetNodalVolumeCurrentDensities("current_density")
                        );
                        CloudCompareHelper.Open(currentDensitiesPlyFilePath);

                    }
                }

            }
        }
    }
}