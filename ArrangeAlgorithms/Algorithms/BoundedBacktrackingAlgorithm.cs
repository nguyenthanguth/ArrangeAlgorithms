using ArrangeAlgorithms.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace ArrangeAlgorithms.Algorithms
{
    /// <summary>
    /// Label arrangement algorithm using a Bounded Backtracking strategy.
    /// Helps resolve local optima by trying alternative candidates when subsequent labels are stuck.
    /// </summary>
    internal class BoundedBacktrackingAlgorithm : IArrangeAlgorithm
    {
        private int _stepsCount;
        private bool _isTimeout;

        public List<GeoVector> Arrange(List<Arrange> arranges, ArrangeOptions options)
        {
            var translations = new GeoVector[arranges.Count];
            // STEP 1: Collect initial static obstacles
            var occupied = ArrangeAlgorithms.Arrange.CollectStaticObstacles(arranges);

            // STEP 2: Calculate label processing priority order
            var processingOrder = PlacementHeuristics.GetProcessingOrder(arranges, occupied, options).ToArray();
            var sortedArranges = processingOrder.Select(idx => arranges[idx]).ToList();
            var sortedTranslations = new GeoVector[sortedArranges.Count];

            _stepsCount = 0;
            _isTimeout = false;

            // STEP 3: Run the recursive backtracking algorithm
            bool success = Backtrack(0, sortedArranges, occupied, sortedTranslations, options);

            // STEP 4: If backtracking fails completely (no collision-free configuration is found),
            // fallback to the Greedy solution to ensure all labels still have a visible position.
            if (!success)
            {
                var greedy = new GreedyAlgorithm();
                return greedy.Arrange(arranges, options);
            }

            // STEP 5: Map arrangement results back to the original input order
            for (int i = 0; i < processingOrder.Length; i++)
            {
                int originalIndex = processingOrder[i];
                translations[originalIndex] = sortedTranslations[i];
            }

            return translations.ToList();
        }

        /// <summary>
        /// Recursive backtracking function to place the label at the specified index.
        /// </summary>
        private bool Backtrack(int index, List<Arrange> sortedArranges, List<Obstacle> occupied, GeoVector[] translations, ArrangeOptions options)
        {
            // Recursion base case: All labels have been successfully placed
            if (index >= sortedArranges.Count)
            {
                return true;
            }

            _stepsCount++;
            if (_stepsCount > options.MaxBacktrackSteps)
            {
                _isTimeout = true;
                return false;
            }

            Arrange arrange = sortedArranges[index];
            if (!PlacementHeuristics.TryGetCandidateBounds(arrange, options, out Bounds region))
            {
                // Cannot form layout for this label: leave it in place and proceed.
                // Arrange.MarkPlacementResults will recognize it has not been arranged.
                translations[index] = GeoVector.Zero;
                return Backtrack(index + 1, sortedArranges, occupied, translations, options);
            }

            GeoPoint centre = arrange.GeoRectangle.Center;

            // Fast filtering of nearby obstacles
            List<Obstacle> nearby = occupied.Where(obstacle => region.Overlaps(obstacle.Box)).ToList();

            // Get list of empty (collision-free) candidates
            var candidates = new List<(GeoVector translation, double clearance)>();
            foreach (GeoPoint candidate in arrange.EnumeratePlacePoints(options))
            {
                GeoVector translation = centre.GetVectorTo(candidate);
                GeoRectangle moved = new GeoRectangle(arrange.GeoRectangle.Center.Add(translation), arrange.GeoRectangle.Width, arrange.GeoRectangle.Height, arrange.GeoRectangle.AngleRad);

                if (!ArrangeAlgorithms.Arrange.Collides(nearby, moved, options.Tolerance))
                {
                    double clearance = PlacementHeuristics.MeasureClearance(nearby, moved);
                    candidates.Add((translation, clearance));
                }
            }

            // Try positions closer to the guide segment first, break ties by selecting the one with more clearance.
            //
            // Previously this was sorted purely by descending clearance, meaning it always tried the FURTHEST position first and
            // almost always stopped there. Measured consequence: average distance from label to guide segment was 2405
            // compared to 550 for Greedy — every label was thrown to the outermost perpendicular level even if closer spots were empty.
            // All default values (Arrange.MarkOffsetFromLine, LongitudinalOvershootRatio) indicate that labels should stay close
            // to the guide segment, so translation magnitude is the primary criteria.
            //
            // Clearance is still useful to break ties: symmetric candidate generator makes equidistant positions
            // (top/bottom of guide segment, forward/backward slides) tie exactly very frequently.
            candidates = candidates
                .OrderBy(c => c.translation.Length)
                .ThenByDescending(c => c.clearance)
                .ToList();

            // Try placing the label into each potential empty candidate
            foreach (var item in candidates)
            {
                translations[index] = item.translation;

                // Add the newly placed label box as a temporary obstacle for subsequent recursion levels
                GeoRectangle moved = new GeoRectangle(arrange.GeoRectangle.Center.Add(item.translation), arrange.GeoRectangle.Width, arrange.GeoRectangle.Height, arrange.GeoRectangle.AngleRad);
                var obs = new Obstacle(moved);
                occupied.Add(obs);

                // Recursion to the next level
                if (Backtrack(index + 1, sortedArranges, occupied, translations, options))
                {
                    return true;
                }

                // If the next recursion level fails, remove the obstacle (Backtrack) and try the next candidate
                occupied.RemoveAt(occupied.Count - 1);

                if (_isTimeout)
                {
                    return false;
                }
            }

            return false;
        }
    }
}
