using System;
using Xunit;
using ArrangeAlgorithms.Geometry;

namespace ArrangeAlgorithms.UnitTest
{
    public class LineTests
    {
        [Fact]
        public void Line_CreationAndProperties_WorkCorrectly()
        {
            var p1 = new GeoPoint(1.0, 1.0);
            var p2 = new GeoPoint(4.0, 5.0);
            var GeoLine = new GeoLine(p1, p2);

            Assert.Equal(p1, GeoLine.StartPoint);
            Assert.Equal(p2, GeoLine.EndPoint);
            Assert.Equal(5.0, GeoLine.Length, 12);
            Assert.Equal(25.0, GeoLine.LengthSquared, 12);
            Assert.Equal(new GeoPoint(2.5, 3.0), GeoLine.MidPoint);
            Assert.Equal(new GeoVector(3.0, 4.0), GeoLine.Direction);
        }

        [Fact]
        public void Line_ProjectionAndClosestPoint_WorkCorrectly()
        {
            var GeoLine = new GeoLine(0.0, 0.0, 10.0, 0.0); // Nằm ngang trên trục X

            // Điểm trên đoạn thẳng
            Assert.Equal(0.5, GeoLine.GetParameterOf(new GeoPoint(5.0, 0.0)), 12);
            Assert.Equal(new GeoPoint(5.0, 0.0), GeoLine.GetClosestPointTo(new GeoPoint(5.0, 5.0)));
            Assert.Equal(5.0, GeoLine.DistanceTo(new GeoPoint(5.0, 5.0)), 12);

            // Điểm ngoài khoảng đầu mút
            Assert.Equal(-0.2, GeoLine.GetParameterOf(new GeoPoint(-2.0, 0.0)), 12);
            Assert.Equal(GeoLine.StartPoint, GeoLine.GetClosestPointTo(new GeoPoint(-2.0, 5.0)));
            
            Assert.True(GeoLine.IsPointOn(new GeoPoint(3.0, 0.0)));
            Assert.False(GeoLine.IsPointOn(new GeoPoint(3.0, 0.1)));
        }

        [Fact]
        public void Line_Intersection_WorksCorrectly()
        {
            var l1 = new GeoLine(0.0, 0.0, 10.0, 10.0);
            var l2 = new GeoLine(0.0, 10.0, 10.0, 0.0);

            // Cắt nhau tại tâm (5, 5)
            Assert.True(l1.TryIntersectWith(l2, out var hit));
            Assert.True(hit.IsEqualTo(new GeoPoint(5.0, 5.0)));

            // Song song
            var l3 = new GeoLine(0.0, 2.0, 10.0, 12.0);
            Assert.False(l1.TryIntersectWith(l3, out _));

            // Không cắt nhau trực tiếp (chỉ cắt trên đường kéo dài)
            var l4 = new GeoLine(20.0, 10.0, 30.0, 0.0);
            Assert.False(l1.TryIntersectWith(l4, out _));
        }

        [Fact]
        public void Line_Direction_ReturnsZeroForDegenerateLine()
        {
            var GeoLine = new GeoLine(1.0, 1.0, 1.0, 1.0); // Điểm đầu trùng điểm cuối
            Assert.Equal(GeoVector.Zero, GeoLine.Direction);
            Assert.Equal(0.0, GeoLine.Length);
            Assert.False(GeoLine.Direction.TryGetNormal(out _));
        }

        [Fact]
        public void Line_CollinearAndOverlappingIntersection_WorkCorrectly()
        {
            // Hai đoạn thẳng cùng nằm trên 1 đường thẳng
            var l1 = new GeoLine(0.0, 0.0, 5.0, 0.0);
            
            // Trường hợp 1: Nằm trên đường thẳng kéo dài nhưng không chạm nhau
            var l2 = new GeoLine(10.0, 0.0, 15.0, 0.0);
            Assert.False(l1.TryIntersectWith(l2, out _));

            // Trường hợp 2: Chồng lấn một phần (Collinear Overlap)
            var l3 = new GeoLine(3.0, 0.0, 8.0, 0.0);
            // Thuật toán hình học cơ bản của AutoCAD/thư viện thường trả về false cho collinear overlap
            // vì có vô số giao điểm, ta kiểm thử hành vi thực tế của thư viện:
            Assert.False(l1.TryIntersectWith(l3, out _));
        }

        [Fact]
        public void Line_PointProjectionEdgeCases_WorkCorrectly()
        {
            var GeoLine = new GeoLine(0.0, 0.0, 10.0, 0.0);

            // Chi chiếu điểm rơi chính xác vào điểm đầu (parameter t = 0)
            Assert.Equal(0.0, GeoLine.GetParameterOf(new GeoPoint(0.0, 5.0)), 12);
            Assert.Equal(GeoLine.StartPoint, GeoLine.GetClosestPointTo(new GeoPoint(0.0, 5.0)));

            // Chi chiếu điểm rơi chính xác vào điểm cuối (parameter t = 1)
            Assert.Equal(1.0, GeoLine.GetParameterOf(new GeoPoint(10.0, -5.0)), 12);
            Assert.Equal(GeoLine.EndPoint, GeoLine.GetClosestPointTo(new GeoPoint(10.0, -5.0)));
        }

        [Fact]
        public void Line_TJunctionIntersection_WorksCorrectly()
        {
            var l1 = new GeoLine(0.0, 0.0, 10.0, 0.0);
            
            // Một đoạn thẳng vuông góc và chạm chính xác vào điểm mút
            var l2 = new GeoLine(5.0, 0.0, 5.0, 5.0);
            Assert.True(l1.TryIntersectWith(l2, out var hit));
            Assert.True(hit.IsEqualTo(new GeoPoint(5.0, 0.0)));
        }

        [Fact]
        public void Line_IntersectsWithLine_WorksCorrectly()
        {
            var l1 = new GeoLine(0.0, 0.0, 10.0, 0.0);

            Assert.True(l1.IntersectsWith(new GeoLine(5.0, -5.0, 5.0, 5.0)));   // Cắt nhau
            Assert.False(l1.IntersectsWith(new GeoLine(0.0, 5.0, 10.0, 5.0)));  // Song song
            Assert.False(l1.IntersectsWith(new GeoLine(20.0, -5.0, 20.0, 5.0))); // Cắt phần kéo dài, không cắt đoạn
        }

        [Fact]
        public void Line_IntersectsWithRectangleAndPolygon_WorksCorrectly()
        {
            var rect = new GeoRectangle(new GeoPoint(0.0, 0.0), 4.0, 4.0);
            var poly = new GeoPolygon(
                new GeoPoint(-2.0, -2.0),
                new GeoPoint(2.0, -2.0),
                new GeoPoint(2.0, 2.0),
                new GeoPoint(-2.0, 2.0)
            );

            var crossing = new GeoLine(-5.0, 0.0, 5.0, 0.0);
            var outside = new GeoLine(10.0, 10.0, 12.0, 12.0);

            Assert.True(crossing.IntersectsWith(rect));
            Assert.False(outside.IntersectsWith(rect));

            Assert.True(crossing.IntersectsWith(poly));
            Assert.False(outside.IntersectsWith(poly));

            // Gọi theo chiều ngược lại phải cho cùng kết quả
            Assert.True(rect.IntersectsWith(crossing));
            Assert.True(poly.IntersectsWith(crossing));
        }
    }
}

