using Core.FileSystem;
using System;
using System.Diagnostics;
using System.IO;

namespace FiniteElementAnalysis.CloudCompare
{
    public static class CloudCompareHelper
    {
        public static void Open(string filePath, bool openTempCopy = false)
        {
            using (TemporaryFile tempFile = new TemporaryFile(Path.GetExtension(filePath)))
            {
                if (openTempCopy)
                {
                    File.Copy(filePath, tempFile.FilePath, true);
                }
                string cloudComparePath = FindCloudCompareExe();

                // Prepare the command to open the GUI and load the PLY file
                ProcessStartInfo processInfo = new ProcessStartInfo();
                processInfo.FileName = cloudComparePath;
                processInfo.Arguments = $"\"{filePath}\"";  // Directly pass the file path without any arguments

                // Set flags for the process
                processInfo.UseShellExecute = true;  // UseShellExecute is true to open the GUI application

                // Start the process
                Process process = new Process();
                process.StartInfo = processInfo;

                try
                {
                    process.Start();
                    Console.WriteLine("CloudCompare GUI has been opened with the specified file.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }
        }

        private static string FindCloudCompareExe()
        {
            string exeName = "CloudCompare.exe";

            // 1. Search in environment PATH
            string environmentPath = Environment.GetEnvironmentVariable("PATH")!;
            if (!string.IsNullOrEmpty(environmentPath))
            {
                string[] paths = environmentPath.Split(Path.PathSeparator);
                foreach (string path in paths)
                {
                    string potentialPath = Path.Combine(path, exeName);
                    if (File.Exists(potentialPath))
                    {
                        return potentialPath;
                    }
                }
            }

            // 2. Common installation paths
            string[] commonPaths = new string[]
            {
                @"C:\Program Files\CloudCompare",
                @"C:\Program Files (x86)\CloudCompare",
                @"C:\CloudCompare",
                @"D:\CloudCompare" // For cases where software might be installed in a secondary drive
            };

            foreach (string commonPath in commonPaths)
            {
                string potentialPath = Path.Combine(commonPath, exeName);
                if (File.Exists(potentialPath))
                {
                    return potentialPath;
                }
            }

            // 3. Custom user-defined paths (optional)
            string[] customPaths = new string[]
            {
                $"C:\\Users\\{Environment.UserName}\\Tools\\CloudCompare" // Replace or add any custom paths you frequently use
            };

            foreach (string customPath in customPaths)
            {
                string potentialPath = Path.Combine(customPath, exeName);
                if (File.Exists(potentialPath))
                {
                    return potentialPath;
                }
            }

            // 4. Not found
            throw new FileNotFoundException($"{exeName} could not be found in the environment PATH or common locations.");
        }
    }
}