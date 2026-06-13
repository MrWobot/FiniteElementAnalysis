using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

namespace FiniteElementAnalysis.Fields
{
    public class DebugValuesFile
    {
        private string _FilePath;

        public DebugValuesFile(string name, bool replace)
        {
            _FilePath = Path.Combine($"D:\\debugging\\{name}.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(_FilePath));
            if (replace)
            {
                File.Delete(_FilePath);
            }
        }

        public void Write(double[] array)
        {
            File.Delete(_FilePath);
            using (StreamWriter writer = new StreamWriter(_FilePath, append: true))
            {
                foreach (double value in array)
                {
                    writer.WriteLine(value.ToString("G17", CultureInfo.InvariantCulture));
                }
            }
        }

        public void Write(double[][] array)
        {
            File.Delete(_FilePath);
            using (StreamWriter writer = new StreamWriter(_FilePath, append: true))
            {
                foreach (double[] row in array)
                {
                    writer.WriteLine(string.Join(",", Array.ConvertAll(row, val => val.ToString("G17", CultureInfo.InvariantCulture))));
                }
            }
        }

        public double[] ReadDoubleArray()
        {
            List<double> values = new List<double>();

            try
            {
                using (StreamReader reader = new StreamReader(_FilePath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Length < 1) break;
                        if (double.TryParse(line, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                        {
                            values.Add(value);
                        }
                        else
                        {
                            Console.WriteLine($"Warning: Unable to parse '{line}' as a double.");
                        }
                    }
                }
                Console.WriteLine("Array read from file successfully.");
            }
            catch (Exception e)
            {
                Console.WriteLine("Value reading from file: " + e.Message);
            }

            return values.ToArray();
        }

        public double[][] ReadDoubleArray2D()
        {
            List<double[]> values = new List<double[]>();

            try
            {
                using (StreamReader reader = new StreamReader(_FilePath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Length < 1) break;
                        string[] entries = line.Split(',');
                        double[] row = new double[entries.Length];

                        for (int i = 0; i < entries.Length; i++)
                        {
                            if (double.TryParse(entries[i], NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                            {
                                row[i] = value;
                            }
                            else
                            {
                                Console.WriteLine($"Warning: Unable to parse '{entries[i]}' as a double.");
                            }
                        }

                        values.Add(row);
                    }
                }
                Console.WriteLine("2D array read from file successfully.");
            }
            catch (Exception e)
            {
                Console.WriteLine("Value reading from file: " + e.Message);
            }

            return values.ToArray();
        }
    }
}
