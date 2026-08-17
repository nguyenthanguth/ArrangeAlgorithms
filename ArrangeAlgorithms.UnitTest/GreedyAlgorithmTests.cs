using ArrangeAlgorithms.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ArrangeAlgorithms.UnitTest
{
    public class GreedyAlgorithmTests
    {
        /// <summary>Shared configuration for tests below, small dimensions for easy manual calculation.</summary>
        private static ArrangeOptions GreedyOptions(int perpendicularLevels = 3)
        {
            return new ArrangeOptions
            {
                Algorithm = ArrangeAlgorithmType.Greedy,
                RowGap = 5.0,
                PerpendicularLevels = perpendicularLevels
            };
        }

        /// <summary>20x10 label initially placed at the midpoint of the guide segment.</summary>
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

        [Fact]
        public void Arrange_Run_Greedy_ArrangesNonOverlappingLabels()
        {
            var leaderLine = new GeoLine(0.0, 0.0, 10.0, 0.0);
            var a1 = new Arrange
            {
                GeoRectangle = new GeoRectangle(new GeoPoint(5.0, 0.0), 20.0, 10.0),
                GeoLine = leaderLine,
                MarkOffsetFromLine = 5.0
            };
            var a2 = new Arrange
            {
                GeoRectangle = new GeoRectangle(new GeoPoint(5.0, 0.0), 20.0, 10.0),
                GeoLine = leaderLine,
                MarkOffsetFromLine = 5.0
            };

            var list = new List<Arrange> { a1, a2 };
            var options = new ArrangeOptions
            {
                Algorithm = ArrangeAlgorithmType.Greedy,
                RowGap = 5.0,
                PerpendicularLevels = 3
            };

            var translations = Arrange.Run(list, options);

            Assert.Equal(2, translations.Count);
            var moved1 = new GeoRectangle(a1.GeoRectangle.Center + translations[0], a1.GeoRectangle.Width, a1.GeoRectangle.Height);
            var moved2 = new GeoRectangle(a2.GeoRectangle.Center + translations[1], a2.GeoRectangle.Width, a2.GeoRectangle.Height);

            Assert.False(moved1.IntersectsWith(moved2));
            Assert.True(a1.Placed);
            Assert.True(a2.Placed);
        }

        [Fact]
        public void Arrange_Run_Greedy_RespectsObstacles()
        {
            // Horizontal guide segment lies directly below the label. It must have a real length: a degenerate guide segment
            // has no direction and thus generates no candidate positions.
            var leaderLine = new GeoLine(0.0, 0.0, 20.0, 0.0);
            var rect = new GeoRectangle(new GeoPoint(10.0, 10.0), 20.0, 10.0);

            var blockPoly = new GeoPolygon(
                new GeoPoint(-50.0, 0.0),
                new GeoPoint(50.0, 0.0),
                new GeoPoint(50.0, 30.0),
                new GeoPoint(-50.0, 30.0)
            );

            var arrange = new Arrange
            {
                GeoRectangle = rect,
                GeoLine = leaderLine,
                MarkOffsetFromLine = 5.0,
                BlockPolygons = new List<GeoPolygon> { blockPoly }
            };

            var options = new ArrangeOptions
            {
                Algorithm = ArrangeAlgorithmType.Greedy,
                RowGap = 5.0,
                PerpendicularLevels = 2
            };

            var translations = Arrange.Run(new List<Arrange> { arrange }, options);

            var moved = new GeoRectangle(arrange.GeoRectangle.Center + translations[0], arrange.GeoRectangle.Width, arrange.GeoRectangle.Height);

            Assert.True(moved.Center.Y < 0.0);
            Assert.False(blockPoly.IntersectsWith(moved));
            Assert.True(arrange.Placed);
        }

        [Fact]
        public void Arrange_Run_Greedy_WithNoValidSpaces_FallsBackToFirstCandidate()
        {
            var leaderLine = new GeoLine(0.0, 0.0, 10.0, 0.0);
            // The label starts far from the candidate cluster. If it is placed exactly at the first candidate,
            // the fallback to that candidate yields a zero vector, and the test becomes meaningless.
            var rect = new GeoRectangle(new GeoPoint(5.0, 40.0), 20.0, 10.0);

            // Huge blocked polygon wrapping the entire space around the label area
            var blockPoly = new GeoPolygon(
                new GeoPoint(-100.0, -100.0),
                new GeoPoint(100.0, -100.0),
                new GeoPoint(100.0, 100.0),
                new GeoPoint(-100.0, 100.0)
            );

            var arrange = new Arrange
            {
                GeoRectangle = rect,
                GeoLine = leaderLine,
                MarkOffsetFromLine = 5.0,
                BlockPolygons = new List<GeoPolygon> { blockPoly }
            };

            var options = new ArrangeOptions
            {
                Algorithm = ArrangeAlgorithmType.Greedy,
                RowGap = 5.0,
                PerpendicularLevels = 2
            };

            var translations = Arrange.Run(new List<Arrange> { arrange }, options);

            // Regardless of constraints, the Greedy algorithm must fallback to the first candidate
            // instead of staying in place, which causes uncertainty.
            Assert.False(arrange.Placed);
            Assert.NotEqual(GeoVector.Zero, translations[0]);
        }

        [Fact]
        public void Arrange_Run_Greedy_ReturnsTranslationsInInputOrder()
        {
            // The second label is constrained by the blocked region and should be processed BEFORE the first label.
            // Still, the returned results must match the original input index.
            var near = LabelOn(new GeoLine(0.0, 0.0, 40.0, 0.0));
            var far = LabelOn(new GeoLine(1000.0, 0.0, 1040.0, 0.0));

            far.BlockPolygons = new List<GeoPolygon>
            {
                new GeoPolygon(
                    new GeoPoint(990.0, 0.0),
                    new GeoPoint(1060.0, 0.0),
                    new GeoPoint(1060.0, 30.0),
                    new GeoPoint(990.0, 30.0))
            };

            var translations = Arrange.Run(new List<Arrange> { near, far }, GreedyOptions());

            // translations[0] must correspond to the left label, translations[1] to the right label.
            Assert.Equal(20.0, MovedBox(near, translations[0]).Center.X, 6);
            Assert.Equal(1020.0, MovedBox(far, translations[1]).Center.X, 6);

            // And the label blocked above must dodge downwards.
            Assert.True(MovedBox(far, translations[1]).Center.Y < 0.0);
        }

        [Fact]
        public void Arrange_Run_Greedy_IsDeterministic()
        {
            List<GeoVector> Solve()
            {
                var labels = new List<Arrange>();
                for (int i = 0; i < 6; i++)
                {
                    labels.Add(LabelOn(new GeoLine(i * 12.0, 0.0, i * 12.0 + 30.0, 0.0)));
                }
                return Arrange.Run(labels, GreedyOptions());
            }

            var first = Solve();
            var second = Solve();

            Assert.Equal(first.Count, second.Count);
            for (int i = 0; i < first.Count; i++)
            {
                Assert.Equal(first[i], second[i]);
            }
        }

        [Fact]
        public void Arrange_Run_Greedy_WithEmptyList_ReturnsEmptyResult()
        {
            var translations = Arrange.Run(new List<Arrange>(), GreedyOptions());
            Assert.Empty(translations);
        }

        [Fact]
        public void Arrange_Run_Greedy_SkipsNullEntries()
        {
            var a = LabelOn(new GeoLine(0.0, 0.0, 40.0, 0.0));
            var b = LabelOn(new GeoLine(0.0, 0.0, 40.0, 0.0));

            var translations = Arrange.Run(new List<Arrange> { a, null, b }, GreedyOptions());

            // Null entry still occupies a slot in the results to prevent index shifting.
            Assert.Equal(3, translations.Count);
            Assert.Equal(GeoVector.Zero, translations[1]);

            // The two real labels must still be arranged normally.
            Assert.False(MovedBox(a, translations[0]).IntersectsWith(MovedBox(b, translations[2])));
            Assert.True(a.Placed);
            Assert.True(b.Placed);
        }

        [Fact]
        public void Arrange_Run_Greedy_AvoidsBlockLines()
        {
            var leader = new GeoLine(0.0, 0.0, 40.0, 0.0);
            var arrange = LabelOn(leader);

            // Block line directly obstructs the first candidate position (above the guide segment).
            var blockLine = new GeoLine(-20.0, 10.0, 60.0, 10.0);
            arrange.BlockLines = new List<GeoLine> { blockLine };

            var translations = Arrange.Run(new List<Arrange> { arrange }, GreedyOptions());
            var moved = MovedBox(arrange, translations[0]);

            Assert.True(moved.Center.Y < 0.0);
            Assert.False(moved.IntersectsWith(blockLine));
            Assert.True(arrange.Placed);
        }

        [Fact]
        public void Arrange_Run_Greedy_RotatedLabels_AreSeparatedAlongPerpendicular()
        {
            // The guide segment is tilted at 45 degrees; the label rotates to stay parallel to it.
            var leader = new GeoLine(0.0, 0.0, 40.0, 40.0);
            var box = new GeoRectangle(leader.MidPoint, 20.0, 10.0, Math.PI / 4.0);

            var a = new Arrange { GeoLine = leader, GeoRectangle = box, MarkOffsetFromLine = 5.0 };
            var b = new Arrange { GeoLine = leader, GeoRectangle = box, MarkOffsetFromLine = 5.0 };

            var translations = Arrange.Run(new List<Arrange> { a, b }, GreedyOptions());

            Assert.False(MovedBox(a, translations[0]).IntersectsWith(MovedBox(b, translations[1])));
            Assert.True(a.Placed);
            Assert.True(b.Placed);

            // The two labels must be placed on opposite sides of the guide segment, i.e., shifted in perpendicular directions.
            Assert.NotEqual(translations[0], translations[1]);
        }

        [Fact]
        public void Arrange_Run_Greedy_RepeatedObstacleOnEveryLabel_GivesSameResult()
        {
            // A common usage pattern is to assign the same blocked region to EVERY label. This only inflates
            // the obstacle list and must absolutely not change the arrangement result.
            GeoPolygon Block() => new GeoPolygon(
                new GeoPoint(-50.0, 0.0),
                new GeoPoint(50.0, 0.0),
                new GeoPoint(50.0, 30.0),
                new GeoPoint(-50.0, 30.0));

            List<GeoVector> Solve(bool onEveryLabel)
            {
                var block = Block();
                var labels = new List<Arrange>();
                for (int i = 0; i < 4; i++)
                {
                    var label = LabelOn(new GeoLine(i * 15.0, 0.0, i * 15.0 + 40.0, 0.0));
                    label.BlockPolygons = (onEveryLabel || i == 0)
                        ? new List<GeoPolygon> { block }
                        : new List<GeoPolygon>();
                    labels.Add(label);
                }
                return Arrange.Run(labels, GreedyOptions());
            }

            var once = Solve(false);
            var everywhere = Solve(true);

            for (int i = 0; i < once.Count; i++)
            {
                Assert.Equal(once[i], everywhere[i]);
            }
        }

        [Fact]
        public void Arrange_Run_Greedy_LabelOverlappedByLaterFallback_IsNotReportedAsPlaced()
        {
            // Three labels share a short guide segment but only have one perpendicular level, meaning there is only room for two.
            // The third label is forced to fallback and overlap an already placed label.
            var leader = new GeoLine(0.0, 0.0, 10.0, 0.0);
            var labels = new List<Arrange> { LabelOn(leader), LabelOn(leader), LabelOn(leader) };

            Arrange.Run(labels, GreedyOptions(perpendicularLevels: 1));

            // The OVERLAPPED label must also be reported as failed, not just the label causing the collision.
            // Previously it still retained the success flag because that spot was empty when its turn came.
            Assert.Equal(1, labels.Count(x => x.Placed));
        }

        [Fact]
        public void Arrange_Run_Greedy_WithoutConstraintOrdering_StillAvoidsOverlap()
        {
            var leader = new GeoLine(0.0, 0.0, 40.0, 0.0);
            var a = LabelOn(leader);
            var b = LabelOn(leader);

            var options = GreedyOptions();
            options.PlaceMostConstrainedFirst = false;
            options.PlaceFromInsideOut = false;

            var translations = Arrange.Run(new List<Arrange> { a, b }, options);

            Assert.False(MovedBox(a, translations[0]).IntersectsWith(MovedBox(b, translations[1])));
            Assert.True(a.Placed);
            Assert.True(b.Placed);
        }
    }
}

