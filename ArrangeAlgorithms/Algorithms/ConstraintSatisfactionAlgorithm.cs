using ArrangeAlgorithms.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace ArrangeAlgorithms.Algorithms
{
    /// <summary>
    /// Label arrangement algorithm based on Constraint Satisfaction Problem (CSP) theory.
    /// Applies backtracking search combined with MRV (Minimum Remaining Values) variable selection and Forward Checking pruning technique.
    /// </summary>
    internal class ConstraintSatisfactionAlgorithm : IArrangeAlgorithm
    {
        private class CSPVariable
        {
            public int OriginalIndex { get; }
            public Arrange Arrange { get; }
            // List of initial valid translation candidates (no static collisions)
            public List<GeoVector> Domain { get; set; }
            public GeoVector AssignedValue { get; set; }
            public bool IsAssigned { get; set; }

            public CSPVariable(int originalIndex, Arrange arrange)
            {
                OriginalIndex = originalIndex;
                Arrange = arrange;
                Domain = new List<GeoVector>();
                AssignedValue = GeoVector.Zero;
                IsAssigned = false;
            }
        }

        private int _backtrackSteps;
        private bool _isTimeout;

        public List<GeoVector> Arrange(List<Arrange> arranges, ArrangeOptions options)
        {
            var translations = new GeoVector[arranges.Count];
            // STEP 1: Collect initial static obstacles
            var staticObstacles = ArrangeAlgorithms.Arrange.CollectStaticObstacles(arranges);

            // STEP 2: Initialize CSP variables and filter initial domains
            var variables = new List<CSPVariable>();
            for (int i = 0; i < arranges.Count; i++)
            {
                var arrange = arranges[i];
                if (arrange == null) continue;

                var v = new CSPVariable(i, arrange);
                GeoPoint centre = arrange.GeoRectangle.Center;

                // Pre-filter obstacles out of the label's reach. The loop below runs
                // (number of candidates x number of obstacles) times, so filtering once here cuts most of the work.
                // Degenerate labels that cannot form bounds retain the full list.
                List<Obstacle> nearby = PlacementHeuristics.TryGetCandidateBounds(arrange, options, out Bounds region)
                    ? staticObstacles.Where(o => region.Overlaps(o.Box)).ToList()
                    : staticObstacles;

                foreach (GeoPoint candidate in arrange.EnumeratePlacePoints(options))
                {
                    GeoVector trans = centre.GetVectorTo(candidate);
                    GeoRectangle moved = arrange.GeoRectangle.Translate(trans);

                    // Only add to domain if candidate does not collide with static obstacles from the start
                    if (!ArrangeAlgorithms.Arrange.Collides(nearby, moved, options.Tolerance))
                    {
                        v.Domain.Add(trans);
                    }
                }

                // Pre-sort domain: candidates with smaller longitudinal shift are sorted first
                v.Domain = v.Domain.OrderBy(t => t.Length).ToList();
                variables.Add(v);
            }

            _backtrackSteps = 0;
            _isTimeout = false;

            // STEP 3: Solve the constraint satisfaction problem
            bool success = SolveCSP(variables, options);

            // STEP 4: If CSP fails completely (no clean solution),
            // automatically fallback to the greedy algorithm to maintain availability
            if (!success)
            {
                var greedy = new GreedyAlgorithm();
                return greedy.Arrange(arranges, options);
            }

            // STEP 5: Aggregate translation results
            for (int i = 0; i < arranges.Count; i++)
            {
                if (arranges[i] == null) continue;

                var variable = variables.Find(v => v.OriginalIndex == i);
                translations[i] = variable != null ? variable.AssignedValue : GeoVector.Zero;
            }

            return translations.ToList();
        }

        /// <summary>
        /// Recursively solves CSP using MRV heuristics and Forward Checking pruning.
        /// </summary>
        private bool SolveCSP(List<CSPVariable> variables, ArrangeOptions options)
        {
            // Check assignment count
            var unassigned = variables.Where(v => !v.IsAssigned).ToList();
            if (unassigned.Count == 0)
            {
                return true;
            }

            _backtrackSteps++;
            if (_backtrackSteps > options.MaxBacktrackSteps)
            {
                _isTimeout = true;
                return false;
            }

            // HEURISTIC MRV: Select the unassigned variable with the smallest domain size
            CSPVariable currentVar = unassigned.OrderBy(v => v.Domain.Count).First();

            if (currentVar.Domain.Count == 0)
            {
                // Stuck: Unassigned variable has no valid candidates left
                return false;
            }

            // Store backups of domains to restore during backtracking
            var domainsBackup = variables.ToDictionary(v => v.OriginalIndex, v => v.Domain.ToList());

            // Try each translation value in the current variable's domain
            foreach (GeoVector val in currentVar.Domain)
            {
                currentVar.AssignedValue = val;
                currentVar.IsAssigned = true;

                // FORWARD CHECKING: Filter the domains of other unassigned variables
                bool forwardCheckOk = true;
                GeoRectangle currentRect = currentVar.Arrange.GeoRectangle.Translate(val);

                foreach (var otherVar in variables.Where(v => !v.IsAssigned))
                {
                    // Remove candidates of otherVar that collide with the newly assigned currentVar label
                    var newDomain = new List<GeoVector>();
                    foreach (GeoVector otherVal in otherVar.Domain)
                    {
                        GeoRectangle otherRect = otherVar.Arrange.GeoRectangle.Translate(otherVal);

                        if (!currentRect.CollidesWith(otherRect, options.Tolerance))
                        {
                            newDomain.Add(otherVal);
                        }
                    }

                    otherVar.Domain = newDomain;

                    // If the domain of any variable becomes empty -> fail early
                    if (otherVar.Domain.Count == 0)
                    {
                        forwardCheckOk = false;
                        break;
                    }
                }

                if (forwardCheckOk)
                {
                    // Recursively assign the next variable
                    if (SolveCSP(variables, options))
                    {
                        return true;
                    }
                }

                // BACKTRACK: Restore the previous domain states
                currentVar.IsAssigned = false;
                foreach (var v in variables)
                {
                    v.Domain = domainsBackup[v.OriginalIndex].ToList();
                }

                if (_isTimeout)
                {
                    return false;
                }
            }

            return false;
        }
    }
}
