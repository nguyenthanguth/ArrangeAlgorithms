using ArrangeAlgorithms.Geometry;
using System;
using Xunit;

namespace ArrangeAlgorithms.UnitTest
{
    public class RectangleTests
    {
        [Fact]
        public void Rectangle_CreationAndProperties_WorkCorrectly()
        {
            // Hình chữ nhật thẳng đứng (AABB) biểu diễn bằng Center
            var rect1 = new GeoRectangle(0.0, 0.0, 4.0, 6.0);
            Assert.Equal(new GeoPoint(2.0, 3.0), rect1.Center);
            Assert.Equal(4.0, rect1.Width);
            Assert.Equal(6.0, rect1.Height);
            Assert.Equal(0.0, rect1.AngleRad);
            Assert.False(rect1.IsRotated);

            Assert.Equal(new GeoPoint(0.0, 0.0), rect1.LowerLeft);
            Assert.Equal(new GeoPoint(4.0, 0.0), rect1.LowerRight);
            Assert.Equal(new GeoPoint(0.0, 6.0), rect1.UpperLeft);
            Assert.Equal(new GeoPoint(4.0, 6.0), rect1.UpperRight);

            // Hình chữ nhật xoay (OBB)
            var center = new GeoPoint(0.0, 0.0);
            var rect2 = new GeoRectangle(center, 4.0, 2.0, Math.PI / 2.0); // Rộng 4 dọc theo trục X cục bộ (trục Y), cao 2 dọc theo trục Y cục bộ (-X)
            Assert.True(rect2.IsRotated);

            // Xoay 90 độ ngược chiều kim đồng hồ biến đổi toạ độ cục bộ theo (x, y) -> (-y, x).
            // LowerLeft cục bộ là (-halfW, -halfH) = (-2, -1) -> (1.0, -2.0)
            Assert.True(rect2.LowerLeft.IsEqualTo(new GeoPoint(1.0, -2.0)));
            // LowerRight cục bộ là (halfW, -halfH) = (2, -1) -> (1.0, 2.0)
            Assert.True(rect2.LowerRight.IsEqualTo(new GeoPoint(1.0, 2.0)));
            // UpperRight cục bộ là (halfW, halfH) = (2, 1) -> (-1.0, 2.0)
            Assert.True(rect2.UpperRight.IsEqualTo(new GeoPoint(-1.0, 2.0)));
            // UpperLeft cục bộ là (-halfW, halfH) = (-2, 1) -> (-1.0, -2.0)
            Assert.True(rect2.UpperLeft.IsEqualTo(new GeoPoint(-1.0, -2.0)));
        }

        [Fact]
        public void Rectangle_ContainsPoint_WorksCorrectly()
        {
            var center = new GeoPoint(0.0, 0.0);
            var rect = new GeoRectangle(center, 4.0, 2.0, Math.PI / 4.0); // Xoay 45 độ

            // Tâm chắc chắn nằm trong
            Assert.True(rect.Contains(center));

            // Điểm (0, 0.9) - xoay 45 độ, trục X cục bộ là 0.9*sin(45) ~ 0.63, trục Y cục bộ là 0.9*cos(45) ~ 0.63.
            // HalfWidth = 2.0, HalfHeight = 1.0. Cả hai 0.63 đều nằm trong giới hạn.
            Assert.True(rect.Contains(new GeoPoint(0.0, 0.9)));

            // Điểm (2, 0) - khi chưa xoay nằm ngay trên biên, nhưng xoay 45 độ làm nó rơi ra ngoài (khoảng cách 2 > HalfHeight của trục xoay)
            // Chiếu cục bộ: X = 2*cos(45) ~ 1.41 (<= 2.0), Y = -2*sin(45) ~ -1.41 (không thuộc [-1.0, 1.0])
            Assert.False(rect.Contains(new GeoPoint(2.0, 0.0)));
        }

        [Fact]
        public void Rectangle_IntersectsWith_WorksCorrectly_SAT()
        {
            var center1 = new GeoPoint(0.0, 0.0);
            var rect1 = new GeoRectangle(center1, 4.0, 2.0, 0.0);

            // 1. Hai AABB hoàn toàn không giao nhau
            var rect2 = new GeoRectangle(new GeoPoint(5.0, 0.0), 2.0, 2.0, 0.0);
            Assert.False(rect1.IntersectsWith(rect2));

            // 2. Hai AABB giao nhau
            var rect3 = new GeoRectangle(new GeoPoint(3.0, 0.0), 3.0, 2.0, 0.0);
            Assert.True(rect1.IntersectsWith(rect3));

            // 3. Hai AABB lồng nhau
            var rect4 = new GeoRectangle(center1, 1.0, 1.0, 0.0);
            Assert.True(rect1.IntersectsWith(rect4));

            // 4. Hai OBB xoay cắt chéo nhau
            var rect5 = new GeoRectangle(new GeoPoint(2.5, 1.5), 2.0, 2.0, Math.PI / 4.0); // Xoay 45 độ
            Assert.True(rect1.IntersectsWith(rect5));

            // 5. Hai OBB xoay không cắt nhau
            var rect6 = new GeoRectangle(new GeoPoint(4.0, 3.0), 2.0, 2.0, Math.PI / 4.0);
            Assert.False(rect1.IntersectsWith(rect6));
        }

