using System;

namespace ArrangeAlgorithms.Geometry
{
    /// <summary>
    /// Biểu diễn một hình chữ nhật xoay (Oriented Bounding Box - OBB) 2D, có thể xoay.
    /// </summary>
    public readonly struct GeoRectangle : IEquatable<GeoRectangle>
    {
        /// <summary>
        /// Lấy điểm tâm của hình chữ nhật.
        /// </summary>
        public GeoPoint Center { get; }

        /// <summary>
        /// Lấy chiều rộng của hình chữ nhật (dọc theo trục X cục bộ của nó).
        /// </summary>
        public double Width { get; }

        /// <summary>
        /// Lấy chiều cao của hình chữ nhật (dọc theo trục Y cục bộ của nó).
        /// </summary>
        public double Height { get; }

        /// <summary>
        /// Lấy góc xoay theo radian (ngược chiều kim đồng hồ).
        /// </summary>
        public double AngleRad { get; }

        /// <summary>
        /// Lấy giá trị cho biết hình chữ nhật có đang xoay hay không (góc xoay khác không).
        /// </summary>
        public bool IsRotated => Math.Abs(AngleRad) > Tolerance.Global.EqualVector;

        /// <summary>
        /// Khởi tạo một thực thể GeoRectangle mới từ tâm, chiều rộng, chiều cao và góc xoay.
        /// </summary>
        /// <param name="center">Điểm tâm của hình chữ nhật.</param>
        /// <param name="width">Chiều rộng hình chữ nhật.</param>
        /// <param name="height">Chiều cao hình chữ nhật.</param>
        /// <param name="angleRad">Góc xoay theo radian.</param>
        public GeoRectangle(GeoPoint center, double width, double height, double angleRad = 0.0)
        {
            Center = center;
            Width = width;
            Height = height;
            AngleRad = angleRad;
        }

        /// <summary>
        /// Khởi tạo một thực thể GeoRectangle từ vị trí góc dưới-trái (khi chưa xoay) và kích thước.
        /// </summary>
        /// <param name="x">Tọa độ X của góc dưới-trái của hình chữ nhật khi chưa xoay.</param>
        /// <param name="y">Tọa độ Y của góc dưới-trái của hình chữ nhật khi chưa xoay.</param>
        /// <param name="width">Chiều rộng hình chữ nhật.</param>
        /// <param name="height">Chiều cao hình chữ nhật.</param>
        public GeoRectangle(double x, double y, double width, double height)
            : this(new GeoPoint(x + width * 0.5, y + height * 0.5), width, height, 0.0)
        {
        }

        /// <summary>
        /// Lấy tọa độ điểm góc dưới-trái (bottom-left).
        /// </summary>
        public GeoPoint LowerLeft
        {
            get
            {
                double cos = Math.Cos(AngleRad);
                double sin = Math.Sin(AngleRad);
                double halfW = Width * 0.5;
                double halfH = Height * 0.5;
                return new GeoPoint(
                    Center.X - halfW * cos + halfH * sin,
                    Center.Y - halfW * sin - halfH * cos);
            }
        }

        /// <summary>
        /// Lấy tọa độ điểm góc dưới-phải (bottom-right).
        /// </summary>
        public GeoPoint LowerRight
        {
            get
            {
                double cos = Math.Cos(AngleRad);
                double sin = Math.Sin(AngleRad);
                double halfW = Width * 0.5;
                double halfH = Height * 0.5;
                return new GeoPoint(
                    Center.X + halfW * cos + halfH * sin,
                    Center.Y + halfW * sin - halfH * cos);
            }
        }

        /// <summary>
        /// Lấy tọa độ điểm góc trên-trái (top-left).
        /// </summary>
        public GeoPoint UpperLeft
        {
            get
            {
                double cos = Math.Cos(AngleRad);
                double sin = Math.Sin(AngleRad);
                double halfW = Width * 0.5;
                double halfH = Height * 0.5;
                return new GeoPoint(
                    Center.X - halfW * cos - halfH * sin,
                    Center.Y - halfW * sin + halfH * cos);
            }
        }

        /// <summary>
        /// Lấy tọa độ điểm góc trên-phải (top-right).
        /// </summary>
        public GeoPoint UpperRight
        {
            get
            {
                double cos = Math.Cos(AngleRad);
                double sin = Math.Sin(AngleRad);
                double halfW = Width * 0.5;
                double halfH = Height * 0.5;
                return new GeoPoint(
                    Center.X + halfW * cos - halfH * sin,
                    Center.Y + halfW * sin + halfH * cos);
            }
        }

        /// <summary>
        /// Lấy danh sách 4 đỉnh của hình chữ nhật theo chiều ngược chiều kim đồng hồ: LowerLeft, LowerRight, UpperRight, UpperLeft.
        /// </summary>
        public GeoPoint[] GetVertices()
        {
            return new[] { LowerLeft, LowerRight, UpperRight, UpperLeft };
        }

        /// <summary>
        /// Lấy 4 cạnh khép kín của hình chữ nhật, nối lần lượt các đỉnh do <see cref="GetVertices"/> trả về.
        /// </summary>
        public GeoLine[] GetEdges()
        {
            GeoPoint[] v = GetVertices();
            return new[]
            {
                new GeoLine(v[0], v[1]),
                new GeoLine(v[1], v[2]),
                new GeoLine(v[2], v[3]),
                new GeoLine(v[3], v[0])
            };
        }

        /// <summary>
        /// Kiểm tra xem hình chữ nhật có chứa một điểm hay không.
        /// </summary>
        public bool Contains(GeoPoint GeoPoint)
        {
            double dx = GeoPoint.X - Center.X;
            double dy = GeoPoint.Y - Center.Y;

            double cos = Math.Cos(AngleRad);
            double sin = Math.Sin(AngleRad);

            // Chiếu điểm lên hệ tọa độ cục bộ quanh Center
            double localX = dx * cos + dy * sin;
            double localY = -dx * sin + dy * cos;

            double halfW = Width * 0.5;
            double halfH = Height * 0.5;

            return localX >= -halfW && localX <= halfW &&
                   localY >= -halfH && localY <= halfH;
        }

        /// <summary>
        /// Kiểm tra xem hình chữ nhật này có giao với hình chữ nhật khác hay không, sử dụng dung sai mặc định.
        /// </summary>
        public bool IntersectsWith(GeoRectangle other)
        {
            return IntersectsWith(other, Tolerance.Global);
        }

        /// <summary>
        /// Kiểm tra xem hình chữ nhật này có giao với hình chữ nhật khác hay không bằng thuật toán Separating Axis Theorem (SAT).
        /// Khe hở hẹp hơn dung sai vẫn được coi là hai hình chạm nhau.
        /// </summary>
        public bool IntersectsWith(GeoRectangle other, Tolerance tolerance)
        {
            GeoPoint[] r1 = GetVertices();
            GeoPoint[] r2 = other.GetVertices();

            // Các trục cần kiểm tra (vuông góc với các cạnh của cả hai hình chữ nhật)
            GeoVector[] axes = new GeoVector[4];
            axes[0] = new GeoVector(r1[1].X - r1[0].X, r1[1].Y - r1[0].Y).Normalize(); // Trục X của R1
            axes[1] = new GeoVector(r1[3].X - r1[0].X, r1[3].Y - r1[0].Y).Normalize(); // Trục Y của R1
            axes[2] = new GeoVector(r2[1].X - r2[0].X, r2[1].Y - r2[0].Y).Normalize(); // Trục X của R2
            axes[3] = new GeoVector(r2[3].X - r2[0].X, r2[3].Y - r2[0].Y).Normalize(); // Trục Y của R2

            foreach (var axis in axes)
            {
                if (axis.X == 0 && axis.Y == 0) continue;

                // Chiếu R1 lên trục
                double min1 = double.MaxValue;
                double max1 = double.MinValue;
                foreach (var p in r1)
                {
                    double proj = p.X * axis.X + p.Y * axis.Y;
                    if (proj < min1) min1 = proj;
                    if (proj > max1) max1 = proj;
                }

                // Chiếu R2 lên trục
                double min2 = double.MaxValue;
                double max2 = double.MinValue;
                foreach (var p in r2)
                {
                    double proj = p.X * axis.X + p.Y * axis.Y;
                    if (proj < min2) min2 = proj;
                    if (proj > max2) max2 = proj;
                }

                // Kiểm tra sự tách biệt trên trục này. Hai hình chỉ thực sự rời nhau khi khe hở
                // giữa hai hình chiếu vượt quá dung sai; hẹp hơn thế thì coi như chạm nhau.
                if (min2 - max1 > tolerance.EqualPoint || min1 - max2 > tolerance.EqualPoint)
                {
                    return false; // Tìm thấy trục phân tách, hai hình không giao nhau
                }
            }

            return true; // Không tìm thấy trục phân tách nào, hai hình giao nhau
        }

        /// <summary>
        /// Kiểm tra xem hình chữ nhật này có giao với một đoạn thẳng hay không, sử dụng dung sai mặc định.
        /// </summary>
        public bool IntersectsWith(GeoLine geoLine)
        {
            return IntersectsWith(geoLine, Tolerance.Global);
        }

        /// <summary>
        /// Kiểm tra xem hình chữ nhật này có giao với một đoạn thẳng hay không.
        /// </summary>
        public bool IntersectsWith(GeoLine geoLine, Tolerance tolerance)
        {
            // Nếu đoạn thẳng cắt bất kỳ cạnh nào của hình chữ nhật
            foreach (var edge in GetEdges())
            {
                if (geoLine.TryIntersectWith(edge, out _, tolerance))
                {
                    return true;
                }
            }

            // Hoặc nếu đoạn thẳng nằm hoàn toàn bên trong hình chữ nhật.
            // Chỉ cần xét điểm đầu: đoạn thẳng không cắt cạnh nào mà có một mút bên trong
            // thì toàn bộ đoạn nằm trong.
            if (Contains(geoLine.StartPoint))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Kiểm tra xem hình chữ nhật này có giao với một đa giác hay không, sử dụng dung sai mặc định.
        /// </summary>
        public bool IntersectsWith(GeoPolygon poly)
        {
            return IntersectsWith(poly, Tolerance.Global);
        }

        /// <summary>
        /// Kiểm tra xem hình chữ nhật này có giao với một đa giác hay không.
        /// </summary>
        public bool IntersectsWith(GeoPolygon poly, Tolerance tolerance)
        {
            if (poly == null) throw new ArgumentNullException(nameof(poly));

            return poly.IntersectsWith(this, tolerance);
        }

        /// <summary>
        /// Tính khoảng cách biên ngắn nhất từ hình chữ nhật này tới một đa giác.
        /// </summary>
        public double DistanceTo(GeoPolygon poly)
        {
            if (poly == null) throw new ArgumentNullException(nameof(poly));

            double minDistance = double.MaxValue;
            GeoPoint[] rectVertices = GetVertices();
            GeoLine[] rectEdges = GetEdges();

            foreach (var rv in rectVertices)
            {
                for (int i = 0; i < poly.EdgeCount; i++)
                {
                    double d = poly.GetEdgeAt(i).DistanceTo(rv);
                    if (d < minDistance) minDistance = d;
                }
            }

            for (int i = 0; i < poly.VertexCount; i++)
            {
                var pv = poly[i];
                foreach (var re in rectEdges)
                {
                    double d = re.DistanceTo(pv);
                    if (d < minDistance) minDistance = d;
                }
            }

            return minDistance;
        }

        /// <summary>
        /// Tính khoảng cách biên ngắn nhất từ hình chữ nhật này tới một đoạn thẳng.
        /// </summary>
        public double DistanceTo(GeoLine GeoLine)
        {
            double minDistance = double.MaxValue;
            GeoPoint[] rectVertices = GetVertices();
            GeoLine[] rectEdges = GetEdges();

            foreach (var rv in rectVertices)
            {
                double d = GeoLine.DistanceTo(rv);
                if (d < minDistance) minDistance = d;
            }

            foreach (var re in rectEdges)
            {
                double d1 = re.DistanceTo(GeoLine.StartPoint);
                double d2 = re.DistanceTo(GeoLine.EndPoint);
                if (d1 < minDistance) minDistance = d1;
                if (d2 < minDistance) minDistance = d2;
            }

            return minDistance;
        }

        /// <summary>
        /// Tính khoảng cách biên ngắn nhất từ hình chữ nhật này tới một hình chữ nhật khác.
        /// </summary>
        public double DistanceTo(GeoRectangle other)
        {
            double minDistance = double.MaxValue;
            GeoPoint[] r1Vertices = GetVertices();
            GeoPoint[] r2Vertices = other.GetVertices();
            GeoLine[] r1Edges = GetEdges();
            GeoLine[] r2Edges = other.GetEdges();

            foreach (var r1v in r1Vertices)
            {
                foreach (var r2e in r2Edges)
                {
                    double d = r2e.DistanceTo(r1v);
                    if (d < minDistance) minDistance = d;
                }
            }

            foreach (var r2v in r2Vertices)
            {
                foreach (var r1e in r1Edges)
                {
                    double d = r1e.DistanceTo(r2v);
                    if (d < minDistance) minDistance = d;
                }
            }

            return minDistance;
        }

        public bool Equals(GeoRectangle other)
        {
            return Center.Equals(other.Center) &&
                   Width.Equals(other.Width) &&
                   Height.Equals(other.Height) &&
                   AngleRad.Equals(other.AngleRad);
        }

        public override bool Equals(object obj)
        {
            return obj is GeoRectangle other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Center.GetHashCode();
                hash = (hash * 397) ^ Width.GetHashCode();
                hash = (hash * 397) ^ Height.GetHashCode();
                hash = (hash * 397) ^ AngleRad.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(GeoRectangle left, GeoRectangle right) => left.Equals(right);
        public static bool operator !=(GeoRectangle left, GeoRectangle right) => !left.Equals(right);

        public override string ToString()
        {
            return $"GeoRectangle[Center:{Center}, Width:{Width:0.000}, Height:{Height:0.000}, AngleRad:{AngleRad:0.000}]";
        }
    }
}

