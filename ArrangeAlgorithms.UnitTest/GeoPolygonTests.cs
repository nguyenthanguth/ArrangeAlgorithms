using System;
using Xunit;
using ArrangeAlgorithms.Geometry;

namespace ArrangeAlgorithms.UnitTest
{
    public class PolygonTests
    {
        [Fact]
        public void Polygon_WindingAndArea_WorkCorrectly()
        {
            // Tam giác ngược chiều kim đồng hồ (CCW)
            var p1 = new GeoPoint(0.0, 0.0);
            var p2 = new GeoPoint(4.0, 0.0);
            var p3 = new GeoPoint(0.0, 3.0);
            var poly = new GeoPolygon(p1, p2, p3);

            Assert.Equal(3, poly.VertexCount);
            Assert.Equal(3, poly.EdgeCount);
            Assert.Equal(6.0, poly.GetArea(), 12);
            Assert.Equal(6.0, poly.GetSignedArea(), 12);
            Assert.False(poly.IsClockwise());

            // Tam giác cùng chiều kim đồng hồ (CW)
            var polyCW = new GeoPolygon(p1, p3, p2);
            Assert.Equal(6.0, polyCW.GetArea(), 12);
            Assert.Equal(-6.0, polyCW.GetSignedArea(), 12);
            Assert.True(polyCW.IsClockwise());
        }

        [Fact]
        public void Polygon_Centroid_WorksCorrectly()
        {
            var p1 = new GeoPoint(0.0, 0.0);
            var p2 = new GeoPoint(6.0, 0.0);
            var p3 = new GeoPoint(0.0, 6.0);
            var poly = new GeoPolygon(p1, p2, p3);

            // Trọng tâm tam giác vuông này nằm ở (2, 2)
            Assert.True(poly.GetCentroid().IsEqualTo(new GeoPoint(2.0, 2.0)));
        }

        [Fact]
        public void Polygon_ContainsPoint_WorksCorrectly()
        {
            var poly = new GeoPolygon(
                new GeoPoint(0.0, 0.0),
                new GeoPoint(4.0, 0.0),
                new GeoPoint(4.0, 4.0),
                new GeoPoint(0.0, 4.0)
            ); // Hình vuông 4x4

            Assert.True(poly.Contains(new GeoPoint(2.0, 2.0)));   // Điểm bên trong
            Assert.True(poly.Contains(new GeoPoint(0.0, 2.0)));   // Điểm trên biên
            Assert.False(poly.Contains(new GeoPoint(5.0, 2.0)));  // Điểm ngoài
        }

        [Fact]
        public void Polygon_IntersectsWithRectangle_WorksCorrectly()
        {
            var poly = new GeoPolygon(
                new GeoPoint(0.0, 0.0),
                new GeoPoint(4.0, 0.0),
                new GeoPoint(4.0, 4.0),
                new GeoPoint(0.0, 4.0)
            ); // Hình vuông 4x4

            // 1. Cắt chéo nhau
            var rect1 = new GeoRectangle(new GeoPoint(4.0, 4.0), 2.0, 2.0, Math.PI / 4.0); // Tâm ở đỉnh hình vuông, xoay 45 độ
            Assert.True(poly.IntersectsWith(rect1));

            // 2. Chứa trọn hình chữ nhật
            var rect2 = new GeoRectangle(new GeoPoint(2.0, 2.0), 1.0, 1.0, 0.1);
            Assert.True(poly.IntersectsWith(rect2));

            // 3. Tách biệt hoàn toàn
            var rect3 = new GeoRectangle(new GeoPoint(8.0, 8.0), 2.0, 2.0, 0.5);
            Assert.False(poly.IntersectsWith(rect3));
        }

        [Fact]
        public void Polygon_IntersectsWithLine_WorksCorrectly()
        {
            var poly = new GeoPolygon(
                new GeoPoint(0.0, 0.0),
                new GeoPoint(4.0, 0.0),
                new GeoPoint(4.0, 4.0),
                new GeoPoint(0.0, 4.0)
            ); // Hình vuông 4x4

            // 1. Đoạn thẳng xuyên qua hình vuông
            Assert.True(poly.IntersectsWith(new GeoLine(-2.0, 2.0, 6.0, 2.0)));

            // 2. Đoạn thẳng nằm trọn bên trong (không cắt cạnh nào)
            Assert.True(poly.IntersectsWith(new GeoLine(1.0, 1.0, 3.0, 3.0)));

            // 3. Đoạn thẳng chạm biên
            Assert.True(poly.IntersectsWith(new GeoLine(-2.0, 4.0, 2.0, 4.0)));

            // 4. Tách biệt hoàn toàn
            Assert.False(poly.IntersectsWith(new GeoLine(6.0, 6.0, 8.0, 8.0)));
        }

