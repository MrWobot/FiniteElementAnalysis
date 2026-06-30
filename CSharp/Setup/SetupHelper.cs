
using Core.Enums;
using Core.FileSystem;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Mesh.Parsing.MtlFiles;
using FiniteElementAnalysis.Mesh.Parsing.Planar;
using FiniteElementAnalysis.Mesh.Parsing.Tetrahedral;
using FiniteElementAnalysis.Mesh.Planar;
using FiniteElementAnalysis.Mesh.Planar.Thickness;
using FiniteElementAnalysis.Mesh.Polyhedral;
using FiniteElementAnalysis.Mesh.Refinement.Tetrahedral.Tetgen;
using FiniteElementAnalysis.Mesh.Refinement.Triangular;
using FiniteElementAnalysis.Mesh.Tetrahedral;
using FiniteElementAnalysis.Ply;

namespace FiniteElementAnalysis.Setup
{
    public class SetupHelper
    {
        public static Setup2D Setup2D(
            byte[] objFileBytes,
            byte[] mtlFileBytes,
            BoundariesCollection boundaries,
            VolumesCollection volumes,
            PlanarThicknessSourceBase thicknessSource,
            double toleranceMeters = 0.001,
            Units units = Units.Meters,
            string temporaryDirectoryPath = "D:\\temp",
            string? outputDirectory = null) {

            MtlFile mtlFile = MtlFileParser.Read(mtlFileBytes);
            PlanarDomain domain = PlanarDomainFromObjMtlHelper.Read(
                objFileBytes, volumes, boundaries, thicknessSource,
                out Dictionary<int, Boundary> mapMarkerToBoundary,
                units, toleranceMeters);
            domain.ApplyColours(mtlFile);
            if (outputDirectory == null)
            {
                string projectDirectory = DirectoryHelper.GetProjectDirectory();
                outputDirectory = Path.Combine(projectDirectory, "output");
            }
            DirectoryHelper.DeleteRecursively(outputDirectory, throwOnError: false);
            Directory.CreateDirectory(outputDirectory);
            PlanarDomainDrawingHelper.Draw(domain, 
                Path.Combine(outputDirectory, "domain.png"));
            PlanarDomainDrawingHelper.Draw(domain, 
                Path.Combine(outputDirectory, "domainShowingAllTriangles.png"),
                showAllTriangles:true);
            TemporaryDirectory temporaryDirectory = TemporaryDirectory.InCustomTempDirectory(temporaryDirectoryPath);
            string workingDirectoryPath = Path.Combine(temporaryDirectory.AbsolutePath, "workings");
            TemporaryWorkingDirectoryManager workingDirectoryManager = new TemporaryWorkingDirectoryManager(workingDirectoryPath);
            Console.WriteLine($"Output directory: \"{outputDirectory}\"");
            Console.WriteLine($"Temporary directory: \"{temporaryDirectory.AbsolutePath}\"");
            Console.WriteLine($"Temporary working directory: \"{workingDirectoryManager.DirectoryPath}\"");

            domain = MeshRefinementHelper2D.Refine(domain, globalMaximumArea:1e-6);
            PlanarDomainDrawingHelper.Draw(domain, Path.Combine(outputDirectory, "domainRefined.png"));
            PlanarDomainDrawingHelper.Draw(domain, Path.Combine(outputDirectory, "domainRefinedShowingAllTriangles.png"),
                showAllTriangles:true);
            return new Setup2D(outputDirectory, temporaryDirectory, workingDirectoryManager, domain);
        }
        public static Setup3D Setup3D(
            byte[] objFileBytes,
            BoundariesCollection boundaries,
            VolumesCollection volumes,
            double maxDistanceNodeMergeMeters,
            double globalMaximumTetrahedralVolumeConstraintMeters,
            Units units = Units.Meters,
            string temporaryDirectoryPath = "D:\\temp",
            string? outputDirectory = null)
        {
            PolyhedralDomain domain = PolyhedralDomainFromObjHelper.Read(
                objFileBytes, volumes, boundaries,
                out Dictionary<int, Boundary> mapMarkerToBoundary, units, maxDistanceNodeMergeMeters);
            TemporaryDirectory temporaryDirectory = TemporaryDirectory.InCustomTempDirectory(temporaryDirectoryPath);
            string workingDirectoryPath = Path.Combine(temporaryDirectory.AbsolutePath, "workings");
            TemporaryWorkingDirectoryManager workingDirectoryManager = new TemporaryWorkingDirectoryManager(workingDirectoryPath);
            if (outputDirectory == null)
            {
                string projectDirectory = DirectoryHelper.GetProjectDirectory();
                outputDirectory = Path.Combine(projectDirectory, "output");
            }
            DirectoryHelper.DeleteRecursively(outputDirectory, throwOnError: false);
            Console.WriteLine($"Output directory: \"{outputDirectory}\"");
            Console.WriteLine($"Temporary directory: \"{temporaryDirectory.AbsolutePath}\"");
            Console.WriteLine($"Temporary working directory: \"{workingDirectoryManager.DirectoryPath}\"");
            TetgenGenerateMeshResult generateMeshResult = Tetgen.GenerateTetrahedralMesh(
                domain,
                new TetgenParameters
                {
                    RefineMesh = true,
                    MaximumTetrahedralVolumeConstraint = globalMaximumTetrahedralVolumeConstraintMeters,
                    CheckConsistencyOfFinalMesh = true
                },
                temporaryDirectory);
            generateMeshResult.ThrowIfErrors();
            TetrahedralMesh mesh = generateMeshResult.ToMesh(boundaries, volumes, mapMarkerToBoundary);
            string meshPlyFilePath = Path.Combine(outputDirectory, "mesh.ply");
            PlyWriter.Write(meshPlyFilePath, mesh);
            return new Setup3D(outputDirectory!, temporaryDirectory, workingDirectoryManager, mesh);
        }
    }
}