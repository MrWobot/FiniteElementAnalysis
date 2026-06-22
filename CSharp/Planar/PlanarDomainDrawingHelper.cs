using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using FiniteElementAnalysis.Polyhedrals;
using Core.Maths.Tensors;
using Core.Geometry;
using Core.Graphics;
using SixLabors.ImageSharp.ColorSpaces;

namespace FiniteElementAnalysis.Planar
{

    public static class PlanarDomainDrawingHelper
    {
        public static void Draw(PlanarDomain domain, string filePath, int smallestDimensionPixels = 1000)
        {
            Core.Geometry.Rectangle domainRectangle = domain.Rectangle;
            int imageWidth;
            int imageHeight;
            double domainWidth = domainRectangle.XTo - domainRectangle.XFrom;
            double domainHeight= domainRectangle.YTo - domainRectangle.YFrom;
            if (domainWidth > domainHeight) {
                imageWidth = (int)Math.Ceiling(((double)smallestDimensionPixels) * (domainWidth / domainHeight));
                imageHeight = smallestDimensionPixels;
            }
            else { 
                imageHeight = (int)Math.Ceiling(((double)smallestDimensionPixels)*(domainHeight/ domainWidth));
                imageWidth = smallestDimensionPixels;
            }
            var image = Create(imageWidth, imageHeight, Color.Transparent);
            var nodeVectorToImageCoordinate = Create_NodeVectorToImageCoordinate(domainRectangle, imageWidth, imageHeight);
            foreach (var segment in domain.Segments) {
                var node1 = nodeVectorToImageCoordinate(segment.Nodes[0]);
                var node2 = nodeVectorToImageCoordinate(segment.Nodes[1]);
                var node3 = nodeVectorToImageCoordinate(segment.Nodes[2]);
                FillTriangle(image, node1.X, node1.Y, node2.X, node2.Y, node3.X, node3.Y, ToRGBA32(segment.VolumeBelongsTo.Color));
            }
            foreach (var edge in domain.Edges) {
                if (edge.Boundary == null) continue;
                var node1 = nodeVectorToImageCoordinate(edge.Node1);
                var node2 = nodeVectorToImageCoordinate(edge.Node2);
                DrawLine(image, node1.X, node1.Y, node2.X, node2.Y, 4f, ToRGBA32((edge.Boundary.Color)));
            }
            Save(image, filePath);
        }
        private static Rgba32 ToRGBA32(RGBF rgbf)
        {
            rgbf.ToBytes(out byte r, out byte g, out byte b);
            return new Rgba32(
                r, g, b,
                255
            );
        }
        private static Func<Vector2D, Vector2f> Create_NodeVectorToImageCoordinate(Core.Geometry.Rectangle domainRectangle, int imageWidth, int imageHeight) {
            return (nodeVector2D) =>
            {
                return new Vector2f(
                    (float)((
                        (nodeVector2D.X - domainRectangle.XFrom)
                        / (domainRectangle.XTo - domainRectangle.XFrom)
                    )
                    * (double)imageWidth),
                    (float)((
                        (nodeVector2D.Y - domainRectangle.YFrom)
                        / (domainRectangle.YTo - domainRectangle.YFrom)
                    )
                    * (double)imageHeight)
                );
            };
        }
        private static Image<Rgba32> Create(int width, int height, Color background)
            => new Image<Rgba32>(width, height, background);

        private static Image<Rgba32> FillTriangle(Image<Rgba32> img,
            float x0, float y0, float x1, float y1, float x2, float y2, Color c)
        {
            var path = new PathBuilder()
                .MoveTo(new PointF(x0, y0))
                .LineTo(x1, y1)
                .LineTo(x2, y2)
                .CloseFigure()
                .Build();

            img.Mutate(ctx => ctx.Fill(c, path));
            return img;
        }

        private static Image<Rgba32> DrawLine(Image<Rgba32> img,
            float x0, float y0, float x1, float y1, float thickness, Color c)
        {
            img.Mutate(ctx => ctx.DrawLine(c, thickness,
                new PointF(x0, y0), new PointF(x1, y1)));
            return img;
        }

        private static void Save(Image<Rgba32> img, string path)
            => img.Save(path); // format inferred from extension — .png, .bmp etc

        private static Func<Boundaries.Volume, Color> Create_GetVolumeColour(Func<Color> getNextColour)
        {
            return Create_GetTColour<Boundaries.Volume>(getNextColour);
        }
        private static Func<Boundaries.Boundary, Color> Create_GetBoundaryColour(Func<Color> getNextColour)
        {
            return Create_GetTColour<Boundaries.Boundary>(getNextColour);
        }
        private static Func<T, Color> Create_GetTColour<T>(Func<Color> getNextColour) {
            var mapTToColour = new Dictionary<T, Color>();
            return (T t) =>
            {
                if (mapTToColour.TryGetValue(t, out Color colour))
                {
                    return colour;
                }
                colour = getNextColour();
                mapTToColour[t] = colour;
                return colour;
            };
        }
    }
}