using Core.FileSystem;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.MeshGeneration;
using FiniteElementAnalysis.Polyhedrals;
using FiniteElementAnalysis;
using FiniteElementAnalysis.Solvers;
using Core.Enums;
using FiniteElementAnalysis.SourceRegions;
using FiniteElementAnalysis.Fields;
using InfernoDispatcher;
using Core.MemoryManagement;
using Shutdown;
using Logging;
using FiniteElementAnalysis.Ply;
using FiniteElementAnalysis.CloudCompare;
using Core.Maths.Tensors;
using FiniteElementAnalysis.Boundaries.Magnetic;
using FiniteElementAnalysis.Results;
using FiniteElementAnalysis.Mesh.Tetrahedral;
using FiniteElementAnalysis.Mesh.Generation;

namespace SimpleMagneticConductionAnalysis
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
                OPERATION_MAGNETIC_FIELD = "MagneticField",
                WINDING_CURRENT_FIRST_BOUNDARY = "WindingCurrentFirstBoundary",
                WINDING_CURRENT_SECOND_BOUNDARY = "WindingCurrentSecondBoundary",
                FAR_FIELD_MAGNETIC_BOUNDARY = "FarFieldMagneticBoundary",
                WINDING_VOLUME = "WindingVolume",
                FREE_SPACE_VOLUME = "FreeSpaceVolume";
            const double PERMEABILITY_FREE_SPACE = 4 * Math.PI * 1e-7,
                COPPER_WIRE_PERMEABILITY = PERMEABILITY_FREE_SPACE,
                THREED_PRINTED_RESIN_PERMEABILITY = PERMEABILITY_FREE_SPACE;
            string OUTPUT_DIRECTORY = Path.Combine(DirectoryHelper.GetProjectDirectory(), "output");
            string MAGNETIC_FIELD_PLY_FILE_PATH = Path.Combine(OUTPUT_DIRECTORY, "magneticField.ply");
            string CURRENT_DENSITIES_PLY_FILE_PATH = Path.Combine(OUTPUT_DIRECTORY, "coilCurrentDensities.ply");
            string VOLTAGES_PLY_FILE_PATH = Path.Combine(OUTPUT_DIRECTORY, "coilVoltags.ply");
            string VOLTAGES_2_PLY_FILE_PATH = Path.Combine(OUTPUT_DIRECTORY, "coilVoltags2.ply");

            BoundariesCollection boundaries = new BoundariesCollection(
                new MultipleOperationBoundary(
                    WINDING_CURRENT_FIRST_BOUNDARY,
                    OPERATION_WINDING_CURRENT,
                        new FixedVoltageDirichletBoundary(
                            WINDING_CURRENT_FIRST_BOUNDARY,
                            0
                        ),
                    OPERATION_MAGNETIC_FIELD,
                        new MaterialBoundary(WINDING_CURRENT_FIRST_BOUNDARY)
                    ),
                new MultipleOperationBoundary(
                    WINDING_CURRENT_SECOND_BOUNDARY,
                    OPERATION_WINDING_CURRENT,
                        new FixedCurrentBoundary(
                            WINDING_CURRENT_SECOND_BOUNDARY, 10
                        ),
                    OPERATION_MAGNETIC_FIELD,
                        new MaterialBoundary(WINDING_CURRENT_SECOND_BOUNDARY)
                    ),
                new MultipleOperationBoundary(
                    FAR_FIELD_MAGNETIC_BOUNDARY,
                    OPERATION_WINDING_CURRENT,
                    null,
                    OPERATION_MAGNETIC_FIELD,
                        new FixedMagneticVectorPotentialDirichletBoundary(
                            FAR_FIELD_MAGNETIC_BOUNDARY, new Vector3D(0,0,0)
                        )
                    ),
                new MaterialBoundary("MaterialBoundary"),
                new AdiabaticBoundaryInsulated("AdiabaticBoundary")
            );
            VolumesCollection volumes = new VolumesCollection(
                new MultipleOperationVolume(
                    WINDING_VOLUME,
                    OPERATION_WINDING_CURRENT,
                        new StaticCurrentVolume(WINDING_VOLUME,
                            Conductivities.Copper),
                    OPERATION_MAGNETIC_FIELD,
                        new StaticMagneticConductionVolume(
                            WINDING_VOLUME, PERMEABILITY_FREE_SPACE),
                    maximumTetrahedralVolumeConstraint: 1e-8//5e-9
                ),
                new MultipleOperationVolume(
                    FREE_SPACE_VOLUME,
                    OPERATION_WINDING_CURRENT,
                    null,
                    OPERATION_MAGNETIC_FIELD,
                        new StaticMagneticConductionVolume(
                            FREE_SPACE_VOLUME, PERMEABILITY_FREE_SPACE),
                    maximumTetrahedralVolumeConstraint: 1e-8//5e-9
                )
            );
            PolyhedralDomain domain = ObjFileToPoly.Read(
                //File.ReadAllBytes("C:\\repos\\snippets\\CircuitAnalysis\\VoltageMultiplier\\Meshes\\TestWindings.obj")
                File.ReadAllBytes("C:\\repos\\snippets\\CircuitAnalysis\\SimpleMagneticConductionAnalysis\\Meshes\\SimpleMagneticConduction.obj"), volumes, boundaries,
                out Dictionary<int, Boundary> mapMarkerToBoundary, Units.Millimeters,
                0.0001d);
            string tempDirectoryPath = "D:\\temp\\";
            using (TemporaryDirectory temporaryDirectory = TemporaryDirectory
                .InCustomTempDirectory(tempDirectoryPath))
            {
                using (TemporaryWorkingDirectoryManager workingDirectoryManager = new TemporaryWorkingDirectoryManager(
                        Path.Combine(temporaryDirectory.AbsolutePath, "workings")))
                {
                    Console.WriteLine($"Output to: \"{OUTPUT_DIRECTORY}\"");
                    DirectoryHelper.DeleteRecursively(OUTPUT_DIRECTORY, throwOnError: false);
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
                        StaticCurrentConductionResult firstHalfWindingStaticCurrentSolverResult
                         = staticCurrentSolver.Solve(
                            firstHalfWindingMesh,
                            workingDirectoryManager,
                            OPERATION_WINDING_CURRENT,
                            cachedSolverResult: null);

                        firstHalfWindingStaticCurrentSolverResult.Print();
                        PlyWriter.Write(
                            VOLTAGES_PLY_FILE_PATH,
                            firstHalfWindingMesh,
                            new FieldResult[] {
                                new ScalarFieldResult("voltage", firstHalfWindingStaticCurrentSolverResult.NodalVoltages),
                                new ScalarFieldResult("rhs", firstHalfWindingStaticCurrentSolverResult.CoreResult.RHSVector)
                            }
                        );


                        /*
                        TetrahedralMesh secondHalfWindingMesh = mesh.ToOperationSpecificMesh(
                            OPERATION_WINDING_CURRENT_2);
                        SolverResult secondHalfWindingStaticCurrentSolverResult
                         = staticCurrentSolver.Solve(
                            secondHalfWindingMesh,
                            workingDirectoryManager,
                            OPERATION_WINDING_CURRENT_2,
                            cachedSolverResult: null);

                        secondHalfWindingStaticCurrentSolverResult.Print();
                        PlyWriter.WritePlyFile(
                            VOLTAGES_2_PLY_FILE_PATH,
                            secondHalfWindingMesh.Nodes,
                            secondHalfWindingMesh.AllFaces,
                            new FieldResult[] {
                                new ScalarFieldResult("voltage", secondHalfWindingStaticCurrentSolverResult.UnknownsVector)
                            }
                        );
                        */




                        //CloudCompareHelper.Open(VOLTAGES_PLY_FILE_PATH);
                        PlyWriter.Write(
                            CURRENT_DENSITIES_PLY_FILE_PATH,
                            firstHalfWindingMesh,
                            firstHalfWindingStaticCurrentSolverResult.GetNodalVolumeCurrentDensities("volume_current_density")
                        );
                        //CloudCompareHelper.Open(CURRENT_DENSITIES_PLY_FILE_PATH);
                        var staticMagneticConductionSolver = new StaticMagneticConductionSolver();
                        var magneticFieldMesh = mesh.ToOperationSpecificMesh(OPERATION_MAGNETIC_FIELD);
                        var magneticFieldResult = staticMagneticConductionSolver.Solve(
                            magneticFieldMesh,
                            workingDirectoryManager,
                            OPERATION_MAGNETIC_FIELD,
                            new DelegateApplySourceRegion[]{
                                    firstHalfWindingStaticCurrentSolverResult.ApplyVolumeCurrentDensities
                            }
                       );
                        PlyWriter.Write(
                            MAGNETIC_FIELD_PLY_FILE_PATH,
                            magneticFieldMesh,
                            new FieldResult[] {
                                new VectorFieldResult(
                                    "magnetic_vector_potential", 
                                    magneticFieldResult.NodalMagneticVectorPotentials,
                                    includeMagnitude:true),
                                new VectorFieldResult("rhs", magneticFieldResult.CoreResult.RHSVector,
                                includeMagnitude:true),
                                new VectorFieldResult(
                                    "magnetic_flux",
                                    magneticFieldResult.GetNodalMagneticFluxDensityB(),
                                    includeMagnitude:true)
                            }
                        );
                        //CloudCompareHelper.Open(VOLTAGES_PLY_FILE_PATH);
                        double minValue = magneticFieldResult.NodalMagneticVectorPotentials.Min();
                        double maxValue = magneticFieldResult.NodalMagneticVectorPotentials.Max();
                        }
                }

            }
        }
    }
}