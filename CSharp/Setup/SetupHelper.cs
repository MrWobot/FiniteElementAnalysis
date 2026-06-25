
using Core.Cleanup;
using Core.Enums;
using Core.FileSystem;
using Core.Timing;
using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Boundaries.Thermal;
using FiniteElementAnalysis.Mesh.Generation;
using FiniteElementAnalysis.Mesh.Generation.Planar;
using FiniteElementAnalysis.Mesh.Tetrahedral;
using FiniteElementAnalysis.MtlFiles;
using FiniteElementAnalysis.Planar;
using FiniteElementAnalysis.Ply;
using FiniteElementAnalysis.Polyhedrals;
using SkiaSharp;
using System;
using TriangleNet;
using TriangleNet.Geometry;
using TriangleNet.Meshing;
using TriangleNet.Meshing.Algorithm;

namespace FiniteElementAnalysis.Setup
{
    public class SetupHelper
    {
        public static Setup2D Setup2D(
            byte[] objFileBytes,
            byte[] mtlFileBytes,
            BoundariesCollection boundaries,
            VolumesCollection volumes,
            double maxDistanceNodeMergeMeters = 0.000001,
            Units units = Units.Meters,
            string? outputDirectory = null) {

            MtlFile mtlFile = MtlFileParser.Read(mtlFileBytes);
            PlanarDomain domain = PlanarDomainFromObjMtlHelper.Read(
                objFileBytes, volumes, boundaries,
                out Dictionary<int, Boundary> mapMarkerToBoundary,
                units, maxDistanceNodeMergeMeters);
            domain.ApplyColours(mtlFile);
            PlanarDomainDrawingHelper.Draw(domain, "D:\\temp\\domain.png");
            TemporaryDirectory temporaryDirectory = new TemporaryDirectory();
            TemporaryWorkingDirectoryManager workingDirectoryManager = new TemporaryWorkingDirectoryManager();
            if (outputDirectory == null)
            {
                string projectDirectory = DirectoryHelper.GetProjectDirectory();
                outputDirectory = Path.Combine(projectDirectory, "output");
            }
            Console.WriteLine($"Output to: \"{outputDirectory}\"");
            DirectoryHelper.DeleteRecursively(outputDirectory, throwOnError: false);


            //PolyFileGenerator.Generate(polyFilePath, domain);
            /*
            TetrahedralMesh mesh = generateMeshResult.ToMesh(boundaries, volumes, mapMarkerToBoundary);
            return new Setup2D(outputDirectory!, temporaryDirectory, workingDirectoryManager, mesh);
            
            var points = Generate.RandomPoints(50, new Rectangle(0, 0, 100, 100));

            // Choose triangulator: Incremental, SweepLine or Dwyer.
            var triangulator = new Dwyer();

            // Generate mesh.
            var mesh = triangulator.Triangulate(points, new Configuration());

            if (print) SvgImage.Save(mesh, "example-1.svg", 500);

            return mesh.Triangles.Count > 0;
            */
            return null;
        }
        public static Setup3D Setup3D(
            byte[] objFileBytes,
            BoundariesCollection boundaries,
            VolumesCollection volumes,
            double maxDistanceNodeMergeMeters= 0.000001,
            Units units = Units.Meters,
            string temporaryDirectoryPath = "D:\\temp",
            string? outputDirectory = null)
        {
            PolyhedralDomain domain = ObjFileToPoly.Read(
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
            Console.WriteLine($"Output to: \"{outputDirectory}\"");
            DirectoryHelper.DeleteRecursively(outputDirectory, throwOnError: false);
            string polyFilePath = Path.Combine(temporaryDirectory.AbsolutePath, "mesh.poly");
            PolyFileGenerator.Generate(polyFilePath, domain);
            using (var tetgen = new Tetgen())
            {
                TetgenGenerateMeshResult generateMeshResult = tetgen.GenerateTetrahedralMesh(
                    polyFilePath,
                    new TetgenParameters
                    {
                        RefineMesh = true,
                        MaximumTetrahedralVolumeConstraint = maxDistanceNodeMergeMeters,
                        CheckConsistencyOfFinalMesh = true
                    });
                if (generateMeshResult.ExitCode != 0)
                {
                    string moreExceptionInfo = generateMeshResult.GetMoreExceptionInfo();
                    Console.WriteLine(moreExceptionInfo);
                    throw new Exception(generateMeshResult.Output);
                }
                TetrahedralMesh mesh = generateMeshResult.ToMesh(boundaries, volumes, mapMarkerToBoundary);
                string meshPlyFilePath = Path.Combine(outputDirectory, "mesh.ply");
                PlyWriter.Write(meshPlyFilePath, mesh);
                return new Setup3D(outputDirectory!, temporaryDirectory, workingDirectoryManager, mesh);
            }
        }
    }
}