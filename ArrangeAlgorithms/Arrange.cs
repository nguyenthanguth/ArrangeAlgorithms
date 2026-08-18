using ArrangeAlgorithms.Algorithms;
using ArrangeAlgorithms.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ArrangeAlgorithms
{
    /// <summary>
    /// Represents a label to be arranged along with surrounding geometric objects (obstacles).
    /// </summary>
    public class Arrange
    {
        /// <summary>Gets or sets the bounding rectangle of the label — the geometry to be translated.</summary>
        public GeoRectangle GeoRectangle { get; set; }

        /// <summary>
        /// Gets or sets the path segment of the object the label points to.
        /// Its midpoint is the origin for expanding candidate positions.
        /// </summary>
        public GeoLine GeoLine { get; set; }

        /// <summary>
        /// Gets or sets the minimum perpendicular clearance between the label edge and the path.
        /// Added to half the label height, it determines the distance from the path to the label center.
        /// <para>
        /// This property is defined per label rather than in <see cref="ArrangeOptions"/> because
        /// each label may require a unique offset — e.g., larger text labels may need to be placed further away than smaller ones.
        /// </para>
        /// </summary>
        public double MarkOffsetFromLine { get; set; } = 50.0;

        /// <summary>Gets or sets the list of static block polygons that the label must not overlap.</summary>
        public List<GeoPolygon> BlockPolygons { get; set; }

        /// <summary>Gets or sets the list of static block lines that the label must not overlap.</summary>
        public List<GeoLine> BlockLines { get; set; }

        /// <summary>
        /// Indicates whether the label has been successfully placed in a completely empty position.
        /// </summary>
        public bool Placed { get; private set; }

        /// <summary>
        /// Gets the translation vector calculated for the label.
        /// </summary>
        public GeoVector TranslationVector { get; internal set; } = GeoVector.Zero;

        /// <summary>
        /// Sets the success or failure status of the label placement.
        /// </summary>
        internal void SetPlaced(bool value)
        {
            Placed = value;
        }

        /// <summary>Arranges the list of labels using the default configuration parameters.</summary>
        /// <param name="arranges">List of labels to be arranged.</param>
        /// <returns>List of translation GeoVectors for each label in the same input order.</returns>
        public static List<GeoVector> Run(List<Arrange> arranges)
        {
            return Run(arranges, ArrangeOptions.Default);
        }

        /// <summary>
        /// Arranges the list of labels to ensure they do not overlap each other or any blocked regions.
        /// </summary>
        /// <param name="arranges">List of labels to be arranged.</param>
        /// <param name="options">Configuration options controlling the algorithm.</param>
        /// <returns>List of translation GeoVectors for each label in the same input order.</returns>
        public static List<GeoVector> Run(List<Arrange> arranges, ArrangeOptions options)
        {
            if (arranges == null)
            {
                throw new ArgumentNullException(nameof(arranges));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            // Select arrangement algorithm based on options
            IArrangeAlgorithm algorithm;
            switch (options.Algorithm)
            {
                case ArrangeAlgorithmType.BoundedBacktracking:
                    algorithm = new BoundedBacktrackingAlgorithm();
                    break;
                case ArrangeAlgorithmType.SimulatedAnnealing:
                    algorithm = new SimulatedAnnealingAlgorithm();
                    break;
                case ArrangeAlgorithmType.ForceDirected:
                    algorithm = new ForceDirectedAlgorithm();
                    break;
                case ArrangeAlgorithmType.ConstraintSatisfaction:
                    algorithm = new ConstraintSatisfactionAlgorithm();
                    break;
                case ArrangeAlgorithmType.Greedy:
                default:
                    algorithm = new GreedyAlgorithm();
                    break;
            }

            // --- PASS 1: Strict arrangement with all constraints ---
            List<GeoVector> translations = algorithm.Arrange(arranges, options);
            MarkPlacementResults(arranges, translations, options);

            // --- PASS 2: Relaxation pass for failed labels ---
            var failedArranges = arranges.Where(w => w != null && !w.Placed).ToList();
            if (failedArranges.Count > 0)
            {
                RelaxFailedPlaced(arranges, failedArranges, translations, algorithm, options);
            }

            // Store the calculated translation vectors directly in the Arrange objects
            for (int i = 0; i < arranges.Count; i++)
            {
                if (arranges[i] != null)
                {
                    arranges[i].TranslationVector = translations[i];
                }
            }

            return translations;
        }

        /// <summary>
        /// Re-arranges only the failed labels by allowing them to overlap with their own BlockLines,
        /// while freezing successfully placed labels and treating them as static block obstacles.
        /// </summary>
        private static void RelaxFailedPlaced(
            List<Arrange> arranges,
            List<Arrange> failedArranges,
            List<GeoVector> translations,
            IArrangeAlgorithm algorithm,
            ArrangeOptions options)
        {
            // Record the original indices of failed labels to map results back later
            var failedIndices = new List<int>();
            for (int i = 0; i < arranges.Count; i++)
            {
                if (arranges[i] != null && !arranges[i].Placed)
                {
                    failedIndices.Add(i);
                }
            }

            // Backup original static constraints of failed labels
            var backupPolygons = new Dictionary<Arrange, List<GeoPolygon>>();
            var backupLines = new Dictionary<Arrange, List<GeoLine>>();

            // Convert successfully placed labels into static block polygons for failed labels to avoid
            var greenBoxes = new List<GeoPolygon>();
            for (int i = 0; i < arranges.Count; i++)
            {
                if (arranges[i] != null && arranges[i].Placed)
                {
                    var rect = new GeoRectangle(
                        arranges[i].GeoRectangle.Center.Add(translations[i]),
                        arranges[i].GeoRectangle.Width,
                        arranges[i].GeoRectangle.Height,
                        arranges[i].GeoRectangle.AngleRad);

                    greenBoxes.Add(new GeoPolygon(rect.GetVertices()));
                }
            }

            // Set up relaxed constraints (clear BlockLines and add green boxes to BlockPolygons)
            foreach (var arrange in failedArranges)
            {
                backupPolygons[arrange] = arrange.BlockPolygons;
                backupLines[arrange] = arrange.BlockLines;

                // Relax: Allow overlaps with guide BlockLines
                arrange.BlockLines = new List<GeoLine>();

                // Hard block: Do not overlap with successfully placed green labels
                var newPolygons = arrange.BlockPolygons != null 
                                  ? new List<GeoPolygon>(arrange.BlockPolygons) 
                                  : new List<GeoPolygon>();
                newPolygons.AddRange(greenBoxes);
                arrange.BlockPolygons = newPolygons;
            }

            // Re-run the arrangement algorithm ONLY for failed labels
            List<GeoVector> translations2 = algorithm.Arrange(failedArranges, options);

            // Restore original constraints to prevent side-effects on input data
            foreach (var arrange in failedArranges)
            {
                arrange.BlockPolygons = backupPolygons[arrange];
                arrange.BlockLines = backupLines[arrange];
            }

            // Map relaxation results back into the main translations list
            for (int i = 0; i < failedArranges.Count; i++)
            {
                int originalIndex = failedIndices[i];
                translations[originalIndex] = translations2[i];
            }

            // Re-evaluate final Placed flags on the combined layout
            MarkPlacementResults(arranges, translations, options);
        }

        /// <summary>
        /// Collects all static obstacles (block polygons and block lines) from the list of labels.
        /// <para>
        /// Duplicate obstacles are deduplicated to retain only a single instance. A common library usage pattern
        /// is to assign the same set of blocked regions to every label — e.g., each label avoids all other path segments —
        /// causing the list to grow quadratically with the number of labels, even though the number of distinct
        /// geometries remains small. Deduplication here benefits all algorithms.
        /// </para>
        /// </summary>
        internal static List<Obstacle> CollectStaticObstacles(List<Arrange> arranges)
        {
            var occupied = new List<Obstacle>();
            if (arranges == null) return occupied;

            var seenPolygons = new HashSet<GeoPolygon>();
            var seenLines = new HashSet<GeoLine>();

            foreach (Arrange arrange in arranges)
            {
                if (arrange == null) continue;

                if (arrange.BlockPolygons != null)
                {
                    foreach (GeoPolygon block in arrange.BlockPolygons)
                    {
                        if (block != null && seenPolygons.Add(block))
                        {
                            occupied.Add(new Obstacle(block));
                        }
                    }
                }

                if (arrange.BlockLines != null)
                {
                    foreach (GeoLine block in arrange.BlockLines)
                    {
                        if (seenLines.Add(block))
                        {
                            occupied.Add(new Obstacle(block));
                        }
                    }
                }
            }
            return occupied;
        }

        /// <summary>
        /// Checks whether the label rectangle after translation overlaps any obstacles.
        /// </summary>
        internal static bool Collides(List<Obstacle> obstacles, GeoRectangle moved, Tolerance tolerance)
        {
            var movedBox = Bounds.Of(moved);

            foreach (Obstacle obstacle in obstacles)
            {
                // Rough filtering using bounding box (AABB) first to improve collision check performance
                if (!movedBox.Overlaps(obstacle.Box))
                {
                    continue;
                }

                // Detailed collision check based on specific geometric type
                switch (obstacle.Type)
                {
                    case ObstacleType.GeoRectangle:
                        // OBB vs OBB: Using SAT (Separating Axis Theorem)
                        if (moved.IntersectsWith(obstacle.GeoRectangle, tolerance))
                            return true;
                        break;
                    case ObstacleType.GeoPolygon:
                        // OBB vs Polygon: Check edge intersections and containment
                        if (moved.IntersectsWith(obstacle.GeoPolygon, tolerance))
                            return true;
                        break;
                    case ObstacleType.GeoLine:
                        // OBB vs Line Segment: Check edge intersections and endpoints
                        if (moved.IntersectsWith(obstacle.GeoLine, tolerance))
                            return true;
                        break;
                }
            }

            return false;
        }

        /// <summary>
        /// Re-verifies results on the final layout and updates the <see cref="Placed"/> flag for each label.
        /// <para>
        /// Each algorithm only knows the state at the time it places a label, so the flag set by them means
        /// "this spot was empty when my turn came". A label placed later, when stuck, might fallback to a position
        /// that overlaps an already placed label, causing the overlapped label to still report success.
        /// Users need to know if the final layout has overlaps, so the final verification must be done here,
        /// after all labels have settled.
        /// </para>
        /// </summary>
        internal static void MarkPlacementResults(List<Arrange> arranges, IList<GeoVector> translations, ArrangeOptions options)
        {
            var staticObstacles = CollectStaticObstacles(arranges);

            var finalBoxes = new GeoRectangle[arranges.Count];
            var finalBounds = new Bounds[arranges.Count];
            for (int i = 0; i < arranges.Count; i++)
            {
                if (arranges[i] == null) continue;

                finalBoxes[i] = new GeoRectangle(
                    arranges[i].GeoRectangle.Center.Add(translations[i]),
                    arranges[i].GeoRectangle.Width,
                    arranges[i].GeoRectangle.Height,
                    arranges[i].GeoRectangle.AngleRad);
                finalBounds[i] = Bounds.Of(finalBoxes[i]);
            }

            for (int i = 0; i < arranges.Count; i++)
            {
                if (arranges[i] == null) continue;

                // A label that cannot form a layout has never been arranged. Just because it randomly
                // does not overlap anyone does not mean success, so it must be filtered out before collision checking.
                if (!arranges[i].TryGetLayout(options, out _))
                {
                    arranges[i].SetPlaced(false);
                    continue;
                }

                bool clean = !Collides(staticObstacles, finalBoxes[i], options.Tolerance);

                for (int j = 0; j < arranges.Count && clean; j++)
                {
                    if (i == j || arranges[j] == null) continue;
                    if (!finalBounds[i].Overlaps(finalBounds[j])) continue;
                    if (finalBoxes[i].IntersectsWith(finalBoxes[j], options.Tolerance)) clean = false;
                }

                arranges[i].SetPlaced(clean);
            }
        }

        /// <summary>Generates candidate position list with default configuration.</summary>
        public List<GeoPoint> GetPlacePoints()
        {
            return GetPlacePoints(ArrangeOptions.Default);
        }

        /// <summary>Generates candidate positions for the label center.</summary>
        public List<GeoPoint> GetPlacePoints(ArrangeOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return EnumeratePlacePoints(options).ToList();
        }

        /// <summary>
        /// Iterator for potential candidate points for the label center.
        /// The point expansion process starts from the path segment midpoint (Anchor):
        /// - Perpendicular offset to create different label rows.
        /// - Longitudinal shift along the parallel path direction.
        /// </summary>
        internal IEnumerable<GeoPoint> EnumeratePlacePoints(ArrangeOptions options)
        {
            if (!TryGetLayout(options, out Layout layout))
            {
                yield break;
            }

            int produced = 0;

            // Calculate dynamic longitudinal shift step based on 5% of maximum shift.
            // Enforce a minimum protection threshold of 0.1 to avoid zero steps causing infinite loops.
            double step = layout.MaximumShift / 20.0;
            if (step < 0.1)
            {
                step = layout.Height;
            }

            // Iterate through each perpendicular distance level (each label row)
            for (int level = 0; level < options.PerpendicularLevels; level++)
            {
                double offset = layout.BaseOffset + level * (layout.Height + options.RowGap);

                // Pure perpendicular shift (no longitudinal shift): right and left sides
                yield return layout.Anchor + layout.Perpendicular * offset;
                yield return layout.Anchor + layout.Perpendicular * -offset;
                produced += 2;

                double shift = step;

                // Slide label longitudinally in both directions (forward and backward) parallel to object direction
                while (shift <= layout.MaximumShift && produced < options.MaximumCandidates)
                {
                    // Top/Right row - backward shift
                    yield return layout.Anchor + layout.Perpendicular * offset - layout.Direction * shift;
                    // Bottom/Left row - backward shift
                    yield return layout.Anchor + layout.Perpendicular * -offset - layout.Direction * shift;
                    // Top/Right row - forward shift
                    yield return layout.Anchor + layout.Perpendicular * offset + layout.Direction * shift;
                    // Bottom/Left row - forward shift
                    yield return layout.Anchor + layout.Perpendicular * -offset + layout.Direction * shift;

                    produced += 4;
                    shift += step;
                }
            }
        }

        /// <summary>
        /// Calculates base geometric layout parameters based on path and label dimensions.
        /// </summary>
        internal bool TryGetLayout(ArrangeOptions options, out Layout layout)
        {
            layout = default(Layout);

            // STEP 1: Initial validity check of label box dimensions.
            // If width or height is smaller than minimum configuration, ignore it to prevent division by zero or geometric distortion.
            // Strict comparison (<): label dimensions exactly equal to threshold are still valid.
            if (GeoRectangle.Width < options.MinimumBoxSize || GeoRectangle.Height < options.MinimumBoxSize)
            {
                return false;
            }

            // STEP 2: Determine directional axis (unit GeoVector) along the label guide path (Anchor GeoLine).
            // This axis points in the direction where the label can slide longitudinally.
            if (!GeoLine.Direction.TryGetNormal(out GeoVector direction))
            {
                return false;
            }

            // Determine axis perpendicular to the label guide path.
            // This axis points in the direction to shift the label away or towards the object (forming label rows).
            GeoVector perpendicular = direction.GetPerpendicularVector();

            // Initialize extremum values to measure label bounding box after projection onto the new local coordinate system
            double alongMin = double.MaxValue;
            double alongMax = double.MinValue;
            double acrossMin = double.MaxValue;
            double acrossMax = double.MinValue;

            // STEP 3: Measure label bounding box in the new local coordinate system.
            // Iterate through 4 vertices of the label rectangle (which can be rotated at an arbitrary angle)
            var vertices = GeoRectangle.GetVertices();
            for (int i = 0; i < vertices.Length; i++)
            {
                // Transform vertex coordinates into a GeoVector from origin (0,0)
                GeoVector offset = new GeoPoint(0.0, 0.0).GetVectorTo(vertices[i]);

                // Project vertex onto the longitudinal path axis (dot product)
                double along = offset.DotProduct(direction);

                // Project vertex onto the perpendicular path axis (dot product)
                double across = offset.DotProduct(perpendicular);

                // Update minimum and maximum coordinate bounds on both axes
                alongMin = Math.Min(alongMin, along);
                alongMax = Math.Max(alongMax, along);
                acrossMin = Math.Min(acrossMin, across);
                acrossMax = Math.Max(acrossMax, across);
            }

            // Calculate actual width and height of the label in the local coordinate system
            double width = alongMax - alongMin;
            double height = acrossMax - acrossMin;

            // Verify size after projection to ensure it does not degenerate to zero
            if (width < options.MinimumBoxSize || height < options.MinimumBoxSize)
            {
                return false;
            }

            // STEP 4: Set up the complete Layout structure
            layout = new Layout(
                GeoLine.MidPoint, // Anchor point (midpoint of the guide path)
                direction,        // Local longitudinal axis
                perpendicular,    // Local perpendicular axis
                height,           // Actual label height along perpendicular axis

                // BaseOffset: Minimum perpendicular distance from the path to the center of the first label row,
                // which equals half the label height plus this label's unique offset margin (MarkOffsetFromLine)
                height * 0.5 + MarkOffsetFromLine,

                // MaximumShift: Maximum allowable longitudinal shift distance along the path,
                // which equals half the path length plus a portion of the label width overshooting the ends (LongitudinalOvershootRatio)
                GeoLine.Length * 0.5 + width * options.LongitudinalOvershootRatio);

            return true;
        }

        /// <summary>
        /// Calculates the maximum bounding box diagonal dimension of the label.
        /// </summary>
        internal double GetBoxSpan(ArrangeOptions options)
        {
            var vertices = GeoRectangle.GetVertices();
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;
            foreach (var v in vertices)
            {
                if (v.X < minX) minX = v.X;
                if (v.Y < minY) minY = v.Y;
                if (v.X > maxX) maxX = v.X;
                if (v.Y > maxY) maxY = v.Y;
            }

            return Math.Max(maxX - minX, maxY - minY);
        }
    }

    /// <summary>
    /// Internal structure containing base geometric layout information to generate candidates.
    /// </summary>
    internal readonly struct Layout
    {
        internal Layout(GeoPoint anchor, GeoVector direction, GeoVector perpendicular,
            double height, double baseOffset, double maximumShift)
        {
            Anchor = anchor;
            Direction = direction;
            Perpendicular = perpendicular;
            Height = height;
            BaseOffset = baseOffset;
            MaximumShift = maximumShift;
        }

        internal GeoPoint Anchor { get; }
        internal GeoVector Direction { get; }
        internal GeoVector Perpendicular { get; }
        internal double Height { get; }
        internal double BaseOffset { get; }
        internal double MaximumShift { get; }
    }

    internal enum ObstacleType
    {
        GeoPolygon,
        GeoLine,
        GeoRectangle
    }

    /// <summary>
    /// Represents a static obstacle or an occupied label.
    /// </summary>
    internal readonly struct Obstacle
    {
        internal ObstacleType Type { get; }
        internal GeoPolygon GeoPolygon { get; }
        internal GeoLine GeoLine { get; }
        internal GeoRectangle GeoRectangle { get; }
        internal Bounds Box { get; }

        internal Obstacle(GeoPolygon polygon)
        {
            Type = ObstacleType.GeoPolygon;
            GeoPolygon = polygon;
            GeoLine = default(GeoLine);
            GeoRectangle = default(GeoRectangle);
            Box = Bounds.Of(polygon);
        }

        internal Obstacle(GeoLine line)
        {
            Type = ObstacleType.GeoLine;
            GeoPolygon = null;
            GeoLine = line;
            GeoRectangle = default(GeoRectangle);
            Box = Bounds.Of(line);
        }

        internal Obstacle(GeoRectangle rectangle)
        {
            Type = ObstacleType.GeoRectangle;
            GeoPolygon = null;
            GeoLine = default(GeoLine);
            GeoRectangle = rectangle;
            Box = Bounds.Of(rectangle);
        }
    }

    /// <summary>
    /// Represents an AABB bounding box for fast collision filtering.
    /// </summary>
    internal readonly struct Bounds
    {
        internal double MinX { get; }
        internal double MinY { get; }
        internal double MaxX { get; }
        internal double MaxY { get; }

        private Bounds(double minX, double minY, double maxX, double maxY)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }

        internal static Bounds Of(GeoPolygon GeoPolygon)
        {
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;
            int count = GeoPolygon.VertexCount;
            for (int i = 0; i < count; i++)
            {
                var v = GeoPolygon[i];
                if (v.X < minX) minX = v.X;
                if (v.Y < minY) minY = v.Y;
                if (v.X > maxX) maxX = v.X;
                if (v.Y > maxY) maxY = v.Y;
            }
            return new Bounds(minX, minY, maxX, maxY);
        }

        internal static Bounds Of(GeoLine GeoLine)
        {
            return new Bounds(
                Math.Min(GeoLine.StartPoint.X, GeoLine.EndPoint.X),
                Math.Min(GeoLine.StartPoint.Y, GeoLine.EndPoint.Y),
                Math.Max(GeoLine.StartPoint.X, GeoLine.EndPoint.X),
                Math.Max(GeoLine.StartPoint.Y, GeoLine.EndPoint.Y)
            );
        }

        internal static Bounds Of(GeoRectangle rect)
        {
            var vertices = rect.GetVertices();
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;
            foreach (var v in vertices)
            {
                if (v.X < minX) minX = v.X;
                if (v.Y < minY) minY = v.Y;
                if (v.X > maxX) maxX = v.X;
                if (v.Y > maxY) maxY = v.Y;
            }
            return new Bounds(minX, minY, maxX, maxY);
        }

        internal static Bounds Around(IReadOnlyList<GeoPoint> points)
        {
            double minX = points[0].X;
            double minY = points[0].Y;
            double maxX = minX;
            double maxY = minY;

            for (int i = 1; i < points.Count; i++)
            {
                minX = Math.Min(minX, points[i].X);
                minY = Math.Min(minY, points[i].Y);
                maxX = Math.Max(maxX, points[i].X);
                maxY = Math.Max(maxY, points[i].Y);
            }

            return new Bounds(minX, minY, maxX, maxY);
        }

        internal Bounds Expand(double margin)
        {
            return new Bounds(MinX - margin, MinY - margin, MaxX + margin, MaxY + margin);
        }

        internal bool Overlaps(Bounds other)
        {
            return MinX <= other.MaxX && other.MinX <= MaxX
                                      && MinY <= other.MaxY && other.MinY <= MaxY;
        }
    }
}