        [Fact]
        public void Rectangle_ContainsPointOnBoundary_WorksCorrectly()
        {
            var center = new GeoPoint(0.0, 0.0);
            var rect = new GeoRectangle(center, 4.0, 2.0, 0.0);

            // Điểm nằm chính xác trên đỉnh góc
            Assert.True(rect.Contains(new GeoPoint(2.0, 1.0)));

            // Điểm nằm chính xác trên cạnh bên
            Assert.True(rect.Contains(new GeoPoint(2.0, 0.5)));
        }

        [Fact]
        public void Rectangle_IntersectsWithContactOnly_WorksCorrectly()
        {
            var rect1 = new GeoRectangle(0.0, 0.0, 4.0, 4.0); // X: [0, 4], Y: [0, 4]

            // Tiếp xúc ngoài chạm cạnh
            var rect2 = new GeoRectangle(4.0, 0.0, 4.0, 4.0); // X: [4, 8], Y: [0, 4]
            Assert.True(rect1.IntersectsWith(rect2));

            // Tiếp xúc ngoài chạm đúng góc
            var rect3 = new GeoRectangle(4.0, 4.0, 4.0, 4.0); // X: [4, 8], Y: [4, 8]
            Assert.True(rect1.IntersectsWith(rect3));
        }

        [Fact]
        public void Rectangle_IntersectsWithExtremeAspectRatios_WorksCorrectly()
        {
            // Một hình chữ nhật siêu mảnh (dạng thanh dài)
            var rectThin = new GeoRectangle(new GeoPoint(0.0, 0.0), 10.0, 0.0001, Math.PI / 4.0);
            var rectTarget = new GeoRectangle(new GeoPoint(1.0, 1.0), 2.0, 2.0, 0.0);

            Assert.True(rectThin.IntersectsWith(rectTarget));
        }

        [Fact]
        public void Rectangle_IntersectsWithLine_WorksCorrectly()
        {
            var rect = new GeoRectangle(new GeoPoint(0.0, 0.0), 4.0, 4.0); // X: [-2, 2], Y: [-2, 2]

            Assert.True(rect.IntersectsWith(new GeoLine(-5.0, 0.0, 5.0, 0.0)));   // Xuyên qua
            Assert.True(rect.IntersectsWith(new GeoLine(-1.0, -1.0, 1.0, 1.0)));  // Nằm trọn bên trong
            Assert.False(rect.IntersectsWith(new GeoLine(-5.0, 5.0, 5.0, 5.0)));  // Đi ngang phía trên, không chạm
        }

        [Fact]
        public void Rectangle_IntersectsWithPolygon_WorksCorrectly()
        {
            var rect = new GeoRectangle(new GeoPoint(0.0, 0.0), 4.0, 4.0); // X: [-2, 2], Y: [-2, 2]

            var overlapping = new GeoPolygon(
                new GeoPoint(1.0, 1.0),
                new GeoPoint(5.0, 1.0),
                new GeoPoint(5.0, 5.0),
                new GeoPoint(1.0, 5.0)
            );
            var far = new GeoPolygon(
                new GeoPoint(10.0, 10.0),
                new GeoPoint(12.0, 10.0),
                new GeoPoint(12.0, 12.0)
            );

            Assert.True(rect.IntersectsWith(overlapping));
            Assert.False(rect.IntersectsWith(far));

            // Gọi theo chiều ngược lại phải cho cùng kết quả
            Assert.True(overlapping.IntersectsWith(rect));
            Assert.False(far.IntersectsWith(rect));
        }

        [Fact]
        public void Rectangle_CornerConstructor_MatchesCentreConstructor()
        {
            var fromCorner = new GeoRectangle(10.0, 20.0, 4.0, 6.0);
            var fromCentre = new GeoRectangle(new GeoPoint(12.0, 23.0), 4.0, 6.0);

            Assert.Equal(fromCentre, fromCorner);
            Assert.Equal(0.0, fromCorner.AngleRad);
            Assert.False(fromCorner.IsRotated);
        }

        [Fact]
        public void Rectangle_GetEdges_ConnectsVerticesInOrder()
        {
            var rect = new GeoRectangle(new GeoPoint(0.0, 0.0), 4.0, 2.0, 0.6);

            var vertices = rect.GetVertices();
            var edges = rect.GetEdges();

            Assert.Equal(4, edges.Length);
            for (int i = 0; i < 4; i++)
            {
                Assert.Equal(vertices[i], edges[i].StartPoint);
                Assert.Equal(vertices[(i + 1) % 4], edges[i].EndPoint);
            }
        }

