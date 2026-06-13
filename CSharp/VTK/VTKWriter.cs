using System.Globalization;
using System.Text.RegularExpressions;
using FiniteElementAnalysis.Fields;
using FiniteElementAnalysis.Mesh.Tetrahedral;
using FiniteElementAnalysis.Mesh;
namespace FiniteElementAnalysis.VTK
{

    public static class VtkWriter
    {

        public static void WriteVtkFile(string filePath, string name, Node[] nodes,
            TetrahedronElement[] elements, params FieldResult[] vectorFIeldResults)
        {
            int nodeIndex = 0;
            Dictionary<Node, int> mapNodeToIndex = nodes.ToDictionary(n => n, n => nodeIndex++);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                // Header
                writer.WriteLine("# vtk DataFile Version 2.0");
                writer.WriteLine(name);
                writer.WriteLine("ASCII");
                writer.WriteLine("DATASET UNSTRUCTURED_GRID");

                // Points (Nodes)
                int nNodes = nodes.Length;
                writer.WriteLine($"POINTS {nNodes} double");
                foreach (Node node in nodes)
                {
                    writer.WriteLine($"{node.X.ToString(CultureInfo.InvariantCulture)} " +
                                     $"{node.Y.ToString(CultureInfo.InvariantCulture)} " +
                                     $"{node.Z.ToString(CultureInfo.InvariantCulture)}");
                }
                writer.WriteLine();

                // Cells (Elements)
                int nElements = elements.Length;
                int listSize = nElements*5;
                writer.WriteLine($"CELLS {nElements} {listSize}");
                foreach (TetrahedronElement element in elements)
                {
                    writer.Write("4 ");
                    writer.Write($"{mapNodeToIndex[element.NodeA]} ");
                    writer.Write($"{mapNodeToIndex[element.NodeB]} ");
                    writer.Write($"{mapNodeToIndex[element.NodeC]} ");
                    writer.WriteLine($"{mapNodeToIndex[element.NodeD]} ");
                }
                writer.WriteLine();

                // Cell types (5 = VTK_TETRA)
                writer.WriteLine($"CELL_TYPES {nElements}");
                for (int i = 0; i < nElements; i++)
                {
                    writer.WriteLine("10");  // Tetrahedral element
                }
                writer.WriteLine();

                // Point Data (Nodal values)
                writer.WriteLine($"POINT_DATA {nNodes}");
                int vectorFieldResultIndex = 0;
                foreach (FieldResult vectorFieldResult in vectorFIeldResults)
                {
                    if (vectorFieldResult.NNodes != nNodes) {
                        throw new Exception($"{nameof(FieldResult)} in {nameof(vectorFIeldResults)} at index {vectorFieldResultIndex} was the wrong length for {nNodes} nodes");
                    }
                    string dataType = vectorFieldResult.NComponents switch
                    {
                        3 => "VECTORS",
                        1 => "SCALARS",
                        6 => "TENSORS",  // Assuming 6 values for stress/strain tensors
                        2 => "NORMALS",  // Assuming 2D normal vectors
                        _ => throw new NotImplementedException($"Not implemented for {vectorFieldResult.NComponents} values per node")
                    };
                    writer.WriteLine($"{dataType} {NormalizeVectorFieldName(vectorFieldResult.Name)} double");
                    int j = 0;
                    for (int i = 0; i < nNodes; i++)
                    {
                        for (int k = 0; k < vectorFieldResult.NComponents; k++)
                        {
                            if (k > 0)
                            {
                                writer.Write(' ');
                            }
                            writer.Write(vectorFieldResult.Values[j++].ToString(CultureInfo.InvariantCulture));
                        }
                        writer.WriteLine();
                    }
                    vectorFieldResultIndex++;
                }
            }

            Console.WriteLine($"VTK file \"{filePath}\" generated successfully.");
        }
        private static string NormalizeVectorFieldName(string nameOfVectorField)
        {
            if (string.IsNullOrWhiteSpace(nameOfVectorField))
            {
                throw new ArgumentException("The vector field name cannot be null or empty.");
            }
            string normalized = nameOfVectorField.Trim().Replace(' ', '_');
            normalized = Regex.Replace(normalized, @"[^a-zA-Z0-9_]", "_");

            // Ensure the name does not start with a number (VTK rules)
            if (char.IsDigit(normalized[0]))
            {
                normalized = "_" + normalized;
            }

            return normalized;
        }
    }
}