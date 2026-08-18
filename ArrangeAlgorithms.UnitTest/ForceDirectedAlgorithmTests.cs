using ArrangeAlgorithms.Geometry;
using System.Collections.Generic;
using Xunit;

namespace ArrangeAlgorithms.UnitTest
{
    public class ForceDirectedAlgorithmTests
    {
        [Fact]
        public void Arrange_Run_ForceDirected_FindsSolution()
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
                Algorithm = ArrangeAlgorithmType.ForceDirected,
                RowGap = 5.0,
                PerpendicularLevels = 2,
                ForceIterations = 10
            };

            Arrange.Run(new List<Arrange> { a1, a2 }, options);

            var moved1 = new GeoRectangle(a1.GeoRectangle.Center + a1.TranslationVector, a1.GeoRectangle.Width, a1.GeoRectangle.Height);
            var moved2 = new GeoRectangle(a2.GeoRectangle.Center + a2.TranslationVector, a2.GeoRectangle.Width, a2.GeoRectangle.Height);

            Assert.False(moved1.IntersectsWith(moved2));
        }

        [Fact]
        public void Arrange_Run_ForceDirected_PushesLabelsAwayFromObstacleNotToward()
        {
            // Repulsive force from polygon obstacles was once inverted — it ATTRACTED labels to obstacles instead of repelling them.
            // The blocked region is completely above the guide segment, so the label must end up below it.
            var leader = new GeoLine(0.0, 0.0, 40.0, 0.0);
            var blockPoly = new GeoPolygon(
                new GeoPoint(-60.0, 2.0),
                new GeoPoint(60.0, 2.0),
                new GeoPoint(60.0, 60.0),
                new GeoPoint(-60.0, 60.0));

            var label = new Arrange
            {
                GeoRectangle = new GeoRectangle(leader.MidPoint, 20.0, 10.0),
                GeoLine = leader,
                MarkOffsetFromLine = 5.0,
                BlockPolygons = new List<GeoPolygon> { blockPoly }
            };

            Arrange.Run(new List<Arrange> { label }, new ArrangeOptions
            {
                Algorithm = ArrangeAlgorithmType.ForceDirected,
                RowGap = 5.0,
                PerpendicularLevels = 3,
                ForceIterations = 50
            });

            var moved = new GeoRectangle(label.GeoRectangle.Center + label.TranslationVector, 20.0, 10.0);

            Assert.True(moved.Center.Y < 0.0);
            Assert.False(moved.IntersectsWith(blockPoly));
            Assert.True(label.Placed);
        }

        [Fact]
        public void Arrange_Run_ForceDirected_ZeroIterations_StillProducesValidLayout()
        {
            // Even with zero simulation iterations, the discrete mapping step must still yield a valid layout.
            var leader = new GeoLine(0.0, 0.0, 40.0, 0.0);
            var a = new Arrange { GeoRectangle = new GeoRectangle(leader.MidPoint, 20.0, 10.0), GeoLine = leader, MarkOffsetFromLine = 5.0 };
            var b = new Arrange { GeoRectangle = new GeoRectangle(leader.MidPoint, 20.0, 10.0), GeoLine = leader, MarkOffsetFromLine = 5.0 };

            Arrange.Run(new List<Arrange> { a, b }, new ArrangeOptions
            {
                Algorithm = ArrangeAlgorithmType.ForceDirected,
                RowGap = 5.0,
                PerpendicularLevels = 3,
                ForceIterations = 0
            });

            var movedA = new GeoRectangle(a.GeoRectangle.Center + a.TranslationVector, 20.0, 10.0);
            var movedB = new GeoRectangle(b.GeoRectangle.Center + b.TranslationVector, 20.0, 10.0);

            Assert.False(movedA.IntersectsWith(movedB));
            Assert.True(a.Placed);
            Assert.True(b.Placed);
        }

        [Fact]
        public void Arrange_Run_ForceDirected_SpreadsManyLabelsSharingOneLeader()
        {
            var leader = new GeoLine(0.0, 0.0, 200.0, 0.0);
            var labels = new List<Arrange>();
            for (int i = 0; i < 4; i++)
            {
                labels.Add(new Arrange
                {
                    GeoRectangle = new GeoRectangle(leader.MidPoint, 20.0, 10.0),
                    GeoLine = leader,
                    MarkOffsetFromLine = 5.0
                });
            }

            Arrange.Run(labels, new ArrangeOptions
            {
                Algorithm = ArrangeAlgorithmType.ForceDirected,
                RowGap = 5.0,
                PerpendicularLevels = 3,
                ForceIterations = 60
            });

            var boxes = new List<GeoRectangle>();
            for (int i = 0; i < labels.Count; i++)
            {
                boxes.Add(new GeoRectangle(labels[i].GeoRectangle.Center + labels[i].TranslationVector, 20.0, 10.0));
            }

            for (int i = 0; i < boxes.Count; i++)
            {
                for (int j = i + 1; j < boxes.Count; j++)
                {
                    Assert.False(boxes[i].IntersectsWith(boxes[j]));
                }
            }
        }

        [Fact]
        public void Arrange_Run_ForceDirected_RespectsLineObstacles()
        {
            var leader = new GeoLine(0.0, 0.0, 40.0, 0.0);
            var blockLine = new GeoLine(-50.0, 10.0, 150.0, 10.0); // Blocks the first upper level

            var label = new Arrange
            {
                GeoRectangle = new GeoRectangle(leader.MidPoint, 20.0, 10.0),
                GeoLine = leader,
                MarkOffsetFromLine = 5.0,
                BlockLines = new List<GeoLine> { blockLine }
            };

            Arrange.Run(new List<Arrange> { label }, new ArrangeOptions
            {
                Algorithm = ArrangeAlgorithmType.ForceDirected,
                RowGap = 5.0,
                PerpendicularLevels = 3,
                ForceIterations = 30
            });

            var moved = new GeoRectangle(label.GeoRectangle.Center + label.TranslationVector, 20.0, 10.0);

            // Force-directed algorithm must resolve placing layout without intersecting the line obstacle.
            Assert.False(moved.IntersectsWith(blockLine));
            Assert.True(label.Placed);
        }

        [Fact]
        public void Arrange_Run_ForceDirected_HighIterations_DoesNotCrash()
        {
            var leader = new GeoLine(0.0, 0.0, 40.0, 0.0);
            var label = new Arrange
            {
                GeoRectangle = new GeoRectangle(leader.MidPoint, 20.0, 10.0),
                GeoLine = leader,
                MarkOffsetFromLine = 5.0
            };

            var options = new ArrangeOptions
            {
                Algorithm = ArrangeAlgorithmType.ForceDirected,
                ForceIterations = 500, // Very high iteration simulation
                RowGap = 5.0,
                PerpendicularLevels = 3
            };

            // Checking CPU performance and calculation safety under high loop counts.
            Arrange.Run(new List<Arrange> { label }, options);

            Assert.True(label.Placed);
        }
    }
}
