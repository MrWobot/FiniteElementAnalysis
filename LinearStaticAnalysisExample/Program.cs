using Core.FileSystem;
using FiniteElementAnalysis.Boundaries;
using Core.Enums;
using FiniteElementAnalysis.Fields;
using InfernoDispatcher;
using Core.MemoryManagement;
using Shutdown;
using Logging;
using FiniteElementAnalysis.Ply;
using FiniteElementAnalysis.Mesh.Tetrahedral;
using FiniteElementAnalysis.Boundaries.Statics;
using FiniteElementAnalysis.Setup;
using LinearStaticAnalysisExample;
using FiniteElementAnalysis.Results.ThreeD;
using FiniteElementAnalysis.Solvers;

namespace LienarStaticAnalysisExample
{
    // Example usage:
    public class Program
    {
        public static void Main(string[] args)
        {
            ShutdownManager.Initialize(Environment.Exit, () => Logs.Default);
            GpuMemoryInfoNVML.Initialize();
            Dispatcher.InitializeWithNative(Console.WriteLine);
            const string OPERATION_1 = "Operation1",
                MATERIAL_BOUNDARY = "MaterialBoundary",
                FIXED_FORCE_BOUNDARY = "FixedForceBoundary",
                MATERIAL_VOLUME = "MaterialVolume",
                FIXED_DISPLACEMENT_BOUNDARY = "FixedDisplacementBoundary";
            const double FORCE = 82000,
                FORCE_2 = 0,
                S275_YOUNGS_MODULUS = 210 * 1E+9,
                S275_POISONS_RATIO = 0.3;
            BoundariesCollection boundaries = new BoundariesCollection(
                new FreeBoundary(MATERIAL_BOUNDARY),
                new FixedNormalForceNeumannBoundary(FIXED_FORCE_BOUNDARY, FORCE),
                new FixedDisplacementDirichletBoundary(FIXED_DISPLACEMENT_BOUNDARY, new double[] { 0, 0, 0 }, new double[] { 0, 0, 0 })
            );
            VolumesCollection volumes = new VolumesCollection(
                new StaticLinearElasticVolume(MATERIAL_VOLUME, S275_YOUNGS_MODULUS, S275_POISONS_RATIO)
            );
            Setup3D setup3D = SetupHelper.Setup3D(
                MeshResources.Cantilever, 
                boundaries, volumes,
                maxDistanceNodeMergeMeters: 0.00001d,
                globalMaximumTetrahedralVolumeConstraintMeters: 1e-5,
                units: Units.Millimeters
            );
            LinearStaticAnalysisSolver solver = new LinearStaticAnalysisSolver();
            LinearStaticAnalysisResult3D result
                         = solver.Solve(
                            setup3D.Mesh,
                            setup3D.WorkingDirectoryManager,
                            OPERATION_1,
                            cachedSolverResult: null);
            result.Print();
            (double maxDisplacementX, double maxDisplacementY, double maxDisplacementZ)
                = result.CalculateMaxDisplacements();
            Console.WriteLine("Max displacement X was: " + maxDisplacementX);
            Console.WriteLine("Max displacement Y was: " + maxDisplacementY);
            Console.WriteLine("Max displacement Z was: " + maxDisplacementZ);

            PlyWriter.Write(
                Path.Combine(setup3D.OutputDirectory, "displacements.ply"),
                setup3D.Mesh,
                new FieldResult[] {
                    new VectorFieldResult("displacement", result.Displacements, includeMagnitude:true),
                }
            );
            TetrahedralMesh displacedMesh = result.DisplaceMesh();
            PlyWriter.Write(Path.Combine(setup3D.OutputDirectory, "displacedMesh.ply"), setup3D.Mesh);
            result.ComputeNodalNormalAndShearStressStrainAsSeperateVectors(
                computeStress: true,
                computeStrain: true,
                out double[]? nodalNormalStress,
                out double[]? nodalShearStress,
                out double[]? nodalNormalStrain,
                out double[]? nodalShearStrain
                );
            PlyWriter.Write(
                Path.Combine(setup3D.OutputDirectory, "stress.ply"),
                setup3D.Mesh,
                new FieldResult[] {
                                new VectorFieldResult("normal_stress", nodalNormalStress!, includeMagnitude:true),
                                new VectorFieldResult("shear_stress", nodalShearStress!, includeMagnitude:true),
                }
            );
            PlyWriter.Write(
                Path.Combine(setup3D.OutputDirectory, "strain.ply"),
                setup3D.Mesh,
                new FieldResult[] {
                                new VectorFieldResult("normal_strain", nodalNormalStrain!, includeMagnitude:true),
                                new VectorFieldResult("shear_strain", nodalShearStrain!, includeMagnitude:true),
                }
            );
        }
    }
}