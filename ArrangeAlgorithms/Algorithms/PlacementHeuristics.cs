using ArrangeAlgorithms.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ArrangeAlgorithms.Algorithms
{
    /// <summary>
    /// Heuristic calculations shared among sequential label arrangement algorithms
    /// (<see cref="GreedyAlgorithm"/> and <see cref="BoundedBacktrackingAlgorithm"/>):
    /// determining processing order, measuring clearance, and bounding obstacles filtering.
    /// </summary>
    internal static class PlacementHeuristics
    {
        /// <summary>
        /// Calculates processing (sorting) order of labels to optimize placement success.
        /// </summary>
        internal static IEnumerable<int> GetProcessingOrder(
            List<Arrange> arranges, List<Obstacle> staticObstacles, ArrangeOptions options)
        {
            int[] indices = Enumerable.Range(0, arranges.Count)
                .Where(index => arranges[index] != null)
                .ToArray();

            if (indices.Length == 0)
            {
                return indices;
            }

            double centroidX = 0;
            double centroidY = 0;

            // Calculate geometric centroid of the entire distribution area if inside-out placement is enabled
            if (options.PlaceFromInsideOut)
            {
                double sumX = 0;
                double sumY = 0;
                foreach (int index in indices)
                {
                    sumX += arranges[index].GeoRectangle.Center.X;
                    sumY += arranges[index].GeoRectangle.Center.Y;
                }
                centroidX = sumX / indices.Length;
                centroidY = sumY / indices.Length;
            }

            // MODE 1: Geometry-based sorting only (ignores collision freedom)
            if (!options.PlaceMostConstrainedFirst)
            {
                if (options.PlaceFromInsideOut)
                {
                    // Sort in ascending order of squared distance to centroid (from core to outer edge)
                    return indices
                        .OrderBy(index => GetSquaredDistanceToCentroid(arranges[index], centroidX, centroidY))
                        .ToArray();
                }

                // Default sorting: left to right, bottom to top
                return indices
                    .OrderBy(index => arranges[index].GeoRectangle.Center.X)
                    .ThenBy(index => arranges[index].GeoRectangle.Center.Y)
                    .ToArray();
            }

            // MODE 2: Collision-optimal sorting (Default).
            // Count free positions for each label to evaluate how constrained it is.
            // Freedom degree is measured against static obstacles, calculated once before placing any labels.
            // Re-measuring after each placement is more accurate but costs quadratic time relative to label count,
            // whereas most constraints originate from static obstacles.
            var freedom = new int[arranges.Count];
            var room = new int[arranges.Count];

            foreach (int index in indices)
            {
                freedom[index] = CountFreePlaces(arranges[index], staticObstacles, options);
                room[index] = CountAllPlaces(arranges[index], options);
            }

            if (options.PlaceFromInsideOut)
            {
                // Prioritize the most constrained label first. If tie in freedom, prioritize the one closer to the centroid.
                return indices
                    .OrderBy(index => freedom[index])
                    .ThenBy(index => GetSquaredDistanceToCentroid(arranges[index], centroidX, centroidY))
                    .ToArray();
            }

            // Secondary criteria is total candidates. Preferred candidate groups of two labels can be identical
            // — same origin, same expansion pattern — so free spots count within that group alone cannot distinguish
            // short guide segments from long guide segments. The escape path for long guide segment labels lies in
            // far-sliding candidates, outside the sampling range.
            //
            // LINQ's OrderBy is stable, so labels tying both criteria retain their input order and the result remains reproducible.
            return indices
                .OrderBy(index => freedom[index])
                .ThenBy(index => room[index])
                .ToArray();
        }

        /// <summary>
        /// Calculates the squared distance from the label center to the area centroid.
        /// </summary>
        private static double GetSquaredDistanceToCentroid(Arrange arrange, double centroidX, double centroidY)
        {
            double dx = arrange.GeoRectangle.Center.X - centroidX;
            double dy = arrange.GeoRectangle.Center.Y - centroidY;
            return dx * dx + dy * dy;
        }

        /// <summary>
        /// Counts the actual number of free positions within the first sample candidate group.
        /// </summary>
        private static int CountFreePlaces(Arrange arrange, List<Obstacle> staticObstacles, ArrangeOptions options)
        {
            GeoPoint centre = arrange.GeoRectangle.Center;
            int free = 0;
            int examined = 0;

            foreach (GeoPoint candidate in arrange.EnumeratePlacePoints(options))
            {
                // Only sample a small quantity configured by FreedomSampleSize (default = 12) to guarantee performance
                if (examined >= options.FreedomSampleSize)
                {
                    return free;
                }

                examined++;

                var translation = centre.GetVectorTo(candidate);
                var moved = new GeoRectangle(arrange.GeoRectangle.Center.Add(translation), arrange.GeoRectangle.Width, arrange.GeoRectangle.Height, arrange.GeoRectangle.AngleRad);

                // If this position does not overlap any obstacles, consider it a free position
                if (!ArrangeAlgorithms.Arrange.Collides(staticObstacles, moved, options.Tolerance))
                {
                    free++;
                }
            }

            return examined == 0 ? -1 : free;
        }

        /// <summary>
        /// Calculates the maximum total number of candidates that can be generated along the guide segment.
        /// </summary>
        private static int CountAllPlaces(Arrange arrange, ArrangeOptions options)
        {
            if (!arrange.TryGetLayout(options, out Layout layout))
            {
                return 0;
            }

            double step = layout.MaximumShift / 20.0;
            if (step < 0.1)
            {
                step = layout.Height;
            }

            // Each level has 2 middle positions plus 4 positions for each longitudinal shift step.
            long shiftsPerLevel = (long)Math.Floor(layout.MaximumShift / step);
            long total = (2L + 4L * shiftsPerLevel) * Math.Max(0, options.PerpendicularLevels);

            return (int)Math.Min(total, options.MaximumCandidates);
        }

        /// <summary>
        /// Calculates the minimum boundary-to-boundary distance from the label to all surrounding obstacles when there is no collision.
        /// </summary>
        internal static double MeasureClearance(List<Obstacle> obstacles, GeoRectangle moved)
        {
            double best = double.MaxValue;

            foreach (Obstacle obstacle in obstacles)
            {
                double distance = double.MaxValue;

                switch (obstacle.Type)
                {
                    case ObstacleType.GeoRectangle:
                        distance = moved.DistanceTo(obstacle.GeoRectangle);
                        break;
                    case ObstacleType.GeoPolygon:
                        distance = moved.DistanceTo(obstacle.GeoPolygon);
                        break;
                    case ObstacleType.GeoLine:
                        distance = moved.DistanceTo(obstacle.GeoLine);
                        break;
                }

                if (distance < best)
                {
                    best = distance;
                }
            }

            return best;
        }

        /// <summary>
        /// Calculates the bounding box containing all potential candidate points that can be generated.
        /// Used for rough filtering to exclude obstacles too far from the label.
        /// </summary>
        internal static bool TryGetCandidateBounds(Arrange arrange, ArrangeOptions options, out Bounds bounds)
        {
            if (!arrange.TryGetLayout(options, out Layout layout))
            {
                bounds = default(Bounds);
                return false;
            }

            double maximumOffset = layout.BaseOffset
                                   + Math.Max(0, options.PerpendicularLevels - 1) * (layout.Height + options.RowGap);

            GeoVector alongMax = layout.Direction * layout.MaximumShift;
            GeoVector acrossMax = layout.Perpendicular * maximumOffset;

            var corners = new[]
            {
                layout.Anchor + acrossMax + alongMax,
                layout.Anchor + acrossMax - alongMax,
                layout.Anchor - acrossMax + alongMax,
                layout.Anchor - acrossMax - alongMax,
            };

            // Create bounding box enclosing the 4 outer corners and expand it by NeighbourMargin for safety
            bounds = Bounds.Around(corners).Expand(arrange.GetBoxSpan(options) + options.NeighbourMargin);
            return true;
        }
    }
}