        [Fact]
        public void Polygon_IntersectsWithPolygon_WorksCorrectly()
        {
            var poly = new GeoPolygon(
                new GeoPoint(0.0, 0.0),
                new GeoPoint(4.0, 0.0),
                new GeoPoint(4.0, 4.0),
                new GeoPoint(0.0, 4.0)
            ); // Hình vuông 4x4

            // 1. Chồng lấn một phần
            var overlapping = new GeoPolygon(
                new GeoPoint(2.0, 2.0),
                new GeoPoint(6.0, 2.0),
                new GeoPoint(6.0, 6.0),
                new GeoPoint(2.0, 6.0)
            );
            Assert.True(poly.IntersectsWith(overlapping));

            // 2. Nằm lọt hẳn bên trong: không cạnh nào cắt nhau, phải nhận ra qua phép bao chứa
            var inner = new GeoPolygon(
                new GeoPoint(1.0, 1.0),
                new GeoPoint(3.0, 1.0),
                new GeoPoint(3.0, 3.0),
                new GeoPoint(1.0, 3.0)
            );
            Assert.True(poly.IntersectsWith(inner));

            // 3. Quan hệ bao chứa phải đối xứng theo cả hai chiều gọi
            Assert.True(inner.IntersectsWith(poly));

            // 4. Tách biệt hoàn toàn
            var far = new GeoPolygon(
                new GeoPoint(10.0, 10.0),
                new GeoPoint(12.0, 10.0),
                new GeoPoint(12.0, 12.0)
            );
            Assert.False(poly.IntersectsWith(far));
        }

        [Fact]
        public void Polygon_Constructor_RejectsDegeneratePolygon()
        {
            // Đa giác chỉ có 2 đỉnh là suy biến. Hàm dựng chặn ngay từ đầu thay vì tạo ra một đối tượng
            // không thể trả lời Contains một cách có nghĩa, nên phép thử đúng là kiểm tra ngoại lệ.
            Assert.Throws<ArgumentException>(
                () => new GeoPolygon(new GeoPoint(0.0, 0.0), new GeoPoint(1.0, 1.0)));
        }

        [Fact]
        public void Polygon_CentroidConcavePolygon_WorksCorrectly()
        {
            // Đa giác hình chữ L (lõm)
            var poly = new GeoPolygon(
                new GeoPoint(0.0, 0.0),
                new GeoPoint(4.0, 0.0),
                new GeoPoint(4.0, 2.0),
                new GeoPoint(2.0, 2.0),
                new GeoPoint(2.0, 4.0),
                new GeoPoint(0.0, 4.0)
            );

            // Công thức diện tích và trọng tâm đa giác tự do bằng giải thuật Shoelace / Green Theorem
            // Diện tích = 12
            Assert.Equal(12.0, poly.GetArea(), 12);
            
            // Ghép từ hai hình chữ nhật: [0,4]x[0,2] diện tích 8 tâm (2,1) và [0,2]x[2,4] diện tích 4 tâm (1,3).
            // Trọng tâm = (8*2 + 4*1)/12 = 5/3 theo X, (8*1 + 4*3)/12 = 5/3 theo Y.
            Assert.True(poly.GetCentroid().IsEqualTo(new GeoPoint(5.0 / 3.0, 5.0 / 3.0)));
        }

        [Fact]
        public void Polygon_ContainsPointConcavePolygon_WorksCorrectly()
        {
            // Đa giác hình chữ L (lõm)
            var poly = new GeoPolygon(
                new GeoPoint(0.0, 0.0),
                new GeoPoint(4.0, 0.0),
                new GeoPoint(4.0, 2.0),
                new GeoPoint(2.0, 2.0),
                new GeoPoint(2.0, 4.0),
                new GeoPoint(0.0, 4.0)
            );

            // Điểm (3, 3) nằm ngoài đa giác, mặc dù nó nằm trong bounding box [0, 4] x [0, 4] của đa giác
            Assert.False(poly.Contains(new GeoPoint(3.0, 3.0)));

            // Điểm (1, 3) nằm trong phần nhánh dọc của L
            Assert.True(poly.Contains(new GeoPoint(1.0, 3.0)));

            // Điểm (3, 1) nằm trong phần nhánh ngang của L
            Assert.True(poly.Contains(new GeoPoint(3.0, 1.0)));
        }

        [Fact]
        public void Polygon_ZeroAreaCollinear_ReturnsCorrectArea()
        {
            // 3 điểm thẳng hàng -> Đa giác dẹt diện tích = 0
            var poly = new GeoPolygon(
                new GeoPoint(0.0, 0.0),
                new GeoPoint(5.0, 5.0),
                new GeoPoint(10.0, 10.0)
            );

            Assert.Equal(0.0, poly.GetArea(), 12);
            Assert.Equal(0.0, poly.GetSignedArea(), 12);
        }
    }
}

