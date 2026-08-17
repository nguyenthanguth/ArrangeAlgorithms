using System;
using System.Collections.Generic;
using System.Linq;

namespace ArrangeAlgorithms.Geometry
{
    /// <summary>
    /// Biểu diễn một đa giác đơn khép kín 2D.
    /// </summary>
    public sealed class GeoPolygon : IEquatable<GeoPolygon>
    {
        private readonly GeoPoint[] _vertices;

        /// <summary>
        /// Lấy danh sách đỉnh chỉ đọc của đa giác.
        /// </summary>
        public IReadOnlyList<GeoPoint> Vertices => _vertices;

        /// <summary>
        /// Lấy số lượng đỉnh của đa giác.
        /// </summary>
        public int VertexCount => _vertices.Length;

        /// <summary>
        /// Lấy số lượng cạnh của đa giác.
        /// </summary>
        public int EdgeCount => _vertices.Length;

        /// <summary>
        /// Khởi tạo một đa giác mới từ danh sách đỉnh. Đỉnh trùng lặp ở cuối để khép kín vòng lặp sẽ tự động được lược bỏ.
        /// </summary>
        /// <param name="vertices">Danh sách các đỉnh.</param>
        public GeoPolygon(IEnumerable<GeoPoint> vertices)
        {
            if (vertices == null) throw new ArgumentNullException(nameof(vertices));

            List<GeoPoint> list = vertices.ToList();
            // Lược bỏ đỉnh cuối nếu trùng với đỉnh đầu tiên
            while (list.Count > 1 && list[list.Count - 1].Equals(list[0]))
            {
                list.RemoveAt(list.Count - 1);
            }

            if (list.Count < 3)
            {
                throw new ArgumentException("Một đa giác phải có ít nhất 3 đỉnh phân biệt.");
            }

            _vertices = list.ToArray();
        }

        /// <summary>
        /// Khởi tạo một đa giác mới trực tiếp từ các đỉnh truyền vào dưới dạng tham số.
        /// </summary>
        public GeoPolygon(params GeoPoint[] vertices) : this((IEnumerable<GeoPoint>)vertices)
        {
        }

