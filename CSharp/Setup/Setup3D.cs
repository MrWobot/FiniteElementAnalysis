
using Core.Cleanup;
using Core.FileSystem;
using Core.Timing;
using FiniteElementAnalysis.Mesh.Tetrahedral;
namespace FiniteElementAnalysis.Setup
{
    public class Setup3D:IDisposable
    {
        public string OutputDirectory { get; }
        public TemporaryDirectory TemporaryDirectory { get; }
        public TemporaryWorkingDirectoryManager WorkingDirectoryManager { get; }
        public TetrahedralMesh Mesh { get; }
        public long StartTime{ get; }
        public long TimeTaken { 
            get
            {
                return TimeHelper.MillisecondsNow - StartTime;
            } 
        }
        public Setup3D(
            string outputDirectory,
            TemporaryDirectory temporaryDirectory,
            TemporaryWorkingDirectoryManager workingDirectoryManager,
            TetrahedralMesh mesh)
        {
            OutputDirectory = outputDirectory;
            TemporaryDirectory = temporaryDirectory;
            WorkingDirectoryManager = workingDirectoryManager;
            Mesh = mesh;
            StartTime = TimeHelper.MillisecondsNow;
        }
        public void Dispose() { 
            TemporaryDirectory.Dispose();
            WorkingDirectoryManager.Dispose();
        }
    }
}