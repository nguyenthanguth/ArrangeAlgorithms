using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using ArrangeAlgorithms;
using ArrangeAlgorithms.Geometry;

namespace ArrangeAlgorithms.UnitTest
{
    public class BoundedBacktrackingAlgorithmTests
    {
        [Fact]
        public void Arrange_Run_BoundedBacktracking_FindsSolution()
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
                Algorithm = ArrangeAlgorithmType.BoundedBacktracking,
                RowGap = 5.0,
                PerpendicularLevels = 2
            };

            var translations = Arrange.Run(new List<Arrange> { a1, a2 }, options);

            Assert.Equal(2, translations.Count);
            var moved1 = new GeoRectangle(a1.GeoRectangle.Center + translations[0], a1.GeoRectangle.Width, a1.GeoRectangle.Height);
            var moved2 = new GeoRectangle(a2.GeoRectangle.Center + translations[1], a2.GeoRectangle.Width, a2.GeoRectangle.Height);

            Assert.False(moved1.IntersectsWith(moved2));
            Assert.True(a1.Placed);
            Assert.True(a2.Placed);
        }

        [Fact]
        public void Arrange_Run_BoundedBacktracking_ReturnsFalseWhenFullyBlocked()
        {
            var leaderLine = new GeoLine(0.0, 0.0, 10.0, 0.0);
            var a1 = new Arrange
            {
                GeoRectangle = new GeoRectangle(new GeoPoint(5.0, 0.0), 20.0, 10.0),
                GeoLine = leaderLine,
                MarkOffsetFromLine = 5.0
            };

            // Đa giác cấm cực lớn bao trùm toàn bộ không gian ứng viên
            var blockPoly = new GeoPolygon(
                new GeoPoint(-100.0, -100.0),
                new GeoPoint(100.0, -100.0),
                new GeoPoint(100.0, 100.0),
                new GeoPoint(-100.0, 100.0)
            );
            a1.BlockPolygons = new List<GeoPolygon> { blockPoly };

            var options = new ArrangeOptions
            {
                Algorithm = ArrangeAlgorithmType.BoundedBacktracking,
                RowGap = 5.0,
                PerpendicularLevels = 2
            };

            var translations = Arrange.Run(new List<Arrange> { a1 }, options);

            // Bounded Backtracking khi bị cấm hoàn toàn sẽ trả về Placed = false
            Assert.False(a1.Placed);
        }

        [Fact]
        public void Arrange_Run_BoundedBacktracking_PrefersCandidateNearestTheLeader()
        {
            // Chỗ này từng xếp ứng viên theo khoảng hở GIẢM dần, tức luôn chọn vị trí XA nhất và
            // ném mọi nhãn ra cấp vuông góc ngoài cùng dù chỗ gần vẫn trống.
            var leader = new GeoLine(0.0, 0.0, 40.0, 0.0);
            var label = new Arrange
            {
                GeoRectangle = new GeoRectangle(leader.MidPoint, 20.0, 10.0),
                GeoLine = leader,
                MarkOffsetFromLine = 5.0
            };

            var options = new ArrangeOptions
            {
                Algorithm = ArrangeAlgorithmType.BoundedBacktracking,
                RowGap = 5.0,
                PerpendicularLevels = 3
            };

            var translations = Arrange.Run(new List<Arrange> { label }, options);
            var moved = new GeoRectangle(label.GeoRectangle.Center + translations[0], 20.0, 10.0);

            // Chỉ có một nhãn nên nó phải nằm ở cấp gần nhất: BaseOffset = 5 + 5 = 10.
            // Cấp ngoài cùng sẽ cho |Y| = 10 + 2 * (10 + 5) = 40.
            Assert.Equal(10.0, Math.Abs(moved.Center.Y), 6);
            Assert.True(label.Placed);
        }

        [Fact]
        public void Arrange_Run_BoundedBacktracking_FallsBackToGreedyWhenNoCleanSolutionExists()
        {
            // Bốn nhãn chung một đoạn dẫn ngắn, chỉ một cấp vuông góc: không tồn tại lời giải toàn vẹn.
            // Thuật toán phải lùi về Greedy chứ không được ném lỗi hay bỏ nhãn lại chỗ cũ.
            var leader = new GeoLine(0.0, 0.0, 10.0, 0.0);
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

            var options = new ArrangeOptions
            {
                Algorithm = ArrangeAlgorithmType.BoundedBacktracking,
                RowGap = 5.0,
                PerpendicularLevels = 1
            };

            var translations = Arrange.Run(labels, options);

            Assert.Equal(4, translations.Count);

            // Mọi nhãn đều phải rời khỏi đoạn dẫn, kể cả những nhãn không tìm được chỗ sạch.
            foreach (var translation in translations)
            {
                Assert.NotEqual(GeoVector.Zero, translation);
            }

            // Và ít nhất hai nhãn phải đặt được sạch (hai bên đoạn dẫn).
            Assert.True(labels.Count(x => x.Placed) >= 2);
        }

        [Fact]
        public void Arrange_Run_BoundedBacktracking_RespectsMaxBacktrackSteps()
        {
            // Ngân sách bằng 0 buộc thuật toán bỏ cuộc ngay lập tức và lùi về Greedy,
            // nhưng kết quả trả về vẫn phải hợp lệ chứ không được rỗng hay ném lỗi.
            var leader = new GeoLine(0.0, 0.0, 40.0, 0.0);
            var a = new Arrange { GeoRectangle = new GeoRectangle(leader.MidPoint, 20.0, 10.0), GeoLine = leader, MarkOffsetFromLine = 5.0 };
            var b = new Arrange { GeoRectangle = new GeoRectangle(leader.MidPoint, 20.0, 10.0), GeoLine = leader, MarkOffsetFromLine = 5.0 };

            var options = new ArrangeOptions
            {
                Algorithm = ArrangeAlgorithmType.BoundedBacktracking,
                RowGap = 5.0,
                PerpendicularLevels = 3,
                MaxBacktrackSteps = 0
            };

            var translations = Arrange.Run(new List<Arrange> { a, b }, options);

            Assert.Equal(2, translations.Count);

            var movedA = new GeoRectangle(a.GeoRectangle.Center + translations[0], 20.0, 10.0);
            var movedB = new GeoRectangle(b.GeoRectangle.Center + translations[1], 20.0, 10.0);
            Assert.False(movedA.IntersectsWith(movedB));
        }
    }
}

