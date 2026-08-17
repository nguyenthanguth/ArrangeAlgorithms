using ArrangeAlgorithms.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ArrangeAlgorithms.UnitTest
{
    /// <summary>
    /// Kiểm thử API công khai của <see cref="Arrange"/> và các hợp đồng mà MỌI thuật toán đều phải giữ.
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
        // Kiểm tra tham số đầu vào
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
            // Nhãn không đặt MarkOffsetFromLine nên nó giữ giá trị mặc định 50 của chính Arrange.
            var label = new Arrange
            {
                GeoLine = new GeoLine(0.0, 0.0, 400.0, 0.0),
                GeoRectangle = new GeoRectangle(new GeoPoint(200.0, 0.0), 20.0, 10.0)
            };

            var translations = Arrange.Run(new List<Arrange> { label });

            Assert.Single(translations);

            // BaseOffset mặc định = nửa chiều cao (5) + MarkOffsetFromLine (50) = 55.
            Assert.Equal(55.0, Math.Abs(MovedBox(label, translations[0]).Center.Y), 6);
        }

        // ------------------------------------------------------------------
        // Sinh vị trí ứng viên
        // ------------------------------------------------------------------

        [Fact]
        public void Arrange_GetPlacePoints_FirstPairIsSymmetricAtBaseOffset()
        {
            var label = LabelOn(new GeoLine(0.0, 0.0, 40.0, 0.0));
            var options = OptionsFor(ArrangeAlgorithmType.Greedy);

            var points = label.GetPlacePoints(options);

            // BaseOffset = nửa chiều cao nhãn (5) + MarkOffsetFromLine (5) = 10, đo từ trung điểm đoạn dẫn (20, 0).
            Assert.True(points[0].IsEqualTo(new GeoPoint(20.0, 10.0)));
            Assert.True(points[1].IsEqualTo(new GeoPoint(20.0, -10.0)));
        }

        [Fact]
        public void Arrange_MarkOffsetFromLine_IsPerLabelNotGlobal()
        {
            // Khoảng hở nằm trên từng nhãn, nên hai nhãn dùng chung một ArrangeOptions vẫn phải
            // loang ứng viên từ hai cấp vuông góc khác nhau.
            var leader = new GeoLine(0.0, 0.0, 40.0, 0.0);
            var near = LabelOn(leader);                       // MarkOffsetFromLine = 5
            var far = LabelOn(leader);
            far.MarkOffsetFromLine = 30.0;

            var options = OptionsFor(ArrangeAlgorithmType.Greedy);

            // BaseOffset = nửa chiều cao nhãn (5) cộng khoảng hở riêng của chính nhãn đó.
            Assert.True(near.GetPlacePoints(options)[0].IsEqualTo(new GeoPoint(20.0, 10.0)));
            Assert.True(far.GetPlacePoints(options)[0].IsEqualTo(new GeoPoint(20.0, 35.0)));
        }

        [Fact]
        public void Arrange_Run_HonoursEachLabelsOwnMarkOffsetFromLine()
        {
            // Cùng một đoạn dẫn, cùng một bộ tùy chọn: nhãn khai báo khoảng hở lớn hơn phải
            // dừng xa đoạn dẫn hơn, và cả hai đều nằm đúng cấp vuông góc đầu tiên của mình.
            var leader = new GeoLine(0.0, 0.0, 40.0, 0.0);
            var near = LabelOn(leader);                       // MarkOffsetFromLine = 5
            var far = LabelOn(leader);
            far.MarkOffsetFromLine = 30.0;

            var translations = Arrange.Run(new List<Arrange> { near, far }, OptionsFor(ArrangeAlgorithmType.Greedy));

            Assert.Equal(10.0, Math.Abs(MovedBox(near, translations[0]).Center.Y), 6);
            Assert.Equal(35.0, Math.Abs(MovedBox(far, translations[1]).Center.Y), 6);
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
            Assert.Equal(3 * oneLevel.Count, threeLevels.Count);
        }

        [Fact]
        public void Arrange_GetPlacePoints_MaximumCandidates_CapsLongitudinalSliding()
        {
            var label = LabelOn(new GeoLine(0.0, 0.0, 40.0, 0.0));
            var options = OptionsFor(ArrangeAlgorithmType.Greedy);
            options.MaximumCandidates = 1;

            var points = label.GetPlacePoints(options);

            // Trần này chỉ chặn vòng trượt dọc, không chặn hai vị trí vuông góc thuần của mỗi cấp.
            // Với 3 cấp thì luôn còn đúng 2 x 3 = 6 ứng viên — đủ để vòng lặp không thể chạy vô hạn.
            Assert.Equal(6, points.Count);
        }

        [Fact]
        public void Arrange_GetPlacePoints_DegenerateLeader_ProducesNoCandidates()
        {
            // Đoạn dẫn dài bằng 0 thì không có hướng, không thể suy ra trục để loang ứng viên.
            var label = new Arrange
            {
                GeoLine = new GeoLine(5.0, 0.0, 5.0, 0.0),
                GeoRectangle = new GeoRectangle(new GeoPoint(5.0, 0.0), 20.0, 10.0),
                MarkOffsetFromLine = 5.0
            };

            Assert.Empty(label.GetPlacePoints(OptionsFor(ArrangeAlgorithmType.Greedy)));
        }

        [Fact]
        public void Arrange_GetPlacePoints_BoxAtMinimumSize_IsStillValid()
        {
            // Tài liệu nói nhãn NHỎ HƠN ngưỡng mới bị loại, nên nhãn đúng bằng ngưỡng phải hợp lệ.
            var label = new Arrange
            {
                GeoLine = new GeoLine(0.0, 0.0, 40.0, 0.0),
                GeoRectangle = new GeoRectangle(new GeoPoint(20.0, 0.0), 10.0, 10.0),
                MarkOffsetFromLine = 5.0
            };

            var options = OptionsFor(ArrangeAlgorithmType.Greedy);
            options.MinimumBoxSize = 10.0;

            Assert.NotEmpty(label.GetPlacePoints(options));
        }

        // ------------------------------------------------------------------
        // Kích thước nhãn không hợp lệ
        // ------------------------------------------------------------------

        [Fact]
        public void Arrange_InvalidBoxSize_ReturnsZeroTranslations()
        {
            var leaderLine = new GeoLine(0.0, 0.0, 10.0, 0.0);
            var a1 = new Arrange
            {
                GeoRectangle = new GeoRectangle(new GeoPoint(5.0, 0.0), 2.0, 2.0), // Quá nhỏ
                GeoLine = leaderLine
            };

            var options = new ArrangeOptions
            {
                MinimumBoxSize = 5.0
            };

            var translations = Arrange.Run(new List<Arrange> { a1 }, options);
            Assert.Equal(GeoVector.Zero, translations[0]);
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

            var translations = Arrange.Run(new List<Arrange> { label }, OptionsFor(ArrangeAlgorithmType.Greedy));

            Assert.Equal(GeoVector.Zero, translations[0]);

            // Nhãn không đè ai cả, nhưng nó cũng chưa từng được sắp xếp nên không được báo thành công.
            Assert.False(label.Placed);
        }

        [Fact]
        public void Arrange_LabelAlreadyOnFirstCandidate_ProducesZeroTranslation()
        {
            // Nhãn đã nằm sẵn đúng vị trí ứng viên đầu tiên: không có gì để dịch chuyển,
            // và MinimumMoveDistance phải nuốt luôn mọi sai số nhỏ.
            var leader = new GeoLine(0.0, 0.0, 40.0, 0.0);
            var label = new Arrange
            {
                GeoLine = leader,
                GeoRectangle = new GeoRectangle(new GeoPoint(20.0, 10.0), 20.0, 10.0),
                MarkOffsetFromLine = 5.0
            };

            var translations = Arrange.Run(new List<Arrange> { label }, OptionsFor(ArrangeAlgorithmType.Greedy));

            Assert.Equal(GeoVector.Zero, translations[0]);
            Assert.True(label.Placed);
        }

        // ------------------------------------------------------------------
        // Hợp đồng chung cho cả năm thuật toán
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
        public void Arrange_Run_ReturnsOneTranslationPerLabel(ArrangeAlgorithmType algorithm)
        {
            var labels = new List<Arrange>();
            for (int i = 0; i < 5; i++)
            {
                labels.Add(LabelOn(new GeoLine(i * 50.0, 0.0, i * 50.0 + 40.0, 0.0)));
            }

            var translations = Arrange.Run(labels, OptionsFor(algorithm));

            Assert.Equal(labels.Count, translations.Count);
        }

        [Theory]
        [MemberData(nameof(AllAlgorithms))]
        public void Arrange_Run_WithEmptyList_ReturnsEmptyResult(ArrangeAlgorithmType algorithm)
        {
            var translations = Arrange.Run(new List<Arrange>(), OptionsFor(algorithm));

            Assert.Empty(translations);
        }

        [Theory]
        [MemberData(nameof(AllAlgorithms))]
        public void Arrange_Run_WithNullEntries_KeepsIndicesAligned(ArrangeAlgorithmType algorithm)
        {
            var first = LabelOn(new GeoLine(0.0, 0.0, 40.0, 0.0));
            var second = LabelOn(new GeoLine(200.0, 0.0, 240.0, 0.0));

            var translations = Arrange.Run(new List<Arrange> { first, null, second }, OptionsFor(algorithm));

            Assert.Equal(3, translations.Count);
            Assert.Equal(GeoVector.Zero, translations[1]);

            // Mỗi nhãn thật vẫn phải bám đoạn dẫn của chính nó, không bị hoán chỉ số.
            // Không so tọa độ tuyệt đối: Force-directed cố ý trượt nhãn dọc đoạn dẫn để giãn chúng ra,
            // nên tiêu chí đúng là "gần đoạn dẫn của mình hơn là gần đoạn dẫn của nhãn kia".
            GeoPoint firstCentre = MovedBox(first, translations[0]).Center;
            GeoPoint secondCentre = MovedBox(second, translations[2]).Center;

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

            var translations = Arrange.Run(new List<Arrange> { a, b }, OptionsFor(algorithm));

            Assert.False(MovedBox(a, translations[0]).IntersectsWith(MovedBox(b, translations[1])));
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

            var translations = Arrange.Run(new List<Arrange> { label }, OptionsFor(algorithm));
            var moved = MovedBox(label, translations[0]);

            Assert.False(moved.IntersectsWith(blockPoly));
            Assert.True(moved.Center.Y < 0.0);
            Assert.True(label.Placed);
        }

        [Theory]
        [MemberData(nameof(AllAlgorithms))]
        public void Arrange_Run_IsReproducible(ArrangeAlgorithmType algorithm)
        {
            List<GeoVector> Solve()
            {
                var labels = new List<Arrange>();
                for (int i = 0; i < 6; i++)
                {
                    labels.Add(LabelOn(new GeoLine(i * 12.0, 0.0, i * 12.0 + 30.0, 0.0)));
                }
                return Arrange.Run(labels, OptionsFor(algorithm));
            }

            var first = Solve();
            var second = Solve();

            Assert.Equal(first.Count, second.Count);
            for (int i = 0; i < first.Count; i++)
            {
                Assert.Equal(first[i], second[i]);
            }
        }

        [Theory]
        [MemberData(nameof(AllAlgorithms))]
        public void Arrange_Run_PlacedFlagMatchesFinalLayout(ArrangeAlgorithmType algorithm)
        {
            // Cờ Placed phải mô tả bố cục CUỐI CÙNG. Ba nhãn chung một đoạn dẫn ngắn với một cấp
            // vuông góc thì chỉ đủ chỗ cho hai, nên số nhãn báo thành công phải khớp đúng số nhãn
            // thực sự không đè ai — kể cả nhãn bị nhãn khác lùi vào đè lên.
            var leader = new GeoLine(0.0, 0.0, 10.0, 0.0);
            var labels = new List<Arrange> { LabelOn(leader), LabelOn(leader), LabelOn(leader) };

            var options = OptionsFor(algorithm);
            options.PerpendicularLevels = 1;

            var translations = Arrange.Run(labels, options);

            var boxes = labels.Select((label, i) => MovedBox(label, translations[i])).ToList();
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
