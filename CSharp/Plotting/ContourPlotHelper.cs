
using Core.Geometry;
using Core.Maths.Tensors;
using Core.Trees;
using FiniteElementAnalysis.Mesh.Tetrahedral;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
namespace FiniteElementAnalysis.Plotting
{
    public static class ContourPlotHelper
    {
        public static void Plot(
            TetrahedralMesh mesh, 
            double desiredImagePixelsShortestDimension,
            string directoryPath,
            string fileNamePrefix,
            params PlotPlaneType[] planesToInclude)
        {
            Directory.CreateDirectory(directoryPath);
            if (planesToInclude.Length < 1)
                planesToInclude = new PlotPlaneType[] { PlotPlaneType.X, PlotPlaneType.Y, PlotPlaneType.Z};
            double xMin = mesh.Nodes.Min(n => n.X);
            double yMin = mesh.Nodes.Min(n => n.Y);
            double zMin = mesh.Nodes.Min(n => n.Z);
            double xMax = mesh.Nodes.Max(n => n.X);
            double yMax = mesh.Nodes.Max(n => n.Y);
            double zMax = mesh.Nodes.Max(n => n.Z);
            double dX = xMax - xMin;
            double dY = yMax - yMin;
            double dZ = zMax - zMin;
            Vector3D middlePoint = new Vector3D((xMin + xMax) / 2d, (yMin + yMax) / 2d, (zMin + zMax) / 2d);
            Tuple<FinitePlane, PlotPlaneType>[] planeAndPlotPlane_s =
                planesToInclude.Select(p => new Tuple<FinitePlane, PlotPlaneType>(p switch
                {
                    PlotPlaneType.X =>new FinitePlane(middlePoint, new Vector3D(1, 0, 0), new Vector2D(dY, dZ)),
                    PlotPlaneType.Y => new FinitePlane(middlePoint, new Vector3D(0, -1, 0), new Vector2D(dX, dZ)),
                    PlotPlaneType.Z => new FinitePlane(middlePoint, new Vector3D(0, 0, 1), new Vector2D(dX, dY)),
                    _ => throw new NotImplementedException($"Not implemented for {nameof(PlotPlaneType)} {Enum.GetName(typeof(PlotPlaneType), p)}")
                }, p)).ToArray();
            foreach (var planeAndPlotPlane in planeAndPlotPlane_s)
            {
                FinitePlane plane = planeAndPlotPlane.Item1;
                PlotPlaneType type = planeAndPlotPlane.Item2;
                string filePath = Path.Combine(directoryPath, $"{fileNamePrefix}_{GetFileNameSuffix(type)}.png");
                Plot(mesh, plane, filePath, desiredImagePixelsShortestDimension);
            }
        }
        private static string GetFileNameSuffix(PlotPlaneType p) {
            return Enum.GetName(typeof(PlotPlaneType), p)!;
        }
        public static void Plot(TetrahedralMesh mesh, FinitePlane plane, string filePath, double desiredImagePixelsShortestDimension) {
            double resolution = desiredImagePixelsShortestDimension / (plane.Dimensions.X < plane.Dimensions.Y ? plane.Dimensions.X : plane.Dimensions.Y);
            Plot(mesh, plane, resolution, filePath);
        }
        public static void Plot(TetrahedralMesh mesh, FinitePlane plane, double resolution, string filePath)
        {
            int nXPixels = (int)Math.Ceiling(resolution * plane.Dimensions.X);
            int nYPixels = (int)Math.Ceiling(resolution * plane.Dimensions.Y);
            double?[][] values = new double?[nYPixels][];
            double[][] alphas = new double[nYPixels][];
            double xFrom = -(plane.Dimensions.X / 2);
            double yFrom = -(plane.Dimensions.Y / 2);
            double dY = plane.Dimensions.Y / ((double)nYPixels - 1);
            double dX = plane.Dimensions.X / ((double)nXPixels - 1);
            int halfNSubSteps = 2;
            double dXSubStep = dX / (2 * halfNSubSteps);
            double dYSubStep = dY / (2 * halfNSubSteps);
            for (int yIndex = 0; yIndex < nYPixels; yIndex++)
            {
                double?[] row = new double?[nXPixels];
                values[yIndex] = row;
                double[] alphasRow = new double[nXPixels];
                alphas[yIndex] = alphasRow;
                double y = ((double)yIndex * dY) + yFrom;
                for (int xIndex = 0; xIndex < nXPixels; xIndex++)
                {
                    double x = ((double)xIndex * dX) + xFrom;
                    List<double?> valuesAtSubPoints = new List<double?>();
                    for (int i = -halfNSubSteps; i <= halfNSubSteps; i++)
                    {
                        for (int j = -halfNSubSteps; j <= halfNSubSteps; j++)
                        {
                            Vector3D point = plane.Get3DPointFromXY(x + (i * dXSubStep), y + (j * dYSubStep),
                                new Vector3D(1, 0, 0));
                            TetrahedronElement[] elementsContainingPoint = mesh.ElementsBVHTree.QueryBVH(point)
                                .Where(e => e.IsPointInside(point)).ToArray();
                            if (elementsContainingPoint.Length > 0)
                            {
                                double value = elementsContainingPoint
                                    .Select(e => e.InterpolateScalarValueAtPoint(point)).Sum() / elementsContainingPoint.Length;
                                valuesAtSubPoints.Add(value);
                            }
                            else {
                                valuesAtSubPoints.Add(null);
                            }
                        }
                    }
                    double[] notNullValues = valuesAtSubPoints.Where(v => v != null).Cast<double>().ToArray();
                    double proportionNotNull = (double)notNullValues.Count() / (double)valuesAtSubPoints.Count();
                    alphasRow[xIndex]= proportionNotNull;
                    row[xIndex] = proportionNotNull<=0?null:(notNullValues.Sum() / notNullValues.Count());
                }
            }
            SavePlotAsImage(filePath, values, alphas);
        }

        public static void SavePlotAsImage(string filePath, double?[][] values, double[][] alphas)
        {
            double[] nonNullValues = values.SelectMany(row => row.Where(x => x != null)).Select(d => (double)d!).ToArray();
            if (nonNullValues.Length < 0) return;
            double minValue = nonNullValues.Min();
            double maxValue = nonNullValues.Max();
            int height = values.Length;
            int width = values[0].Length;
            using (var image = new Image<Rgba32>(width, height))
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        double? value = values[y][x];
                        double alpha = alphas[y][x];
                        Rgba32 color;
                        if (value == null)
                        {
                            color = new Rgba32(255, 255, 255,0); // Or another color indicating "no data"
                        }
                        else
                        {
                            color = ColorMap.GetColorForValue((double)value, minValue, maxValue, alpha);
                        }
                        image[x, y] = color;
                    }
                }
                image.Save(filePath); // Save the image to a file
            }
        }

    }
}