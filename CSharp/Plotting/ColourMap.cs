
using Core.Geometry;
using Core.Maths.Tensors;
using Core.Trees;
using FiniteElementAnalysis.Polyhedrals;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Drawing;
namespace FiniteElementAnalysis.Plotting
{
    public class ColorMap
    {
        public static Rgba32 GetColorForValue(double value, double min, double max, double? alpha)
        {
            // Normalize the value to be between 0 and 1
            double t = (value - min) / (max - min);
            t = Math.Max(0.0, Math.Min(1.0, t)); // Clamp t to the range [0, 1]

            // Map t to a color on the spectrum
            return InterpolateColor(t, alpha);
        }

        private static Rgba32 InterpolateColor(double t, double?alpha)
        {
            double r = 0, g = 0, b = 0;

            if (t <= 0.25)
            {
                // Transition from Blue to Cyan
                r = 0;
                g = t / 0.25;
                b = 1;
            }
            else if (t <= 0.5)
            {
                // Transition from Cyan to Green
                r = 0;
                g = 1;
                b = 1 - (t - 0.25) / 0.25;
            }
            else if (t <= 0.75)
            {
                // Transition from Green to Yellow
                r = (t - 0.5) / 0.25;
                g = 1;
                b = 0;
            }
            else
            {
                // Transition from Yellow to Red
                r = 1;
                g = 1 - (t - 0.75) / 0.25;
                b = 0;
            }
            if (alpha != null)
            {
                if (alpha > 0 && alpha < 1) { 
                
                }
                return new Rgba32((float)r, (float)g, (float)b, (float)alpha);
            }
            return new Rgba32((float)r, (float)g, (float)b);
        }
    }
}