using FiniteElementAnalysis.Boundaries;
using FiniteElementAnalysis.Fields;
using FiniteElementAnalysis.Mesh;
using FiniteElementAnalysis.Mesh.Tetrahedral;
using FiniteElementAnalysis.MeshGeneration;
using System.Text.RegularExpressions;

namespace FiniteElementAnalysis.Ply
{
    public static class PlyWriter
    {
        private static readonly string[] FIELD_COMPONENT_COLOURS = new string[] { "red", "green", "blue" };
        private const string LINE_BREAK = "\n"; // Use string for line break in ASCII
        private abstract class FieldComponent
        {
            public abstract double GetColour(int index);
        }
        private class ResultFieldComponent : FieldComponent
        {
            public byte[] Colours { get; }
            public double[] Values { get; }
            public string ColourName { get; }
            public string ComponentName { get; }
            public ResultFieldComponent(byte[] colours, double[] values, string colourName, string componentName) : base()
            {
                Colours = colours;
                Values = values;
                ColourName = colourName;
                ComponentName = componentName;
            }
            public override double GetColour(int index)
            {
                return Colours[index];
            }
            public double GetValue(int index)
            {
                return Values[index];
            }
        }
        private class FixedFieldComponent : FieldComponent
        {
            private double _FixedValue;
            public FixedFieldComponent(double fixedValue)
            {
                _FixedValue = fixedValue;
            }
            public override double GetColour(int index)
            {
                return _FixedValue;
            }
        }
        public static void WritePlyFile(string filePath, Node[] nodes,
            BoundaryFace[] faces, params FieldResult[] fieldResults)
        {
            FieldComponent[] colourFieldComponents = GetFieldComponents(fieldResults,
                out ResultFieldComponent[] resultFieldComponents);
            WritePlyFile(filePath, nodes, faces, colourFieldComponents, resultFieldComponents);
        }
        public static void WriteRegions(string filePath)
        {

        }
        public static void Write(string filePath,
            TetrahedralMesh mesh, params FieldResult[] fieldResults)
        {

            Node[] nodes = mesh.Nodes;
            int nodesLength = nodes.Length;
            BoundaryFace[] faces = mesh.AllFaces;
            HashSet<Volume> volumesIncluded = new HashSet<Volume>();
            foreach (var element in mesh.Elements)
            {
                if (element.VolumeIsAPartOf == null) throw new NullReferenceException();
                volumesIncluded.Add(element.VolumeIsAPartOf);
            }

            byte[][] colours = ColourGenerator.GetNWellSpacedColours(volumesIncluded.Count());
            int colourIndex = 0;
            var mapVolumeToColor = volumesIncluded.ToDictionary(v => v, v => colours[colourIndex++]);
            Func<Volume, byte[]> getVolumeColour = (volume) => mapVolumeToColor[volume];
            var mapNodeIdentifierToColour = mesh.Elements.SelectMany(e => e.Nodes.Select(n => new { node = n, element = e }))
                .GroupBy(o => o.node.Identifier)
                .Select(g => new
                {
                    nodeIdentifier = g.First().node.Identifier,
                    volume = g
                    .Select(o => o.element.VolumeIsAPartOf).GroupBy(v => v)
                    .OrderByDescending(vg => vg.Count())
                    .First().First()
                })
                .ToDictionary(o => o.nodeIdentifier, o => getVolumeColour(o.volume!));
            byte[][] nodeColors = nodes.Select(n => mapNodeIdentifierToColour[n.Identifier]).ToArray();
            string? directoryPath = Path.GetDirectoryName(filePath);
            if (directoryPath == null)
                throw new ArgumentException($"Invalid file path \"{filePath}\"");
            Directory.CreateDirectory(directoryPath);
            int nodeIndex = 0;
            Dictionary<Node, int> mapNodeToIndex = nodes.ToDictionary(n => n, n => nodeIndex++);
            using (var writer = new StreamWriter(File.Open(filePath, FileMode.Create)))
            {
                Action<string> writeString = (str) => writer.Write(str);
                Action writeLineBreak = () => writer.Write(LINE_BREAK);
                Action writeSpace = () => writer.Write(" ");
                FieldResultProperty[] fieldResultProperties = fieldResults.SelectMany(f => f.FieldResultProperties).ToArray();
                // Write the PLY header
                writeString("ply");
                writeLineBreak();
                writeString("format ascii 1.0"); // Change format to ASCII
                writeLineBreak();

                // Write vertex count
                writeString($"element vertex {nodes.Length}");
                writeLineBreak();

                // Write properties for vertices
                writeString("property float x");
                writeLineBreak();
                writeString("property float y");
                writeLineBreak();
                writeString("property float z");
                writeLineBreak();
                writeString("property uchar red");
                writeLineBreak();
                writeString("property uchar green");
                writeLineBreak();
                writeString("property uchar blue");
                writeLineBreak();
                foreach (var fieldResultProperty in fieldResultProperties)
                {
                    if (fieldResultProperty.Values.Length != nodesLength)
                    {
                        throw new Exception($"{nameof(FieldResultProperty)} named \"{fieldResultProperty.Name}\" had {nameof(fieldResultProperty.Values)} length {fieldResultProperty.Values.Length} but there were {nodesLength} nodes.");
                    }
                    writeString($"property double {fieldResultProperty.Name}");
                    writeLineBreak();
                }
                // Write face count
                writeString($"element face {faces.Length}");
                writeLineBreak();

                // Write properties for faces
                writeString("property list uchar int vertex_indices");
                writeLineBreak();

                // End of header
                writeString("end_header");

                // Write vertex data
                nodeIndex = 0;
                while (nodeIndex < nodes.Length)
                {
                    Node node = nodes[nodeIndex];
                    writeLineBreak();
                    byte[] nodeColour = nodeColors[nodeIndex];
                    writer.Write($"{node.X} {node.Y} {node.Z} {nodeColour[0]} {nodeColour[1]} {nodeColour[2]}");
                    foreach (var fieldResultProperty in fieldResultProperties)
                    {
                        writer.Write($" {fieldResultProperty.Values[nodeIndex]}");
                    }
                    nodeIndex++;
                }

                // Write face data
                foreach (var face in faces)
                {
                    writeLineBreak();
                    writer.Write($"3 {mapNodeToIndex[face.Nodes[0]]} {mapNodeToIndex[face.Nodes[1]]} {mapNodeToIndex[face.Nodes[2]]}");
                }
                writeLineBreak();
            }

            Console.WriteLine($"PLY file \"{filePath}\" generated successfully.");
        }
        private static void WritePlyFile(string filePath, Node[] nodes,
            BoundaryFace[] faces, FieldComponent[] colourFieldComponents, ResultFieldComponent[] resultParameterFieldComponents)
        {

            string? directoryPath = Path.GetDirectoryName(filePath);
            if (directoryPath == null)
                throw new ArgumentException($"Invalid file path \"{filePath}\"");
            Directory.CreateDirectory(directoryPath);
            int nodeIndex = 0;
            Dictionary<Node, int> mapNodeToIndex = nodes.ToDictionary(n => n, n => nodeIndex++);
            using (var writer = new StreamWriter(File.Open(filePath, FileMode.Create)))
            {
                Action<string> writeString = (str) => writer.Write(str);
                Action writeLineBreak = () => writer.Write(LINE_BREAK);
                Action writeSpace = () => writer.Write(" ");

                // Write the PLY header
                writeString("ply");
                writeLineBreak();
                writeString("format ascii 1.0"); // Change format to ASCII
                writeLineBreak();

                // Write vertex count
                writeString($"element vertex {nodes.Length}");
                writeLineBreak();

                // Write properties for vertices
                writeString("property float x");
                writeLineBreak();
                writeString("property float y");
                writeLineBreak();
                writeString("property float z");
                writeLineBreak();
                writeString("property uchar red");
                writeLineBreak();
                writeString("property uchar green");
                writeLineBreak();
                writeString("property uchar blue");
                writeLineBreak();
                foreach (var resultFieldComponent in resultParameterFieldComponents)
                {
                    writeString($"property double {resultFieldComponent.ComponentName}");
                    writeLineBreak();
                }
                // Write face count
                writeString($"element face {faces.Length}");
                writeLineBreak();

                // Write properties for faces
                writeString("property list uchar int vertex_indices");
                writeLineBreak();

                // End of header
                writeString("end_header");

                // Write vertex data
                nodeIndex = 0;
                for (nodeIndex = 0; nodeIndex < nodes.Length; nodeIndex++)
                {
                    Node node = nodes[nodeIndex];
                    writeLineBreak();
                    writer.Write($"{node.X} {node.Y} {node.Z}"); // Write as string
                    foreach (var fieldComponent in colourFieldComponents)
                    {
                        writeSpace();
                        writer.Write(fieldComponent.GetColour(nodeIndex));
                    }
                    foreach (var resultFieldComponent in resultParameterFieldComponents)
                    {
                        writeSpace();
                        writer.Write(resultFieldComponent.GetValue(nodeIndex));
                    }
                }

                // Write face data
                foreach (var face in faces)
                {
                    writeLineBreak();
                    writer.Write($"3 {mapNodeToIndex[face.Nodes[0]]} {mapNodeToIndex[face.Nodes[1]]} {mapNodeToIndex[face.Nodes[2]]}");
                }
                writeLineBreak();
            }

            Console.WriteLine($"PLY file \"{filePath}\" generated successfully.");
        }
        private static FieldComponent[] GetFieldComponents(FieldResult[] fieldResults,
            out ResultFieldComponent[] resultFieldComponents)
        {

            int nChannelsResuired = fieldResults.Select(f => f.NComponents).Sum();
            if (nChannelsResuired > 3)
                throw new ArgumentException($"Too many colour channels would be required for this combination of {nameof(fieldResults)}. You should create several individual ply files!");
            List<FieldComponent> colourApplications = new List<FieldComponent>(nChannelsResuired);
            colourApplications = new List<FieldComponent>();
            int nFieldComponent = 0;
            resultFieldComponents = new ResultFieldComponent[nChannelsResuired];
            List<ResultFieldComponent> resultFieldComponentsList = new List<ResultFieldComponent>();
            foreach (FieldResult fieldResult in fieldResults)
            {
                for (int valuesOffset = 0; valuesOffset < fieldResult.NComponents; valuesOffset++)
                {
                    string colourName = FIELD_COMPONENT_COLOURS[nFieldComponent];
                    string normalizedFieldResultName = NormalizePropertyName(fieldResult.Name);
                    string componentName = typeof(ScalarFieldResult).IsAssignableFrom(fieldResult.GetType())
                        ? normalizedFieldResultName : $"{normalizedFieldResultName}_{valuesOffset}";
                    var resultFieldComponent = new ResultFieldComponent(
                        GetColoursAndValuesForSingleFieldComponent(out double[] values,
                            fieldResult.NComponents, valuesOffset, fieldResult.Values),
                        values,
                        colourName, componentName);
                    colourApplications.Add(resultFieldComponent);
                    resultFieldComponents[nFieldComponent] = resultFieldComponent;
                    nFieldComponent++;
                }
            }
            while (colourApplications.Count < 3)
            {
                colourApplications.Add(new FixedFieldComponent(0));
            }
            return colourApplications.ToArray();
        }
        private static byte[] GetColoursAndValuesForSingleFieldComponent(
            out double[] values,
            int nFieldComponents, int offsetComponent, double[] valuesForFieldComponents)
        {
            values = new double[valuesForFieldComponents.Length / nFieldComponents];
            int j = offsetComponent;
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = valuesForFieldComponents[j];
                j += nFieldComponents;
            }
            double min = values.Min();
            double max = values.Max();
            double scale = 255d / (max - min);
            double offset = -min;
            byte[] bytes = new byte[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                double newValue = (values[i] + offset) * scale;
                if (newValue < 0)
                {
                    newValue = 0;
                }
                else if (newValue > 255)
                {
                    newValue = 255;
                }
                bytes[i] = (byte)newValue;
            }
            return bytes;
        }
        public static string NormalizePropertyName(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                throw new ArgumentException("Property name cannot be null or empty.", nameof(propertyName));

            // Define a regular expression to match invalid characters
            // Here we allow only alphanumeric characters and underscores
            string normalized = Regex.Replace(propertyName, @"[^a-zA-Z0-9_]", "_");

            // Ensure the name does not start with a digit
            if (char.IsDigit(normalized[0]))
            {
                normalized = "_" + normalized; // Prepend an underscore if it starts with a digit
            }

            return normalized;
        }
    }
}