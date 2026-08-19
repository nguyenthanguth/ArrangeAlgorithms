using ArrangeAlgorithms.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ArrangeAlgorithms.Algorithms
{
    /// <summary>
    /// Label arrangement algorithm using a Simulated Annealing strategy.
    /// Simulates physical thermodynamics to reduce collisions and find global optimal solutions.
    /// </summary>
    internal class SimulatedAnnealingAlgorithm : IArrangeAlgorithm
    {
        public List<GeoVector> Arrange(List<Arrange> arranges, ArrangeOptions options)
        {
            if (arranges.Count == 0)
            {
                return new List<GeoVector>();
            }

            // STEP 1: Collect initial static obstacles
            var staticObstacles = ArrangeAlgorithms.Arrange.CollectStaticObstacles(arranges);

            // STEP 2: Pre-generate lists of all candidate translation points for each label
            var candidates = new List<List<(GeoVector Translation, GeoPoint GeoPoint)>>();
            var currentTranslations = new GeoVector[arranges.Count];
            var currentIndices = new int[arranges.Count];

            // Pre-filter nearby obstacles for each label once here.
            // The filtered region only depends on the guide segment and label dimensions, so it remains constant
            // throughout the annealing process, whereas CalculateEnergy is called thousands of times.
            // Filtering inside the loop would waste performance.
            var nearby = new List<Obstacle>[arranges.Count];

            for (int i = 0; i < arranges.Count; i++)
            {
                var arrange = arranges[i];
                var list = new List<(GeoVector Translation, GeoPoint GeoPoint)>();
                nearby[i] = staticObstacles;

                if (arrange != null)
                {
                    GeoPoint centre = arrange.GeoRectangle.Center;
                    foreach (GeoPoint p in arrange.EnumeratePlacePoints(options))
                    {
                        list.Add((centre.GetVectorTo(p), p));
                    }

                    if (PlacementHeuristics.TryGetCandidateBounds(arrange, options, out Bounds region))
                    {
                        nearby[i] = staticObstacles.Where(o => region.Overlaps(o.Box)).ToList();
                    }
                }

                candidates.Add(list);
                // Start at default level 0 position
                if (list.Count > 0)
                {
                    currentTranslations[i] = list[0].Translation;
                    currentIndices[i] = 0;
                }
                else
                {
                    currentTranslations[i] = GeoVector.Zero;
                    currentIndices[i] = -1;
                }
            }

            double currentEnergy = CalculateEnergy(arranges, currentTranslations, nearby, options);
            double bestEnergy = currentEnergy;
            var bestTranslations = currentTranslations.ToArray();

            // STEP 3: Configure simulated annealing temperature parameters
            double T = options.AnnealingInitialTemperature;
            double coolingRate = options.AnnealingCoolingRate;
            var random = new Random(42); // Fixed seed for reproducible results

            // STEP 4: Cooling loop
            while (T > 0.1)
            {
                // Perform 50 neighbor modification attempts at each temperature level
                for (int step = 0; step < 50; step++)
                {
                    // Randomly select a label
                    int i = random.Next(arranges.Count);
                    if (candidates[i].Count == 0) continue;

                    // Randomly select a different translation candidate for that label
                    int oldCandidateIndex = currentIndices[i];
                    int newCandidateIndex = random.Next(candidates[i].Count);
                    if (newCandidateIndex == oldCandidateIndex) continue;

                    GeoVector oldTranslation = currentTranslations[i];
                    GeoVector newTranslation = candidates[i][newCandidateIndex].Translation;

                    // Try updating
                    currentTranslations[i] = newTranslation;
                    currentIndices[i] = newCandidateIndex;

                    double nextEnergy = CalculateEnergy(arranges, currentTranslations, nearby, options);
                    double delta = nextEnergy - currentEnergy;

                    // Accept the new state based on lower energy or Boltzmann probability distribution
                    if (delta < 0 || random.NextDouble() < Math.Exp(-delta / T))
                    {
                        currentEnergy = nextEnergy;

                        // Record if this is the best solution
                        if (currentEnergy < bestEnergy)
                        {
                            bestEnergy = currentEnergy;
                            bestTranslations = currentTranslations.ToArray();
                        }
                    }
                    else
                    {
                        // Restore the previous state
                        currentTranslations[i] = oldTranslation;
                        currentIndices[i] = oldCandidateIndex;
                    }
                }

                // Cool down temperature
                T *= coolingRate;
            }

            // Placed flag is determined by Arrange.MarkPlacementResults on the final layout.
            return bestTranslations.ToList();
        }

        /// <summary>
        /// Calculates the total energy (penalty function) of the current label configuration.
        /// </summary>
        private static double CalculateEnergy(
            List<Arrange> arranges, GeoVector[] translations, List<Obstacle>[] nearby, ArrangeOptions options)
        {
            double energy = 0.0;

            var movedRects = new GeoRectangle[arranges.Count];
            var movedBoxes = new Bounds[arranges.Count];

            for (int i = 0; i < arranges.Count; i++)
            {
                if (arranges[i] == null) continue;

                movedRects[i] = arranges[i].GeoRectangle.Translate(translations[i]);

                movedBoxes[i] = Bounds.Of(movedRects[i]);
            }

            for (int i = 0; i < arranges.Count; i++)
            {
                if (arranges[i] == null) continue;

                GeoRectangle rect = movedRects[i];
                Bounds box = movedBoxes[i];

                // 1. Penalty for colliding with static obstacles
                if (ArrangeAlgorithms.Arrange.Collides(nearby[i], rect, options.Tolerance))
                {
                    energy += 10000.0;
                }

                // 2. Penalty for cross-collisions between mobile labels
                for (int j = i + 1; j < arranges.Count; j++)
                {
                    if (arranges[j] == null) continue;

                    if (box.Overlaps(movedBoxes[j]))
                    {
                        if (rect.CollidesWith(movedRects[j], options.Tolerance))
                        {
                            energy += 10000.0;
                        }
                    }
                }

                // 3. Penalty for label translation distance from original position (prioritizes labels staying closest to guide segment)
                energy += translations[i].Length * 0.1;
            }

            return energy;
        }
    }
}