        [Fact]
        public void Rectangle_Equality_AndHashCode_WorkCorrectly()
        {
            var a = new GeoRectangle(new GeoPoint(1.0, 2.0), 4.0, 2.0, 0.5);
            var b = new GeoRectangle(new GeoPoint(1.0, 2.0), 4.0, 2.0, 0.5);
            var rotatedDifferently = new GeoRectangle(new GeoPoint(1.0, 2.0), 4.0, 2.0, 0.6);

            Assert.True(a == b);
            Assert.False(a != b);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
            Assert.True(a.Equals((object)b));

            object notARectangle = "không phải GeoRectangle";
            Assert.False(a.Equals(notARectangle));
            Assert.True(a != rotatedDifferently);
        }

        [Fact]
        public void Rectangle_IsRotated_IgnoresNegligibleAngles()
        {
            Assert.False(new GeoRectangle(new GeoPoint(0.0, 0.0), 4.0, 2.0, 0.0).IsRotated);
            Assert.False(new GeoRectangle(new GeoPoint(0.0, 0.0), 4.0, 2.0, 1e-9).IsRotated);
            Assert.True(new GeoRectangle(new GeoPoint(0.0, 0.0), 4.0, 2.0, 0.01).IsRotated);
        }

        [Fact]
        public void Rectangle_DistanceTo_IsZeroWhenTouchingAndPositiveWhenApart()
        {
            var rect = new GeoRectangle(new GeoPoint(0.0, 0.0), 4.0, 2.0); // X: [-2, 2], Y: [-1, 1]

            // Chạm cạnh: khoảng cách biên-biên bằng 0.
            var touching = new GeoRectangle(new GeoPoint(4.0, 0.0), 4.0, 2.0);
            Assert.Equal(0.0, rect.DistanceTo(touching), 9);

            // Cách nhau 3 đơn vị theo trục X.
            var apart = new GeoRectangle(new GeoPoint(7.0, 0.0), 4.0, 2.0);
            Assert.Equal(3.0, rect.DistanceTo(apart), 9);

            // Tới đoạn thẳng nằm ngang phía trên, cách mép trên 4 đơn vị.
            Assert.Equal(4.0, rect.DistanceTo(new GeoLine(-5.0, 5.0, 5.0, 5.0)), 9);

            // Tới đa giác nằm bên phải, cách mép phải 3 đơn vị.
            var poly = new GeoPolygon(
                new GeoPoint(5.0, -1.0),
                new GeoPoint(9.0, -1.0),
                new GeoPoint(9.0, 1.0),
                new GeoPoint(5.0, 1.0));
            Assert.Equal(3.0, rect.DistanceTo(poly), 9);
        }

        [Fact]
        public void Rectangle_DistanceTo_NullPolygon_Throws()
        {
            var rect = new GeoRectangle(new GeoPoint(0.0, 0.0), 4.0, 2.0);

            Assert.Throws<ArgumentNullException>(() => rect.DistanceTo((GeoPolygon)null));
            Assert.Throws<ArgumentNullException>(() => rect.IntersectsWith((GeoPolygon)null));
        }

        [Fact]
        public void Rectangle_IntersectsWith_NarrowGapCountsAsTouchingWithinTolerance()
        {
            var left = new GeoRectangle(new GeoPoint(0.0, 0.0), 4.0, 2.0);   // X tới 2.0

            // Khe hở hẹp hơn dung sai thì coi như chạm nhau.
            var almostTouching = new GeoRectangle(new GeoPoint(4.0 + 1e-6, 0.0), 4.0, 2.0);
            Assert.True(left.IntersectsWith(almostTouching));

            // Khe hở rộng hơn dung sai thì tách hẳn.
            var clearlyApart = new GeoRectangle(new GeoPoint(4.1, 0.0), 4.0, 2.0);
            Assert.False(left.IntersectsWith(clearlyApart));
        }

        [Fact]
        public void Rectangle_GetVertices_PreservesSizeUnderRotation()
        {
            var rect = new GeoRectangle(new GeoPoint(3.0, -2.0), 6.0, 4.0, 1.1);
            var vertices = rect.GetVertices();

            // Cạnh kề nhau phải giữ đúng chiều rộng và chiều cao dù hình đã xoay.
            Assert.Equal(6.0, vertices[0].DistanceTo(vertices[1]), 9);
            Assert.Equal(4.0, vertices[1].DistanceTo(vertices[2]), 9);
            Assert.Equal(6.0, vertices[2].DistanceTo(vertices[3]), 9);
            Assert.Equal(4.0, vertices[3].DistanceTo(vertices[0]), 9);

            // Tâm hình vẫn là trung điểm của hai đường chéo.
            Assert.True(vertices[0].GetMiddlePoint(vertices[2]).IsEqualTo(rect.Center));
            Assert.True(vertices[1].GetMiddlePoint(vertices[3]).IsEqualTo(rect.Center));
        }
    }
}

