using System;
using System.Collections.Generic;
using Xunit;
using ArrangeAlgorithms;
using ArrangeAlgorithms.Geometry;

namespace ArrangeAlgorithms.UnitTest
{
    public class GreedyAlgorithmTests
    {
        [Fact]
        public void Arrange_Run_Greedy_ArrangesNonOverlappingLabels()
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

            var list = new List<Arrange> { a1, a2 };
            var options = new ArrangeOptions
            {
                Algorithm = ArrangeAlgorithmType.Greedy,
                MarkOffsetFromLine = 5.0,
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
            // Đoạn dẫn nằm ngang ngay dưới nhãn. Nó phải có độ dài thực: đoạn dẫn suy biến
            // không có hướng nên không sinh được vị trí ứng viên nào.
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
                BlockPolygons = new List<GeoPolygon> { blockPoly }
            };

            var options = new ArrangeOptions
            {
                Algorithm = ArrangeAlgorithmType.Greedy,
                MarkOffsetFromLine = 5.0,
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
            // Nhãn xuất phát ở xa cụm ứng viên. Nếu đặt nó trùng luôn ứng viên đầu tiên thì
            // phép lùi về ứng viên đó cho ra vector không, và phép thử mất hết ý nghĩa.
            var rect = new GeoRectangle(new GeoPoint(5.0, 40.0), 20.0, 10.0);

            // Đa giác cấm cực lớn bao trùm toàn bộ không gian xung quanh vùng nhãn
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
                BlockPolygons = new List<GeoPolygon> { blockPoly }
            };

            var options = new ArrangeOptions
            {
                Algorithm = ArrangeAlgorithmType.Greedy,
                MarkOffsetFromLine = 5.0,
                RowGap = 5.0,
                PerpendicularLevels = 2
            };

            var translations = Arrange.Run(new List<Arrange> { arrange }, options);

            // Bất kể bị cấm, thuật toán Greedy phải lùi về phương án dự phòng (fallback) là candidate đầu tiên
            // thay vì đứng yên tại chỗ cũ gây bất định.
            Assert.False(arrange.Placed);
            Assert.NotEqual(GeoVector.Zero, translations[0]);
        }
    }
}

