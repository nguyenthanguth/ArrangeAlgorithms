using ArrangeAlgorithms.Geometry;
using System;
using System.Collections.Generic;
using Xunit;

namespace ArrangeAlgorithms.UnitTest
{
    public class SimulatedAnnealingAlgorithmTests
    {
        [Fact]
        public void Arrange_Run_SimulatedAnnealing_FindsSolution()
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

            var options = new ArrangeOptions
            {
                Algorithm = ArrangeAlgorithmType.SimulatedAnnealing,
                RowGap = 5.0,
                PerpendicularLevels = 2
            };

            var translations = Arrange.Run(new List<Arrange> { a1, a2 }, options);

            Assert.Equal(2, translations.Count);
            var moved1 = new GeoRectangle(a1.GeoRectangle.Center + translations[0], a1.GeoRectangle.Width, a1.GeoRectangle.Height);
            var moved2 = new GeoRectangle(a2.GeoRectangle.Center + translations[1], a2.GeoRectangle.Width, a2.GeoRectangle.Height);

            Assert.False(moved1.IntersectsWith(moved2));
        }

        [Fact]
        public void Arrange_Run_SimulatedAnnealing_UsesFixedSeedSoResultsAreReproducible()
        {
            // Simulated annealing is inherently random, but the library fixes the seed so that the layout remains unchanged
            // after each rerun. This is an important contract with CAD users.
            List<GeoVector> Solve()
            {
                var leader = new GeoLine(0.0, 0.0, 40.0, 0.0);
                var labels = new List<Arrange>();
                for (int i = 0; i < 5; i++)
                {
                    labels.Add(new Arrange
                    {
                        GeoRectangle = new GeoRectangle(leader.MidPoint, 20.0, 10.0),
                        GeoLine = leader,
                        MarkOffsetFromLine = 5.0
                    });
                }

                return Arrange.Run(labels, new ArrangeOptions
                {
                    Algorithm = ArrangeAlgorithmType.SimulatedAnnealing,
                    RowGap = 5.0,
                    PerpendicularLevels = 3
                });
            }

            var first = Solve();
            var second = Solve();

            for (int i = 0; i < first.Count; i++)
            {
                Assert.Equal(first[i], second[i]);
            }
        }

        [Fact]
        public void Arrange_Run_SimulatedAnnealing_PenalisesDistanceFromLeader()
        {
            // The energy function adds a penalty based on translation magnitude, so a solitary label
            // must stop at the nearest perpendicular level instead of wandering far away.
            var leader = new GeoLine(0.0, 0.0, 40.0, 0.0);
            var label = new Arrange
            {
                GeoRectangle = new GeoRectangle(leader.MidPoint, 20.0, 10.0),
                GeoLine = leader,
                MarkOffsetFromLine = 5.0
            };

            var translations = Arrange.Run(new List<Arrange> { label }, new ArrangeOptions
            {
                Algorithm = ArrangeAlgorithmType.SimulatedAnnealing,
                RowGap = 5.0,
                PerpendicularLevels = 3
            });

            var moved = new GeoRectangle(label.GeoRectangle.Center + translations[0], 20.0, 10.0);

            Assert.Equal(10.0, Math.Abs(moved.Center.Y), 6);
            Assert.True(label.Placed);
        }

        [Fact]
        public void Arrange_Run_SimulatedAnnealing_KeepsLabelsOutOfBlockedRegion()
        {
            var leader = new GeoLine(0.0, 0.0, 40.0, 0.0);
            var blockPoly = new GeoPolygon(
                new GeoPoint(-60.0, 0.0),
                new GeoPoint(60.0, 0.0),
                new GeoPoint(60.0, 60.0),
                new GeoPoint(-60.0, 60.0));

            var a = new Arrange
            {
                GeoRectangle = new GeoRectangle(leader.MidPoint, 20.0, 10.0),
                GeoLine = leader,
                MarkOffsetFromLine = 5.0,
                BlockPolygons = new List<GeoPolygon> { blockPoly }
            };
            var b = new Arrange
            {
                GeoRectangle = new GeoRectangle(leader.MidPoint, 20.0, 10.0),
                GeoLine = leader,
                MarkOffsetFromLine = 5.0
            };

            var translations = Arrange.Run(new List<Arrange> { a, b }, new ArrangeOptions
            {
                Algorithm = ArrangeAlgorithmType.SimulatedAnnealing,
                RowGap = 5.0,
                PerpendicularLevels = 3
            });

            var movedA = new GeoRectangle(a.GeoRectangle.Center + translations[0], 20.0, 10.0);
            var movedB = new GeoRectangle(b.GeoRectangle.Center + translations[1], 20.0, 10.0);

            // The blocked region is collected globally for the list, so both labels must avoid it.
            Assert.False(movedA.IntersectsWith(blockPoly));
            Assert.False(movedB.IntersectsWith(blockPoly));
            Assert.False(movedA.IntersectsWith(movedB));
        }
    }
}

