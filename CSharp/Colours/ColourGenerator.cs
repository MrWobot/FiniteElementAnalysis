namespace FiniteElementAnalysis.MeshGeneration
{
    using System;
    using System.Drawing;  // Add reference to System.Drawing.Common for Color struct

    public class ColourGenerator
    {
        // Method to generate spaced-out colours
        public static byte[][] GetNWellSpacedColours(int nColoursRequired)
        {
            // Array to store the RGB colors
            byte[][] colors = new byte[nColoursRequired][];

            // Iterate over the number of submeshes and generate a distinct color for each
            for (int i = 0; i < nColoursRequired; i++)
            {
                // Calculate evenly spaced hue (we are cycling through the HSL color wheel)
                float hue = (i * 360f / nColoursRequired) % 360;  // Normalized hue [0, 360)
                float saturation = 0.7f;  // Set saturation and lightness to give vivid colors
                float lightness = 0.5f;

                // Convert HSL to RGB
                Color rgbColor = HslToRgb(hue, saturation, lightness);

                // Store the color as a byte array [R, G, B]
                colors[i] = new byte[] { rgbColor.R, rgbColor.G, rgbColor.B };
            }

            return colors;
        }

        // Method to convert HSL to RGB
        private static Color HslToRgb(float hue, float saturation, float lightness)
        {
            float chroma = (1 - Math.Abs(2 * lightness - 1)) * saturation;
            float hPrime = hue / 60f;
            float x = chroma * (1 - Math.Abs(hPrime % 2 - 1));

            float r = 0, g = 0, b = 0;
            if (hPrime >= 0 && hPrime < 1)
            {
                r = chroma;
                g = x;
            }
            else if (hPrime >= 1 && hPrime < 2)
            {
                r = x;
                g = chroma;
            }
            else if (hPrime >= 2 && hPrime < 3)
            {
                g = chroma;
                b = x;
            }
            else if (hPrime >= 3 && hPrime < 4)
            {
                g = x;
                b = chroma;
            }
            else if (hPrime >= 4 && hPrime < 5)
            {
                r = x;
                b = chroma;
            }
            else if (hPrime >= 5 && hPrime < 6)
            {
                r = chroma;
                b = x;
            }

            float m = lightness - chroma / 2f;
            r += m;
            g += m;
            b += m;

            // Convert to byte values [0, 255] and return Color
            return Color.FromArgb(
                (int)(r * 255),
                (int)(g * 255),
                (int)(b * 255)
            );
        }
    }

}