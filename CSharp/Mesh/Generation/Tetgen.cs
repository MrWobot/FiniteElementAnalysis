using Core.FileSystem;
using FiniteElementAnalysis.MeshGeneration;
using Logging;
using System.Diagnostics;
using System.Text;

namespace FiniteElementAnalysis.Mesh.Generation
{
    public class Tetgen : IDisposable
    {
        private TemporaryFile _TetgenExe;
        public Tetgen()
        {
            _TetgenExe = new TemporaryFile(".exe");
            File.WriteAllBytes(_TetgenExe.FilePath, TetgenResources.tetgen);
        }
        public void Dispose()
        {
            try
            {
                File.Delete(_TetgenExe.FilePath);
            }
            catch (Exception ex)
            {
                Logs.Default.Error(ex);
            }
        }
        public TetgenGenerateMeshResult GenerateTetrahedralMesh(string polyFilePath, TetgenParameters parameters)
        {
            StringBuilder command = new StringBuilder();
            if (parameters.RefineMesh)
            {
                command.Append("-q ");
            }
            if (parameters.CheckConsistencyOfFinalMesh)
            {
                command.Append("-C ");
            }
            command.Append("-A ");
            if (parameters.MaximumTetrahedralVolumeConstraint != null)
            {
                command.Append($"-a{(decimal)parameters.MaximumTetrahedralVolumeConstraint} ");
            }
            command.Append("-a ");
            command.Append("-p ");
            command.Append($"\"{polyFilePath}\"");
            int exitCode = Run(command.ToString(), out string output, out string errorMessage);
            return new TetgenGenerateMeshResult(exitCode, output, errorMessage, Path.GetDirectoryName(polyFilePath)!);
        }
        private int Run(string command, out string output, out string error)
        {
            try
            {

                var processStartInfo = new ProcessStartInfo()
                {
                    FileName = _TetgenExe.FilePath,
                    Arguments = command,
                    UseShellExecute = false,
                    RedirectStandardError=true,
                    RedirectStandardOutput=true
                };
                Process process = new Process();
                process.StartInfo = processStartInfo;
                //process.StartInfo.RedirectStandardOutput = true;
                process.Start();
                process.WaitForExit();
                output = process.StandardOutput.ReadToEnd();
                error = process.StandardError.ReadToEnd();
                return process.ExitCode;
            }
            finally
            {

            }
        }
        public static string CopyTetViewToDirectory(string directoryPath)
        {
            string filePath = Path.Combine(directoryPath, "tetview-win.exe");
            File.WriteAllBytes(filePath, TetgenResources.tetview_win);
            return filePath;
        }
    }
}