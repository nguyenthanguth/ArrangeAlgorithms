using System;

namespace ArrangeAlgorithms.Geometry
{
    /// <summary>
    /// Biểu diễn một đoạn thẳng 2D giữa điểm đầu và điểm cuối.
    /// </summary>
    public readonly struct GeoLine : IEquatable<GeoLine>
    {
        /// <summary>
        /// Lấy điểm đầu của đoạn thẳng.
        /// </summary>
        public GeoPoint StartPoint { get; }

        /// <summary>
        /// Lấy điểm cuối của đoạn thẳng.
        /// </summary>
        public GeoPoint EndPoint { get; }

        /// <summary>
        /// Khởi tạo một thực thể GeoLine mới từ điểm đầu và điểm cuối.
        /// </summary>
        /// <param name="startPoint">Điểm đầu.</param>
        /// <param name="endPoint">Điểm cuối.</param>
        public GeoLine(GeoPoint startPoint, GeoPoint endPoint)
        {
            StartPoint = startPoint;
            EndPoint = endPoint;
        }

        /// <summary>
        /// Khởi tạo một thực thể GeoLine từ tọa độ các điểm đầu mút.
        /// </summary>
        public GeoLine(double startX, double startY, double endX, double endY)
            : this(new GeoPoint(startX, startY), new GeoPoint(endX, endY))
        {
        }

        /// <summary>
        /// Lấy GeoVector hướng từ điểm đầu tới điểm cuối.
        /// </summary>
        public GeoVector Direction => StartPoint.GetVectorTo(EndPoint);

        /// <summary>
        /// Lấy độ dài đoạn thẳng.
        /// </summary>
        public double Length => StartPoint.DistanceTo(EndPoint);

        /// <summary>
        /// Lấy bình phương độ dài đoạn thẳng.
        /// </summary>
        public double LengthSquared => StartPoint.GetDistanceSquaredTo(EndPoint);

        /// <summary>
        /// Lấy trung điểm của đoạn thẳng.
        /// </summary>
        public GeoPoint MidPoint => StartPoint.GetMiddlePoint(EndPoint);

        /// <summary>
        /// Lấy điểm trên đoạn thẳng theo tham số t (t=0 là điểm đầu, t=1 là điểm cuối).
        /// </summary>
        public GeoPoint GetPointAtParameter(double parameter)
        {
            return StartPoint.Add(Direction.Multiply(parameter));
        }

        /// <summary>
        /// Chiếu một điểm lên đường thẳng và lấy giá trị tham số dọc theo đoạn thẳng.
        /// </summary>
        public double GetParameterOf(GeoPoint GeoPoint)
        {
            GeoVector dir = Direction;
            double lenSq = dir.LengthSquared;
            if (lenSq <= Tolerance.Global.EqualPoint * Tolerance.Global.EqualPoint) return 0.0;
            return StartPoint.GetVectorTo(GeoPoint).DotProduct(dir) / lenSq;
        }

        /// <summary>
        /// Lấy điểm gần nhất trên đoạn thẳng tới một điểm cho trước.
        /// </summary>
        public GeoPoint GetClosestPointTo(GeoPoint GeoPoint)
        {
            double t = GetParameterOf(GeoPoint);
            if (t <= 0.0) return StartPoint;
            if (t >= 1.0) return EndPoint;
            return GetPointAtParameter(t);
        }

        /// <summary>
        /// Tính khoảng cách từ một điểm tới điểm gần nhất trên đoạn thẳng này.
        /// </summary>
        public double DistanceTo(GeoPoint GeoPoint)
        {
            return GeoPoint.DistanceTo(GetClosestPointTo(GeoPoint));
        }

        /// <summary>
        /// Kiểm tra xem một điểm có nằm trên đoạn thẳng sử dụng dung sai mặc định hay không.
        /// </summary>
        public bool IsPointOn(GeoPoint GeoPoint)
        {
            return IsPointOn(GeoPoint, Tolerance.Global);
        }

        /// <summary>
        /// Kiểm tra xem một điểm có nằm trên đoạn thẳng trong khoảng dung sai hay không.
        /// </summary>
        public bool IsPointOn(GeoPoint GeoPoint, Tolerance tolerance)
        {
            return DistanceTo(GeoPoint) <= tolerance.EqualPoint;
        }

        /// <summary>
        /// Thử tính giao điểm với một đoạn thẳng khác sử dụng dung sai mặc định.
        /// </summary>
        public bool TryIntersectWith(GeoLine other, out GeoPoint intersection)
        {
            return TryIntersectWith(other, out intersection, Tolerance.Global);
        }

        /// <summary>
        /// Thử tính giao điểm với một đoạn thẳng khác trong khoảng dung sai.
        /// </summary>
        public bool TryIntersectWith(GeoLine other, out GeoPoint intersection, Tolerance tolerance)
        {
            intersection = new GeoPoint(0, 0);
            GeoVector r = Direction;
            GeoVector s = other.Direction;
            double rCrossS = r.CrossProduct(s);

            if (Math.Abs(rCrossS) <= tolerance.EqualPoint)
            {
                return false; // Song song hoặc trùng phương
            }

            GeoVector qMinusP = StartPoint.GetVectorTo(other.StartPoint);
            double t = qMinusP.CrossProduct(s) / rCrossS;
            double u = qMinusP.CrossProduct(r) / rCrossS;

            if (t >= -tolerance.EqualPoint && t <= 1.0 + tolerance.EqualPoint && 
                u >= -tolerance.EqualPoint && u <= 1.0 + tolerance.EqualPoint)
            {
                intersection = GetPointAtParameter(t);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Kiểm tra xem đoạn thẳng này có giao với một đoạn thẳng khác hay không, sử dụng dung sai mặc định.
        /// </summary>
        public bool IntersectsWith(GeoLine other)
        {
            return IntersectsWith(other, Tolerance.Global);
        }

        /// <summary>
        /// Kiểm tra xem đoạn thẳng này có giao với một đoạn thẳng khác hay không.
        /// </summary>
        public bool IntersectsWith(GeoLine other, Tolerance tolerance)
        {
            return TryIntersectWith(other, out _, tolerance);
        }

        /// <summary>
        /// Kiểm tra xem đoạn thẳng này có giao với một hình chữ nhật xoay (GeoRectangle OBB) hay không, sử dụng dung sai mặc định.
        /// </summary>
        public bool IntersectsWith(GeoRectangle rect)
        {
            return IntersectsWith(rect, Tolerance.Global);
        }

        /// <summary>
        /// Kiểm tra xem đoạn thẳng này có giao với một hình chữ nhật xoay (GeoRectangle OBB) hay không.
        /// </summary>
        public bool IntersectsWith(GeoRectangle rect, Tolerance tolerance)
        {
            return rect.IntersectsWith(this, tolerance);
        }

        /// <summary>
        /// Kiểm tra xem đoạn thẳng này có giao với một đa giác hay không, sử dụng dung sai mặc định.
        /// </summary>
        public bool IntersectsWith(GeoPolygon poly)
        {
            return IntersectsWith(poly, Tolerance.Global);
        }

        /// <summary>
        /// Kiểm tra xem đoạn thẳng này có giao với một đa giác hay không.
        /// </summary>
        public bool IntersectsWith(GeoPolygon poly, Tolerance tolerance)
        {
            if (poly == null) throw new ArgumentNullException(nameof(poly));

            return poly.IntersectsWith(this, tolerance);
        }

        public bool Equals(GeoLine other)
        {
            return StartPoint.Equals(other.StartPoint) && EndPoint.Equals(other.EndPoint);
        }

        public override bool Equals(object obj)
        {
            return obj is GeoLine other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (StartPoint.GetHashCode() * 397) ^ EndPoint.GetHashCode();
            }
        }

        public static bool operator ==(GeoLine left, GeoLine right) => left.Equals(right);
        public static bool operator !=(GeoLine left, GeoLine right) => !left.Equals(right);

        public override string ToString() => $"GeoLine[{StartPoint} -> {EndPoint}]";
    }
}

