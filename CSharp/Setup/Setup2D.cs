
using Core.Cleanup;
using Core.FileSystem;
using Core.Timing;
using FiniteElementAnalysis.Mesh.Planar;
using FiniteElementAnalysis.Mesh.Tetrahedral;
namespace FiniteElementAnalysis.Setup
{
    public class Setup2D:IDisposable
    {
        public string OutputDirectory { get; }
        public TemporaryDirectory TemporaryDirectory { get; }
        public TemporaryWorkingDirectoryManager WorkingDirectoryManager { get; }
        public PlanarDomain Domain { get; }
        public long StartTime{ get; }
        public long TimeTaken { 
            get
            {
                return TimeHelper.MillisecondsNow - StartTime;
            } 
        }
        public Setup2D(
            string outputDirectory,
            TemporaryDirectory temporaryDirectory,
            TemporaryWorkingDirectoryManager workingDirectoryManager,
            PlanarDomain domain)
        {
            OutputDirectory = outputDirectory;
            TemporaryDirectory = temporaryDirectory;
            WorkingDirectoryManager = workingDirectoryManager;
            Domain = domain;
            StartTime = TimeHelper.MillisecondsNow;
        }
        public void Dispose() { 
            TemporaryDirectory.Dispose();
            WorkingDirectoryManager.Dispose();
        }
    }
}