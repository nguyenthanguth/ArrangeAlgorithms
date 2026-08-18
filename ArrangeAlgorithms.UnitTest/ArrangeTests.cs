using ArrangeAlgorithms.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ArrangeAlgorithms.UnitTest
{
    /// <summary>
    /// Tests the public API of <see cref="Arrange"/> and contracts that EVERY algorithm must uphold.
    /// </summary>
    public class ArrangeTests
    {
        private static ArrangeOptions OptionsFor(ArrangeAlgorithmType algorithm)
        {
            return new ArrangeOptions
            {
                Algorithm = algorithm,
                RowGap = 5.0,
                PerpendicularLevels = 3
            };
        }

        private static Arrange LabelOn(GeoLine leader)
        {
            return new Arrange
            {
                GeoLine = leader,
                GeoRectangle = new GeoRectangle(leader.MidPoint, 20.0, 10.0),
                MarkOffsetFromLine = 5.0
            };
        }

        private static GeoRectangle MovedBox(Arrange arrange, GeoVector translation)
        {
            return new GeoRectangle(
                arrange.GeoRectangle.Center + translation,
                arrange.GeoRectangle.Width,
                arrange.GeoRectangle.Height,
                arrange.GeoRectangle.AngleRad);
        }

        // ------------------------------------------------------------------
        // Check input parameters
        // ------------------------------------------------------------------

        [Fact]
        public void Arrange_Run_WithNullList_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Arrange.Run(null));
            Assert.Throws<ArgumentNullException>(() => Arrange.Run(null, ArrangeOptions.Default));
        }

        [Fact]
        public void Arrange_Run_WithNullOptions_Throws()
        {
            var labels = new List<Arrange> { LabelOn(new GeoLine(0.0, 0.0, 40.0, 0.0)) };

            Assert.Throws<ArgumentNullException>(() => Arrange.Run(labels, null));
        }

        [Fact]
        public void Arrange_GetPlacePoints_WithNullOptions_Throws()
        {
            var label = LabelOn(new GeoLine(0.0, 0.0, 40.0, 0.0));

            Assert.Throws<ArgumentNullException>(() => label.GetPlacePoints(null));
        }

        [Fact]
        public void Arrange_Run_SingleArgumentOverload_UsesDefaultOptions()
        {
            // Label does not set MarkOffsetFromLine, so it keeps Arrange's default value of 50.
            var label = new Arrange
            {
                GeoLine = new GeoLine(0.0, 0.0, 400.0, 0.0),
                GeoRectangle = new GeoRectangle(new GeoPoint(200.0, 0.0), 20.0, 10.0)
            };

            Arrange.Run(new List<Arrange> { label });

            // Default BaseOffset = half height (5) + MarkOffsetFromLine (50) = 55.
            Assert.Equal(55.0, Math.Abs(MovedBox(label, label.TranslationVector).Center.Y), 6);
        }

        // ------------------------------------------------------------------
        // Generate candidate positions
        // ------------------------------------------------------------------

        [Fact]
        public void Arrange_GetPlacePoints_FirstPairIsSymmetricAtBaseOffset()
        {
            var label = LabelOn(new GeoLine(0.0, 0.0, 40.0, 0.0));
            var options = OptionsFor(ArrangeAlgorithmType.Greedy);

            var points = label.GetPlacePoints(options);

            // First pair must lie on the first perpendicular level (BaseOffset = height/2 + MarkOffsetFromLine = 5 + 5 = 10)
            Assert.True(points[0].IsEqualTo(new GeoPoint(20.0, 10.0)));
            Assert.True(points[1].IsEqualTo(new GeoPoint(20.0, -10.0)));
        }

        [Fact]
        public void Arrange_MarkOffsetFromLine_IsPerLabelNotGlobal()
        {
            // Offset is per-label, so two labels sharing the same ArrangeOptions must still
            // expand candidates from two different perpendicular levels.
            var leader = new GeoLine(0.0, 0.0, 40.0, 0.0);
            var near = LabelOn(leader);                       // MarkOffsetFromLine = 5
            var far = LabelOn(leader);
            far.MarkOffsetFromLine = 30.0;

            var options = OptionsFor(ArrangeAlgorithmType.Greedy);

            // BaseOffset = half label height (5) plus the label's own specific offset.
            Assert.True(near.GetPlacePoints(options)[0].IsEqualTo(new GeoPoint(20.0, 10.0)));
            Assert.True(far.GetPlacePoints(options)[0].IsEqualTo(new GeoPoint(20.0, 35.0)));
        }

        [Fact]
        public void Arrange_Run_HonoursEachLabelsOwnMarkOffsetFromLine()
        {
            // Same guide segment, same options: label declaring larger offset must
            // stop further from the guide segment, and both must lie exactly on their first perpendicular level.
            var leader = new GeoLine(0.0, 0.0, 40.0, 0.0);
            var near = LabelOn(leader);                       // MarkOffsetFromLine = 5
            var far = LabelOn(leader);
            far.MarkOffsetFromLine = 30.0;

            Arrange.Run(new List<Arrange> { near, far }, OptionsFor(ArrangeAlgorithmType.Greedy));

            Assert.Equal(10.0, Math.Abs(MovedBox(near, near.TranslationVector).Center.Y), 6);
            Assert.Equal(35.0, Math.Abs(MovedBox(far, far.TranslationVector).Center.Y), 6);
            Assert.True(near.Placed);
            Assert.True(far.Placed);
        }

        [Fact]
        public void Arrange_GetPlacePoints_MoreLevelsProduceMoreCandidates()
        {
            var label = LabelOn(new GeoLine(0.0, 0.0, 40.0, 0.0));

            var threeLevels = label.GetPlacePoints(OptionsFor(ArrangeAlgorithmType.Greedy));

            var single = OptionsFor(ArrangeAlgorithmType.Greedy);
            single.PerpendicularLevels = 1;
            var oneLevel = label.GetPlacePoints(single);

            Assert.True(threeLevels.Count > oneLevel.Count);
        }

        // ------------------------------------------------------------------
        // Invalid label dimensions
        // ------------------------------------------------------------------

        [Fact]
        public void Arrange_InvalidBoxSize_ReturnsZeroTranslations()
        {
            var leaderLine = new GeoLine(0.0, 0.0, 10.0, 0.0);
            var a1 = new Arrange
            {
                GeoRectangle = new GeoRectangle(new GeoPoint(5.0, 0.0), 2.0, 2.0), // Too small
                GeoLine = leaderLine
            };

            var options = new ArrangeOptions
            {
                MinimumBoxSize = 5.0
            };

            Arrange.Run(new List<Arrange> { a1 }, options);
            Assert.Equal(GeoVector.Zero, a1.TranslationVector);
            Assert.False(a1.Placed);
        }

        [Fact]
        public void Arrange_DegenerateLeader_LeavesLabelInPlaceAndReportsFailure()
        {
            var label = new Arrange
            {
                GeoLine = new GeoLine(5.0, 0.0, 5.0, 0.0),
                GeoRectangle = new GeoRectangle(new GeoPoint(5.0, 0.0), 20.0, 10.0),
                MarkOffsetFromLine = 5.0
            };

            Arrange.Run(new List<Arrange> { label }, OptionsFor(ArrangeAlgorithmType.Greedy));

            Assert.Equal(GeoVector.Zero, label.TranslationVector);

            // The label does not overlap anyone, but it has never been arranged either, so it must not be reported as successful.
            Assert.False(label.Placed);
        }

        [Fact]
        public void Arrange_LabelAlreadyOnFirstCandidate_ProducesZeroTranslation()
        {
            // Label is already at the first candidate position: nothing to translate,
            // and MinimumMoveDistance must suppress any minor errors.
            var leader = new GeoLine(0.0, 0.0, 40.0, 0.0);
            var label = new Arrange
            {
                GeoLine = leader,
                GeoRectangle = new GeoRectangle(new GeoPoint(20.0, 10.0), 20.0, 10.0),
                MarkOffsetFromLine = 5.0
            };

            Arrange.Run(new List<Arrange> { label }, OptionsFor(ArrangeAlgorithmType.Greedy));

            Assert.Equal(GeoVector.Zero, label.TranslationVector);
            Assert.True(label.Placed);
        }

        // ------------------------------------------------------------------
        // Common contract for all five algorithms
        // ------------------------------------------------------------------

        public static IEnumerable<object[]> AllAlgorithms()
        {
            yield return new object[] { ArrangeAlgorithmType.Greedy };
            yield return new object[] { ArrangeAlgorithmType.BoundedBacktracking };
            yield return new object[] { ArrangeAlgorithmType.SimulatedAnnealing };
            yield return new object[] { ArrangeAlgorithmType.ForceDirected };
            yield return new object[] { ArrangeAlgorithmType.ConstraintSatisfaction };
        }

        [Theory]
        [MemberData(nameof(AllAlgorithms))]
        public void Arrange_Run_PopulatesTranslationVector(ArrangeAlgorithmType algorithm)
        {
            var labels = new List<Arrange>();
            for (int i = 0; i < 5; i++)
            {
                labels.Add(LabelOn(new GeoLine(i * 50.0, 0.0, i * 50.0 + 40.0, 0.0)));
            }

            Arrange.Run(labels, OptionsFor(algorithm));

            // Verify that all labels were successfully arranged
            Assert.True(labels.All(l => l.Placed));
        }

        [Theory]
        [MemberData(nameof(AllAlgorithms))]
        public void Arrange_Run_WithEmptyList_DoesNotThrow(ArrangeAlgorithmType algorithm)
        {
            Arrange.Run(new List<Arrange>(), OptionsFor(algorithm));
        }

        [Theory]
        [MemberData(nameof(AllAlgorithms))]
        public void Arrange_Run_WithNullEntries_KeepsIndicesAligned(ArrangeAlgorithmType algorithm)
        {
            var first = LabelOn(new GeoLine(0.0, 0.0, 40.0, 0.0));
            var second = LabelOn(new GeoLine(200.0, 0.0, 240.0, 0.0));

            var labels = new List<Arrange> { first, null, second };
            Arrange.Run(labels, OptionsFor(algorithm));

            // Each real label must still stick to its own guide segment without index swap.
            // Do not compare absolute coordinates: Force-directed intentionally slides labels along guide segment to spread them,
            // so the correct assertion is "closer to its own guide segment than to the other label's guide segment".
            GeoPoint firstCentre = MovedBox(first, first.TranslationVector).Center;
            GeoPoint secondCentre = MovedBox(second, second.TranslationVector).Center;

            Assert.True(first.GeoLine.DistanceTo(firstCentre) < second.GeoLine.DistanceTo(firstCentre));
            Assert.True(second.GeoLine.DistanceTo(secondCentre) < first.GeoLine.DistanceTo(secondCentre));
        }

        [Theory]
        [MemberData(nameof(AllAlgorithms))]
        public void Arrange_Run_SeparatesTwoLabelsSharingOneLeader(ArrangeAlgorithmType algorithm)
        {
            var leader = new GeoLine(0.0, 0.0, 40.0, 0.0);
            var a = LabelOn(leader);
            var b = LabelOn(leader);

            Arrange.Run(new List<Arrange> { a, b }, OptionsFor(algorithm));

            Assert.False(MovedBox(a, a.TranslationVector).IntersectsWith(MovedBox(b, b.TranslationVector)));
            Assert.True(a.Placed);
            Assert.True(b.Placed);
        }

        [Theory]
        [MemberData(nameof(AllAlgorithms))]
        public void Arrange_Run_AvoidsStaticObstacle(ArrangeAlgorithmType algorithm)
        {
            var leader = new GeoLine(0.0, 0.0, 40.0, 0.0);
            var blockPoly = new GeoPolygon(
                new GeoPoint(-60.0, 0.0),
                new GeoPoint(60.0, 0.0),
                new GeoPoint(60.0, 40.0),
                new GeoPoint(-60.0, 40.0));

            var label = LabelOn(leader);
            label.BlockPolygons = new List<GeoPolygon> { blockPoly };

            Arrange.Run(new List<Arrange> { label }, OptionsFor(algorithm));
            var moved = MovedBox(label, label.TranslationVector);

            Assert.False(moved.IntersectsWith(blockPoly));
            Assert.True(moved.Center.Y < 0.0);
            Assert.True(label.Placed);
        }

        [Theory]
        [MemberData(nameof(AllAlgorithms))]
        public void Arrange_Run_IsReproducible(ArrangeAlgorithmType algorithm)
        {
            List<Arrange> Solve()
            {
                var labels = new List<Arrange>();
                for (int i = 0; i < 6; i++)
                {
                    labels.Add(LabelOn(new GeoLine(i * 12.0, 0.0, i * 12.0 + 30.0, 0.0)));
                }
                Arrange.Run(labels, OptionsFor(algorithm));
                return labels;
            }

            var first = Solve();
            var second = Solve();

            Assert.Equal(first.Count, second.Count);
            for (int i = 0; i < first.Count; i++)
            {
                Assert.Equal(first[i].TranslationVector, second[i].TranslationVector);
            }
        }

        [Theory]
        [MemberData(nameof(AllAlgorithms))]
        public void Arrange_Run_PlacedFlagMatchesFinalLayout(ArrangeAlgorithmType algorithm)
        {
            // The Placed flag must describe the FINAL layout. Three labels sharing a short guide segment with one
            // perpendicular level only have room for two, so the count of successful labels must match the count of
            // labels that actually do not overlap anyone — including labels overlapped by others falling back.
            var leader = new GeoLine(0.0, 0.0, 10.0, 0.0);
            var labels = new List<Arrange> { LabelOn(leader), LabelOn(leader), LabelOn(leader) };

            var options = OptionsFor(algorithm);
            options.PerpendicularLevels = 1;

            Arrange.Run(labels, options);

            var boxes = labels.Select((label, i) => MovedBox(label, label.TranslationVector)).ToList();
            for (int i = 0; i < labels.Count; i++)
            {
                bool clean = true;
                for (int j = 0; j < labels.Count && clean; j++)
                {
                    if (i != j && boxes[i].IntersectsWith(boxes[j])) clean = false;
                }

                Assert.Equal(clean, labels[i].Placed);
            }
        }
    }
}
