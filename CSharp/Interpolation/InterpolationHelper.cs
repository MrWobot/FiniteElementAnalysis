
using Core.Maths.Tensors;
using System;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FiniteElementAnalysis.Interpolation
{
    public static class InterpolationHelper
    {
        /// <summary>
        /// Performs Inverse Distance Weighting (IDW) to approximate the vector at a target point.
        /// </summary>
        /// <param name="target">The target point where we want to estimate the vector.</param>
        /// <param name="points">List of known points with their associated vector values.</param>
        /// <param name="power">The power parameter controlling distance weighting (default is 2).</param>
        /// <returns>The estimated vector at the target point.</returns>
        public static Vector3D InverseDistanceWeighting(Vector3D target, List<(Vector3D point, Vector3D vector)> points, double power = 2)
        {
            Vector3D weightedSum = Vector3D.Zeros();
            double weightSum = 0;

            foreach (var (point, vector) in points)
            {
                double distance = target.DistanceTo(point);
                if (distance == 0) return vector; // If target matches a known point, return its vector

                double weight = 1.0 / Math.Pow(distance, power);
                weightedSum += vector.Scale(weight); // Scale each vector by its weight
                weightSum += weight;
            }

            return weightedSum.Scale(1.0 / weightSum);
        }

        /// <summary>
        /// Performs Weighted Gaussian Interpolation to approximate the vector at a target point.
        /// </summary>
        /// <param name="target">The target point where we want to estimate the vector.</param>
        /// <param name="points">List of known points with their associated vector values.</param>
        /// <param name="alpha">The Gaussian weighting parameter (larger values reduce spread).</param>
        /// <returns>The estimated vector at the target point.</returns>
        public static Vector3D WeightedGaussianInterpolation(Vector3D target, List<(Vector3D point, Vector3D vector)> points, double alpha = 1)
        {
            Vector3D weightedSum = Vector3D.Zeros();
            double weightSum = 0;

            foreach (var (point, vector) in points)
            {
                double distance = target.DistanceTo(point);
                double weight = Math.Exp(-alpha * distance * distance); // Gaussian weighting
                weightedSum += vector.Scale(weight); // Scale each vector by its weight
                weightSum += weight;
            }

            return weightedSum.Scale(1.0 / weightSum);
        }
    }


}