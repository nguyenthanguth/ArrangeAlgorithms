using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using ArrangeAlgorithms;
using ArrangeAlgorithms.Geometry;

namespace ArrangeAlgorithms.UnitTest
{
    public class GreedyAlgorithmTests
    {
        /// <summary>Cấu hình dùng chung cho các phép thử bên dưới, kích thước nhỏ cho dễ tính tay.</summary>
        private static ArrangeOptions GreedyOptions(int perpendicularLevels = 3)
        {
            return new ArrangeOptions
            {
                Algorithm = ArrangeAlgorithmType.Greedy,
                RowGap = 5.0,
                PerpendicularLevels = perpendicularLevels
            };
        }

        /// <summary>Nhãn 20x10 đặt sẵn tại trung điểm đoạn dẫn.</summary>
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

            // Bất kể bị cấm, thuật toán Greedy phải lùi về phương án dự phòng (fallback) là candidate đầu tiên
            // thay vì đứng yên tại chỗ cũ gây bất định.
            Assert.False(arrange.Placed);
            Assert.NotEqual(GeoVector.Zero, translations[0]);
        }

        [Fact]
        public void Arrange_Run_Greedy_ReturnsTranslationsInInputOrder()
        {
            // Nhãn thứ hai bị vùng cấm bó hẹp nên được xử lý TRƯỚC nhãn thứ nhất.
            // Dù vậy kết quả trả về vẫn phải khớp đúng chỉ số đầu vào.
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

            // translations[0] phải là của nhãn bên trái, translations[1] của nhãn bên phải.
            Assert.Equal(20.0, MovedBox(near, translations[0]).Center.X, 6);
            Assert.Equal(1020.0, MovedBox(far, translations[1]).Center.X, 6);

            // Và nhãn bị cấm phía trên phải né xuống dưới.
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

            // Ô rỗng vẫn chiếm một chỗ trong kết quả để chỉ số không bị lệch.
            Assert.Equal(3, translations.Count);
            Assert.Equal(GeoVector.Zero, translations[1]);

            // Hai nhãn thật vẫn phải được sắp xếp bình thường.
            Assert.False(MovedBox(a, translations[0]).IntersectsWith(MovedBox(b, translations[2])));
            Assert.True(a.Placed);
            Assert.True(b.Placed);
        }

        [Fact]
        public void Arrange_Run_Greedy_AvoidsBlockLines()
        {
            var leader = new GeoLine(0.0, 0.0, 40.0, 0.0);
            var arrange = LabelOn(leader);

            // Đoạn thẳng cấm chắn ngang đúng chỗ ứng viên đầu tiên (phía trên đoạn dẫn).
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
            // Đoạn dẫn nghiêng 45 độ, nhãn xoay theo cho song song với nó.
            var leader = new GeoLine(0.0, 0.0, 40.0, 40.0);
            var box = new GeoRectangle(leader.MidPoint, 20.0, 10.0, Math.PI / 4.0);

            var a = new Arrange { GeoLine = leader, GeoRectangle = box, MarkOffsetFromLine = 5.0 };
            var b = new Arrange { GeoLine = leader, GeoRectangle = box, MarkOffsetFromLine = 5.0 };

            var translations = Arrange.Run(new List<Arrange> { a, b }, GreedyOptions());

            Assert.False(MovedBox(a, translations[0]).IntersectsWith(MovedBox(b, translations[1])));
            Assert.True(a.Placed);
            Assert.True(b.Placed);

            // Hai nhãn phải nằm hai bên đoạn dẫn, tức lệch nhau theo phương vuông góc.
            Assert.NotEqual(translations[0], translations[1]);
        }

        [Fact]
        public void Arrange_Run_Greedy_RepeatedObstacleOnEveryLabel_GivesSameResult()
        {
            // Cách dùng phổ biến là gán cùng một vùng cấm cho MỌI nhãn. Việc đó chỉ được làm
            // danh sách vật cản phình lên, tuyệt đối không được đổi kết quả sắp xếp.
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
            // Ba nhãn chung một đoạn dẫn ngắn nhưng chỉ có một cấp vuông góc, tức chỉ đủ chỗ cho hai.
            // Nhãn thứ ba buộc phải lùi về đè lên một nhãn đã đặt xong.
            var leader = new GeoLine(0.0, 0.0, 10.0, 0.0);
            var labels = new List<Arrange> { LabelOn(leader), LabelOn(leader), LabelOn(leader) };

            Arrange.Run(labels, GreedyOptions(perpendicularLevels: 1));

            // Nhãn BỊ ĐÈ cũng phải bị báo là thất bại, không chỉ riêng nhãn gây ra va chạm.
            // Trước đây nó vẫn giữ cờ thành công vì lúc đến lượt nó thì chỗ đó còn trống.
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

