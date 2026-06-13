
using Core.Geometry;
using Core.Maths.Tensors;
using Core.Trees;
using FiniteElementAnalysis.Polyhedrals;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using System.Text;
namespace FiniteElementAnalysis.Plotting
{
    public static class QuiverPlotHelper
    {
        public static void Plot(
            FinitePlane plane,
            Func<Vector3D, Vector3D?> getVectorAtPoint,
            int desiredNArrowsShortestDimension,
            int desiredNPixelsShortestDimension,
            string filePath)
        {
            double planeWidth = plane.Dimensions.X;
            double planeHeight = plane.Dimensions.Y;
            double shortestDimension = planeWidth < planeHeight ? planeWidth : planeHeight;
            double step = shortestDimension/desiredNArrowsShortestDimension;
            double middlePointX = plane.PlanePoint.X;
            double middlePointY = plane.PlanePoint.Y;
            double halfPlaneWidth = planeWidth / 2d;
            double halfPlaneHeight = planeHeight / 2d;
            double xFrom = middlePointX - halfPlaneWidth;
            double yFrom = middlePointY - halfPlaneHeight;
            double xTo = middlePointX + halfPlaneWidth;
            double yTo = middlePointY + halfPlaneHeight;
            List<VectorAtPoint> vectorAtPoints = new List<VectorAtPoint>();
            StringBuilder sbDebug = new StringBuilder();
            for (double y = yFrom; y <= yTo; y += step)
            {
                for (double x = xFrom; x <= xTo; x += step)
                {
                    Vector3D point = plane.Get3DPointFromXY(x, y, new Vector3D(1, 0, 0));
                    Vector3D? vector = getVectorAtPoint(point);
                    if (vector == null) continue;
                    Vector2D projectedVector = plane.ProjectVectorOntoPlane(vector);
                    sbDebug.AppendLine($"{x}, {y}, |{vector.Magnitude()}| [{vector.X}, {vector.Y}, {vector.Z}],  [{projectedVector.X}, {projectedVector.Y}] |{projectedVector.Magnitude}|");
                    vectorAtPoints.Add(new VectorAtPoint(projectedVector, x, y));
                }
            }
            string strDebug = sbDebug.ToString();
            var firstVectorPoint = vectorAtPoints.First();
            double minVectorMagnitude = firstVectorPoint.Vector.Magnitude;
            double maxVectorMagnitude = minVectorMagnitude;
            double minX = firstVectorPoint.X, maxX = minX, minY = firstVectorPoint.Y, maxY = minY;
            foreach (VectorAtPoint vectorAtPoint in vectorAtPoints.Skip(1)) {
                double magnitude = vectorAtPoint.Vector.Magnitude;
                if(magnitude<minVectorMagnitude)
                {
                    minVectorMagnitude = magnitude;
                }
                else if(magnitude>maxVectorMagnitude)
                {
                    maxVectorMagnitude = magnitude;
                }
                if (vectorAtPoint.X < minX)
                {
                    minX = vectorAtPoint.X;
                }
                if (vectorAtPoint.Y < minY)
                {
                    minY = vectorAtPoint.Y;
                }
                if (vectorAtPoint.X > maxX)
                {
                    maxX = vectorAtPoint.X;
                }
                if (vectorAtPoint.Y > maxY)
                {
                    maxY = vectorAtPoint.Y;
                }
            }
            double vectorMagnitudeRange = maxVectorMagnitude - minVectorMagnitude;
            double xRange = maxX - minX;
            double yRange = maxY - minY;

            int pixelsWidth, pixelsHeight;
            double shortestRange;
            if (xRange < yRange)
            {
                shortestRange = xRange;
                pixelsWidth = desiredNPixelsShortestDimension;
                pixelsHeight = (int)Math.Ceiling(pixelsWidth * yRange / xRange);
            }
            else
            {
                shortestRange = yRange;
                pixelsHeight = desiredNPixelsShortestDimension;
                pixelsWidth = (int)Math.Ceiling(pixelsHeight * xRange / yRange);
            }
            double nPixelsPerUnitRange = desiredNPixelsShortestDimension / shortestRange;
            double maxArrowLengthPixels = desiredNPixelsShortestDimension / desiredNArrowsShortestDimension;
            double minArrowLengthPixels = maxArrowLengthPixels / 4d;
            double arrowLengthVariationPixels = maxArrowLengthPixels - minArrowLengthPixels;
            double midPointXPixels = pixelsWidth/2d;
            double midPointYPixels = pixelsHeight / 2d;
            double maxVectorMagnitudeSquared = Math.Pow(maxVectorMagnitude, 2);
            using (var image = new Image<Rgba32>(pixelsWidth, pixelsHeight))
            {
                image.Mutate(ctx => ctx.Fill(Color.White));
                foreach (VectorAtPoint vectorAtPoint in vectorAtPoints)
                {
                    Vector2D pointInPixels = new Vector2D(
                        ((vectorAtPoint.X - minX)* nPixelsPerUnitRange),
                        ((vectorAtPoint.Y- minY) * nPixelsPerUnitRange)
                    );
                    double xSquared = Math.Pow(vectorAtPoint.Vector.X, 2);
                    double ySquared = Math.Pow(vectorAtPoint.Vector.Y, 2);
                    double totalSquared = ySquared + xSquared;
                    double xPixelsStartLength = (xSquared / totalSquared) * minArrowLengthPixels;
                    double yPixelsStartLength = (ySquared / totalSquared) * minArrowLengthPixels;
                    Vector2D halfVectorInPixels = new Vector2D(
                        Math.Sign(vectorAtPoint.Vector.X)*((Math.Sqrt(xSquared/ maxVectorMagnitudeSquared)*arrowLengthVariationPixels)+xPixelsStartLength),
                        Math.Sign(vectorAtPoint.Vector.Y) * ((Math.Sqrt(ySquared/ maxVectorMagnitudeSquared)* arrowLengthVariationPixels)+yPixelsStartLength)
                        ).Scale(0.5);
                    Vector2D arrowFromPixels = pointInPixels.Subtract(halfVectorInPixels);
                    Vector2D arrowToInPixels = pointInPixels.Add(halfVectorInPixels);
                    DrawArrow(image, (float)arrowFromPixels.X, (float)arrowFromPixels.Y, (float)arrowToInPixels.X, (float)arrowToInPixels.Y);
                }
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filePath));
                image.Save(filePath);
            }
        }
        static void DrawArrow(Image<Rgba32> image, float x1, float y1, float x2, float y2)
        {
            image.Mutate(ctx => ctx.DrawLine(Color.Black, 1, new PointF(x1, y1), new PointF(x2, y2)));
            // Draw the arrowhead
            float arrowHeadSize = (float)Math.Sqrt(Math.Pow(x2-x1, 2)+Math.Pow(y2-y1, 2))/4f;

            // Calculate the direction of the arrowhead
            float angle = (float)Math.Atan2(y2 - y1, x2 - x1) + MathF.PI;

            // Create two points for the arrowhead
            PointF p1 = new PointF(
                x2 + arrowHeadSize * MathF.Cos(angle - MathF.PI / 6),
                y2 + arrowHeadSize * MathF.Sin(angle - MathF.PI / 6)
            );

            PointF p2 = new PointF(
                x2 + arrowHeadSize * MathF.Cos(angle + MathF.PI / 6),
                y2 + arrowHeadSize * MathF.Sin(angle + MathF.PI / 6)
            );

            // Draw the two lines forming the arrowhead
            image.Mutate(ctx => ctx.DrawLine(Color.Black, 1, new PointF(x2, y2), p1));
            image.Mutate(ctx => ctx.DrawLine(Color.Black, 1, new PointF(x2, y2), p2));
        }
    }
}