using ArrangeAlgorithms.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ArrangeAlgorithms.Algorithms
{
    /// <summary>
    /// Label arrangement algorithm using continuous physical force simulation (Force-directed),
    /// followed by discrete mapping of label centers to the nearest non-colliding candidate positions.
    /// </summary>
    internal class ForceDirectedAlgorithm : IArrangeAlgorithm
    {
        /// <summary>Influence radius of repulsive force from static obstacles; beyond this, no force is contributed.</summary>
        private const double PushRadius = 1500.0;

        public List<GeoVector> Arrange(List<Arrange> arranges, ArrangeOptions options)
        {
            if (arranges.Count == 0)
            {
                return new List<GeoVector>();
            }

            var staticObstacles = ArrangeAlgorithms.Arrange.CollectStaticObstacles(arranges);
            var anchors = new GeoPoint[arranges.Count];
            var positions = new GeoPoint[arranges.Count];

            // STEP 1: Record initial default positions (Anchors)
            for (int i = 0; i < arranges.Count; i++)
            {
                var arrange = arranges[i];
                if (arrange == null) continue;

                anchors[i] = arrange.GeoRectangle.Center;
                positions[i] = arrange.GeoRectangle.Center;
            }

            // STEP 2: Run continuous physical force simulation
            int iterations = options.ForceIterations;
            double timestep = 0.5;

            for (int step = 0; step < iterations; step++)
            {
                var forces = new GeoVector[arranges.Count];
                for (int i = 0; i < arranges.Count; i++)
                {
                    forces[i] = GeoVector.Zero;
                }

                // 1. Spring Force pulling the label back to its original position to prevent it from drifting too far
                for (int i = 0; i < arranges.Count; i++)
                {
                    if (arranges[i] == null) continue;

                    GeoVector toAnchor = positions[i].GetVectorTo(anchors[i]);
                    forces[i] = forces[i].Add(toAnchor * 0.05); // Spring elasticity coefficient
                }

                // 2. Coulomb Repulsive Force pushing labels away from each other
                for (int i = 0; i < arranges.Count; i++)
                {
                    if (arranges[i] == null) continue;

                    for (int j = i + 1; j < arranges.Count; j++)
                    {
                        if (arranges[j] == null) continue;

                        GeoVector toOther = positions[i].GetVectorTo(positions[j]);
                        double distance = Math.Max(toOther.Length, 10.0);

                        // Only repel if two labels are too close to each other (threshold 2500mm)
                        if (distance < 2500.0)
                        {
                            double pushMagnitude = 150000.0 / (distance * distance);
                            if (!toOther.TryGetNormal(out GeoVector pushDir))
                            {
                                pushDir = GeoVector.XAxis;
                            }
                            forces[i] = forces[i].Subtract(pushDir * pushMagnitude);
                            forces[j] = forces[j].Add(pushDir * pushMagnitude);
                        }
                    }
                }

                // 3. Repulsive force from static obstacles (block polygons and lines)
                for (int i = 0; i < arranges.Count; i++)
                {
                    if (arranges[i] == null) continue;

                    // Obstacles further than PushRadius do not contribute force. Exclude them using bounding box
                    // overlap checks first, since GetClosestBoundaryPoint must iterate through each edge and is significantly more expensive.
                    Bounds reach = Bounds.Around(new[] { positions[i] }).Expand(PushRadius);

                    foreach (Obstacle obstacle in staticObstacles)
                    {
                        if (!reach.Overlaps(obstacle.Box))
                        {
                            continue;
                        }

                        // Get the closest point on the obstacle boundary, then push the label along the direction
                        // FROM that point TO the label. Taking the opposite direction (label -> obstacle center)
                        // would turn the repulsive force into an attractive force.
                        GeoPoint closest = GetClosestBoundaryPoint(obstacle, positions[i]);

                        double dist = Math.Max(closest.DistanceTo(positions[i]), 10.0);
                        if (dist >= PushRadius)
                        {
                            continue;
                        }

                        if (!closest.GetVectorTo(positions[i]).TryGetNormal(out GeoVector pushDir))
                        {
                            pushDir = GeoVector.XAxis;
                        }

                        double pushMagnitude = 200000.0 / (dist * dist);
                        forces[i] = forces[i].Add(pushDir * pushMagnitude);
                    }
                }

                // 4. Update label positions (Enforce maximum displacement to keep system stable)
                for (int i = 0; i < arranges.Count; i++)
                {
                    if (arranges[i] == null) continue;

                    GeoVector stepMove = forces[i] * timestep;
                    if (stepMove.Length > 500.0)
                    {
                        stepMove = stepMove.Normalize() * 500.0;
                    }

                    positions[i] = positions[i].Add(stepMove);
                }
            }

            // STEP 3: Discrete Mapping
            // Find the nearest non-colliding discrete candidate point to the final physical position
            var translations = new GeoVector[arranges.Count];
            var finalOccupied = new List<Obstacle>(staticObstacles);

            for (int i = 0; i < arranges.Count; i++)
            {
                var arrange = arranges[i];
                if (arrange == null) continue;

                GeoPoint centre = arrange.GeoRectangle.Center;
                GeoPoint physTarget = positions[i];
                GeoVector bestTranslation = GeoVector.Zero;
                double bestDist = double.MaxValue;
                bool mapped = false;

                // Pre-filter obstacles out of the label's reach. finalOccupied grows as labels
                // are placed, so this filter is beneficial even if the drawing has no static blocked regions.
                List<Obstacle> nearby = PlacementHeuristics.TryGetCandidateBounds(arrange, options, out Bounds region)
                    ? finalOccupied.Where(o => region.Overlaps(o.Box)).ToList()
                    : finalOccupied;

                // Iterate through all discrete candidates of the label
                foreach (GeoPoint candidate in arrange.EnumeratePlacePoints(options))
                {
                    GeoVector translation = centre.GetVectorTo(candidate);
                    GeoRectangle moved = new GeoRectangle(arrange.GeoRectangle.Center.Add(translation), arrange.GeoRectangle.Width, arrange.GeoRectangle.Height, arrange.GeoRectangle.AngleRad);

                    // Only accept if the candidate does not collide with static obstacles and previously placed labels
                    if (!ArrangeAlgorithms.Arrange.Collides(nearby, moved, options.Tolerance))
                    {
                        double dist = candidate.DistanceTo(physTarget);
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestTranslation = translation;
                            mapped = true;
                        }
                    }
                }

                // If no empty position is found, fallback to the default level 0 position of the label.
                // Placed flag is determined by Arrange.MarkPlacementResults on the final layout.
                if (!mapped)
                {
                    var points = arrange.EnumeratePlacePoints(options).ToList();
                    bestTranslation = points.Count > 0 ? centre.GetVectorTo(points[0]) : GeoVector.Zero;
                }

                translations[i] = bestTranslation;

                // Add the selected position as a static obstacle for subsequent labels
                GeoRectangle finalRect = new GeoRectangle(arrange.GeoRectangle.Center.Add(bestTranslation), arrange.GeoRectangle.Width, arrange.GeoRectangle.Height, arrange.GeoRectangle.AngleRad);
                finalOccupied.Add(new Obstacle(finalRect));
            }

            return translations.ToList();
        }

        /// <summary>
        /// Gets the point on the obstacle boundary closest to a given point.
        /// </summary>
        private static GeoPoint GetClosestBoundaryPoint(Obstacle obstacle, GeoPoint from)
        {
            switch (obstacle.Type)
            {
                case ObstacleType.GeoLine:
                    return obstacle.GeoLine.GetClosestPointTo(from);
                case ObstacleType.GeoRectangle:
                    return GetClosestPointOnEdges(obstacle.GeoRectangle.GetEdges(), from);
                default:
                    return GetClosestPointOnEdges(obstacle.GeoPolygon.GetEdges(), from);
            }
        }

        /// <summary>
        /// Gets the closest point to a given point among the closest points on each edge.
        /// </summary>
        private static GeoPoint GetClosestPointOnEdges(IEnumerable<GeoLine> edges, GeoPoint from)
        {
            GeoPoint best = from;
            double bestDistance = double.MaxValue;

            foreach (GeoLine edge in edges)
            {
                GeoPoint candidate = edge.GetClosestPointTo(from);
                double distance = candidate.DistanceTo(from);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            return best;
        }
    }
}
