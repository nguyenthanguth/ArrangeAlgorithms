using System;
using System.Collections.Generic;
using Xunit;
using ArrangeAlgorithms;
using ArrangeAlgorithms.Geometry;

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
                GeoLine = leaderLine
            };
            var a2 = new Arrange
            {
                GeoRectangle = new GeoRectangle(new GeoPoint(5.0, 0.0), 20.0, 10.0),
                GeoLine = leaderLine
            };

            var options = new ArrangeOptions
            {
                Algorithm = ArrangeAlgorithmType.ForceDirected,
                MarkOffsetFromLine = 5.0,
                RowGap = 5.0,
                PerpendicularLevels = 2,
                ForceIterations = 10
            };

            var translations = Arrange.Run(new List<Arrange> { a1, a2 }, options);

            Assert.Equal(2, translations.Count);
            var moved1 = new GeoRectangle(a1.GeoRectangle.Center + translations[0], a1.GeoRectangle.Width, a1.GeoRectangle.Height);
            var moved2 = new GeoRectangle(a2.GeoRectangle.Center + translations[1], a2.GeoRectangle.Width, a2.GeoRectangle.Height);

            Assert.False(moved1.IntersectsWith(moved2));
        }

        [Fact]
        public void Arrange_Run_ForceDirected_PushesLabelsAwayFromObstacleNotToward()
        {
            // Lực từ vật cản đa giác từng bị đảo dấu — nó HÚT nhãn vào vật cản thay vì đẩy ra.
            // Vùng cấm nằm hẳn phía trên đoạn dẫn, nên nhãn buộc phải kết thúc ở phía dưới.
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
                BlockPolygons = new List<GeoPolygon> { blockPoly }
            };

            var translations = Arrange.Run(new List<Arrange> { label }, new ArrangeOptions
            {
                Algorithm = ArrangeAlgorithmType.ForceDirected,
                MarkOffsetFromLine = 5.0,
                RowGap = 5.0,
                PerpendicularLevels = 3,
                ForceIterations = 50
            });

            var moved = new GeoRectangle(label.GeoRectangle.Center + translations[0], 20.0, 10.0);

            Assert.True(moved.Center.Y < 0.0);
            Assert.False(moved.IntersectsWith(blockPoly));
            Assert.True(label.Placed);
        }

        [Fact]
        public void Arrange_Run_ForceDirected_ZeroIterations_StillProducesValidLayout()
        {
            // Không chạy vòng mô phỏng nào thì bước ánh xạ rời rạc vẫn phải cho kết quả hợp lệ.
            var leader = new GeoLine(0.0, 0.0, 40.0, 0.0);
            var a = new Arrange { GeoRectangle = new GeoRectangle(leader.MidPoint, 20.0, 10.0), GeoLine = leader };
            var b = new Arrange { GeoRectangle = new GeoRectangle(leader.MidPoint, 20.0, 10.0), GeoLine = leader };

            var translations = Arrange.Run(new List<Arrange> { a, b }, new ArrangeOptions
            {
                Algorithm = ArrangeAlgorithmType.ForceDirected,
                MarkOffsetFromLine = 5.0,
                RowGap = 5.0,
                PerpendicularLevels = 3,
                ForceIterations = 0
            });

            var movedA = new GeoRectangle(a.GeoRectangle.Center + translations[0], 20.0, 10.0);
            var movedB = new GeoRectangle(b.GeoRectangle.Center + translations[1], 20.0, 10.0);

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
                    GeoLine = leader
                });
            }

            var translations = Arrange.Run(labels, new ArrangeOptions
            {
                Algorithm = ArrangeAlgorithmType.ForceDirected,
                MarkOffsetFromLine = 5.0,
                RowGap = 5.0,
                PerpendicularLevels = 3,
                ForceIterations = 60
            });

            var boxes = new List<GeoRectangle>();
            for (int i = 0; i < labels.Count; i++)
            {
                boxes.Add(new GeoRectangle(labels[i].GeoRectangle.Center + translations[i], 20.0, 10.0));
            }

            for (int i = 0; i < boxes.Count; i++)
            {
                for (int j = i + 1; j < boxes.Count; j++)
                {
                    Assert.False(boxes[i].IntersectsWith(boxes[j]));
                }
            }
        }
    }
}

