using System;

namespace ArrangeAlgorithms.Geometry
{
    /// <summary>
    /// Biểu diện một điểm 2D với tọa độ kiểu double.
    /// </summary>
    public readonly struct GeoPoint : IEquatable<GeoPoint>
    {
        /// <summary>
        /// Lấy tọa độ X của điểm.
        /// </summary>
        public double X { get; }

        /// <summary>
        /// Lấy tọa độ Y của điểm.
        /// </summary>
        public double Y { get; }

        /// <summary>
        /// Khởi tạo một điểm mới tại gốc tọa độ (0, 0).
        /// </summary>
        public GeoPoint()
        {
            X = 0.0;
            Y = 0.0;
        }

        /// <summary>
        /// Khởi tạo một điểm mới.
        /// </summary>
        /// <param name="x">Tọa độ X.</param>
        /// <param name="y">Tọa độ Y.</param>
        public GeoPoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Khởi tạo một điểm mới từ một điểm khác.
        /// </summary>
        /// <param name="geoPoint">Điểm nguồn.</param>
        public GeoPoint(GeoPoint geoPoint)
        {
            X = geoPoint.X;
            Y = geoPoint.Y;
        }

        /// <summary>
        /// Cộng một GeoVector vào điểm để dịch chuyển tọa độ.
        /// </summary>
        public GeoPoint Add(GeoVector GeoVector)
        {
            return new GeoPoint(X + GeoVector.X, Y + GeoVector.Y);
        }

        /// <summary>
        /// Trừ một GeoVector khỏi điểm.
        /// </summary>
        public GeoPoint Subtract(GeoVector GeoVector)
        {
            return new GeoPoint(X - GeoVector.X, Y - GeoVector.Y);
        }

        /// <summary>
        /// Lấy GeoVector trỏ từ điểm này tới điểm khác.
        /// </summary>
        public GeoVector GetVectorTo(GeoPoint other)
        {
            return new GeoVector(other.X - X, other.Y - Y);
        }

        /// <summary>
        /// Tính khoảng cách Euclid tới điểm khác.
        /// </summary>
        public double DistanceTo(GeoPoint other)
        {
            return Math.Sqrt(GetDistanceSquaredTo(other));
        }

        /// <summary>
        /// Tính bình phương khoảng cách Euclid tới điểm khác.
        /// </summary>
        public double GetDistanceSquaredTo(GeoPoint other)
        {
            double dx = other.X - X;
            double dy = other.Y - Y;
            return dx * dx + dy * dy;
        }

        /// <summary>
        /// Lấy trung điểm giữa điểm này và điểm khác.
        /// </summary>
        public GeoPoint GetMiddlePoint(GeoPoint other)
        {
            return new GeoPoint((X + other.X) * 0.5, (Y + other.Y) * 0.5);
        }

        /// <summary>
        /// Xoay điểm quanh một tâm xoay với góc xoay tính theo radian (ngược chiều kim đồng hồ).
        /// </summary>
        public GeoPoint RotateBy(double angleRad, GeoPoint center)
        {
            double cos = Math.Cos(angleRad);
            double sin = Math.Sin(angleRad);
            double dx = X - center.X;
            double dy = Y - center.Y;

            return new GeoPoint(
                center.X + dx * cos - dy * sin,
                center.Y + dx * sin + dy * cos);
        }

        /// <summary>
        /// Co giãn khoảng cách từ tâm tới điểm này theo một hệ số.
        /// </summary>
        public GeoPoint ScaleBy(double factor, GeoPoint center)
        {
            return new GeoPoint(
                center.X + (X - center.X) * factor,
                center.Y + (Y - center.Y) * factor);
        }

        /// <summary>
        /// So sánh điểm này có trùng với điểm khác trong khoảng dung sai cho phép hay không.
        /// </summary>
        public bool IsEqualTo(GeoPoint other, Tolerance tolerance)
        {
            return GetDistanceSquaredTo(other) <= tolerance.EqualPoint * tolerance.EqualPoint;
        }

        /// <summary>
        /// So sánh điểm này có trùng với điểm khác sử dụng dung sai mặc định hay không.
        /// </summary>
        public bool IsEqualTo(GeoPoint other) => IsEqualTo(other, Tolerance.Global);

        public static GeoPoint operator +(GeoPoint p, GeoVector v) => p.Add(v);
        public static GeoPoint operator -(GeoPoint p, GeoVector v) => p.Subtract(v);
        public static GeoVector operator -(GeoPoint p2, GeoPoint p1) => p1.GetVectorTo(p2);

        public bool Equals(GeoPoint other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override bool Equals(object obj)
        {
            return obj is GeoPoint other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        public static bool operator ==(GeoPoint left, GeoPoint right) => left.Equals(right);
        public static bool operator !=(GeoPoint left, GeoPoint right) => !left.Equals(right);

        public override string ToString() => $"({X:0.000}, {Y:0.000})";
    }
}