        /// <summary>
        /// Lấy đỉnh tại một chỉ số cho trước.
        /// </summary>
        public GeoPoint this[int index]
        {
            get
            {
                if (index < 0 || index >= _vertices.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                return _vertices[index];
            }
        }

        /// <summary>
        /// Lấy đoạn thẳng cạnh của đa giác tại chỉ số cho trước.
        /// </summary>
        public GeoLine GetEdgeAt(int index)
        {
            if (index < 0 || index >= EdgeCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            return new GeoLine(_vertices[index], _vertices[(index + 1) % _vertices.Length]);
        }

        /// <summary>
        /// Liệt kê tất cả các cạnh của đa giác.
        /// </summary>
        public IEnumerable<GeoLine> GetEdges()
        {
            for (int i = 0; i < EdgeCount; i++)
            {
                yield return GetEdgeAt(i);
            }
        }

        /// <summary>
        /// Tính diện tích có dấu của đa giác bằng công thức Shoelace.
        /// Dương nếu các đỉnh đi theo chiều ngược kim đồng hồ (CCW), âm nếu đi theo chiều kim đồng hồ (CW).
        /// </summary>
        public double GetSignedArea()
        {
            double area = 0.0;
            int n = _vertices.Length;
            for (int i = 0; i < n; i++)
            {
                GeoPoint p1 = _vertices[i];
                GeoPoint p2 = _vertices[(i + 1) % n];
                area += p1.X * p2.Y - p2.X * p1.Y;
            }
            return area * 0.5;
        }

        /// <summary>
        /// Tính diện tích tuyệt đối của đa giác.
        /// </summary>
        public double GetArea() => Math.Abs(GetSignedArea());

        /// <summary>
        /// Kiểm tra xem đa giác được định hướng theo chiều kim đồng hồ hay không.
        /// </summary>
        public bool IsClockwise() => GetSignedArea() < 0.0;

        /// <summary>
        /// Tính trọng tâm hình học của đa giác.
        /// </summary>
        public GeoPoint GetCentroid()
        {
            double signedArea = GetSignedArea();
            if (Math.Abs(signedArea) <= Tolerance.Global.EqualPoint * Tolerance.Global.EqualPoint)
            {
                // Dự phòng: Lấy trung bình cộng tọa độ các đỉnh nếu đa giác bị suy biến
                double sumX = 0.0;
                double sumY = 0.0;
                foreach (var v in _vertices)
                {
                    sumX += v.X;
                    sumY += v.Y;
                }
                return new GeoPoint(sumX / _vertices.Length, sumY / _vertices.Length);
            }

            double cx = 0.0;
            double cy = 0.0;
            int n = _vertices.Length;
            for (int i = 0; i < n; i++)
            {
                GeoPoint p1 = _vertices[i];
                GeoPoint p2 = _vertices[(i + 1) % n];
                double factor = p1.X * p2.Y - p2.X * p1.Y;
                cx += (p1.X + p2.X) * factor;
                cy += (p1.Y + p2.Y) * factor;
            }

            double areaFactor = 1.0 / (6.0 * signedArea);
            return new GeoPoint(cx * areaFactor, cy * areaFactor);
        }

        /// <summary>
        /// Kiểm tra xem đa giác có chứa một điểm hay không sử dụng dung sai mặc định (chấp nhận điểm nằm trên biên).
        /// </summary>
        public bool Contains(GeoPoint GeoPoint)
        {
            return Contains(GeoPoint, Tolerance.Global);
        }

        /// <summary>
        /// Kiểm tra xem đa giác có chứa một điểm hay không (chấp nhận điểm nằm trên biên).
        /// Sử dụng thuật toán Ray Casting (bắn tia ngang và đếm số giao điểm).
        /// </summary>
        public bool Contains(GeoPoint GeoPoint, Tolerance tolerance)
        {
            // Kiểm tra điểm trên biên đa giác trước
            for (int i = 0; i < EdgeCount; i++)
            {
                if (GetEdgeAt(i).IsPointOn(GeoPoint, tolerance))
                {
                    return true;
                }
            }

            bool inside = false;
            int n = _vertices.Length;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                GeoPoint p1 = _vertices[i];
                GeoPoint p2 = _vertices[j];

                if (((p1.Y > GeoPoint.Y) != (p2.Y > GeoPoint.Y)) &&
                    (GeoPoint.X < (p2.X - p1.X) * (GeoPoint.Y - p1.Y) / (p2.Y - p1.Y) + p1.X))
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        /// <summary>
        /// Kiểm tra xem đa giác có giao với một hình chữ nhật xoay (GeoRectangle OBB) sử dụng dung sai mặc định.
        /// </summary>
        public bool IntersectsWith(GeoRectangle rect)
        {
            return IntersectsWith(rect, Tolerance.Global);
        }

        /// <summary>
        /// Kiểm tra xem đa giác có giao với một hình chữ nhật xoay (GeoRectangle OBB) hay không.
        /// </summary>
        public bool IntersectsWith(GeoRectangle rect, Tolerance tolerance)
        {
            // 1. Kiểm tra xem có bất kỳ cạnh nào của đa giác cắt cạnh của hình chữ nhật hay không
            GeoLine[] rectEdges = rect.GetEdges();

            foreach (var polyEdge in GetEdges())
            {
                foreach (var rectEdge in rectEdges)
                {
                    if (polyEdge.TryIntersectWith(rectEdge, out _, tolerance))
                    {
                        return true;
                    }
                }
            }

            // 2. Kiểm tra xem đa giác có nằm hoàn toàn bên trong hình chữ nhật hay không
            if (rect.Contains(_vertices[0]))
            {
                return true;
            }

            // 3. Kiểm tra xem hình chữ nhật có nằm hoàn toàn bên trong đa giác hay không
            if (Contains(rect.Center, tolerance))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Kiểm tra xem đa giác có giao với một đoạn thẳng hay không, sử dụng dung sai mặc định.
        /// </summary>
        public bool IntersectsWith(GeoLine geoLine)
        {
            return IntersectsWith(geoLine, Tolerance.Global);
        }

        /// <summary>
        /// Kiểm tra xem đa giác có giao với một đoạn thẳng hay không.
        /// </summary>
        public bool IntersectsWith(GeoLine geoLine, Tolerance tolerance)
        {
            // 1. Kiểm tra xem đoạn thẳng có cắt bất kỳ cạnh nào của đa giác hay không
            foreach (var polyEdge in GetEdges())
            {
                if (geoLine.TryIntersectWith(polyEdge, out _, tolerance))
                {
                    return true;
                }
            }

            // 2. Hoặc đoạn thẳng nằm hoàn toàn bên trong đa giác.
            // Chỉ cần xét điểm đầu: đoạn thẳng không cắt cạnh nào mà có một mút nằm trong
            // thì toàn bộ đoạn nằm trong.
            return Contains(geoLine.StartPoint, tolerance);
        }

        /// <summary>
        /// Kiểm tra xem đa giác có giao với một đa giác khác hay không, sử dụng dung sai mặc định.
        /// </summary>
        public bool IntersectsWith(GeoPolygon other)
        {
            return IntersectsWith(other, Tolerance.Global);
        }

        /// <summary>
        /// Kiểm tra xem đa giác có giao với một đa giác khác hay không.
        /// </summary>
        public bool IntersectsWith(GeoPolygon other, Tolerance tolerance)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            // 1. Kiểm tra xem cạnh của hai đa giác có cắt nhau hay không
            foreach (var edge in GetEdges())
            {
                foreach (var otherEdge in other.GetEdges())
                {
                    if (edge.TryIntersectWith(otherEdge, out _, tolerance))
                    {
                        return true;
                    }
                }
            }

            // 2. Không cạnh nào cắt nhau: hoặc hai đa giác rời nhau, hoặc một đa giác nằm lọt
            // hẳn bên trong đa giác kia. Xét một đỉnh của mỗi bên là đủ để phân biệt hai trường hợp.
            return Contains(other._vertices[0], tolerance) || other.Contains(_vertices[0], tolerance);
        }

        public bool Equals(GeoPolygon other)
        {
            if (other == null || other.VertexCount != VertexCount) return false;
            for (int i = 0; i < _vertices.Length; i++)
            {
                if (!_vertices[i].Equals(other._vertices[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is GeoPolygon other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                foreach (var v in _vertices)
                {
                    hash = hash * 31 + v.GetHashCode();
                }
                return hash;
            }
        }

        public override string ToString() => $"GeoPolygon[{VertexCount} vertices, Area:{GetArea():0.000}]";
    }
}

