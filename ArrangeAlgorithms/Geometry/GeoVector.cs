using System;

namespace ArrangeAlgorithms.Geometry
{
    /// <summary>
    /// Biểu diễn một GeoVector 2D với tọa độ kiểu double.
    /// </summary>
    public readonly struct GeoVector : IEquatable<GeoVector>
    {
        /// <summary>
        /// Lấy tọa độ X của GeoVector.
        /// </summary>
        public double X { get; }

        /// <summary>
        /// Lấy tọa độ Y của GeoVector.
        /// </summary>
        public double Y { get; }

        /// <summary>
        /// Lấy GeoVector không (0, 0).
        /// </summary>
        public static GeoVector Zero => new GeoVector(0.0, 0.0);

        /// <summary>
        /// Lấy GeoVector đơn vị dọc theo trục X (1, 0).
        /// </summary>
        public static GeoVector XAxis => new GeoVector(1.0, 0.0);

        /// <summary>
        /// Lấy GeoVector đơn vị dọc theo trục Y (0, 1).
        /// </summary>
        public static GeoVector YAxis => new GeoVector(0.0, 1.0);

        /// <summary>
        /// Khởi tạo một GeoVector mới.
        /// </summary>
        /// <param name="x">Thành phần X.</param>
        /// <param name="y">Thành phần Y.</param>
        public GeoVector(double x, double y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Lấy chiều dài của GeoVector.
        /// </summary>
        public double Length => Math.Sqrt(LengthSquared);

        /// <summary>
        /// Lấy bình phương chiều dài của GeoVector.
        /// </summary>
        public double LengthSquared => X * X + Y * Y;

        /// <summary>
        /// Lấy GeoVector đơn vị đã chuẩn hóa. Ném ngoại lệ InvalidOperationException nếu GeoVector có chiều dài bằng 0.
        /// </summary>
        public GeoVector Normalize()
        {
            if (!TryGetNormal(out GeoVector normal))
            {
                throw new InvalidOperationException("Không thể chuẩn hóa GeoVector có chiều dài bằng không.");
            }
            return normal;
        }

        /// <summary>
        /// Thử chuẩn hóa GeoVector mà không ném ngoại lệ sử dụng dung sai mặc định.
        /// </summary>
        public bool TryGetNormal(out GeoVector normal)
        {
            return TryGetNormal(out normal, Tolerance.Global);
        }

        /// <summary>
        /// Thử chuẩn hóa GeoVector với một dung sai cụ thể.
        /// </summary>
        public bool TryGetNormal(out GeoVector normal, Tolerance tolerance)
        {
            double len = Length;
            if (len <= tolerance.EqualVector)
            {
                normal = Zero;
                return false;
            }

            normal = new GeoVector(X / len, Y / len);
            return true;
        }

        /// <summary>
        /// Cộng GeoVector này với GeoVector khác.
        /// </summary>
        public GeoVector Add(GeoVector other)
        {
            return new GeoVector(X + other.X, Y + other.Y);
        }

        /// <summary>
        /// Trừ GeoVector khác khỏi GeoVector này.
        /// </summary>
        public GeoVector Subtract(GeoVector other)
        {
            return new GeoVector(X - other.X, Y - other.Y);
        }

        /// <summary>
        /// Nhân các thành phần của GeoVector với một số vô hướng.
        /// </summary>
        public GeoVector Multiply(double scalar)
        {
            return new GeoVector(X * scalar, Y * scalar);
        }

        /// <summary>
        /// Tính tích vô hướng (dot product) với GeoVector khác.
        /// </summary>
        public double DotProduct(GeoVector other)
        {
            return X * other.X + Y * other.Y;
        }

        /// <summary>
        /// Tính tích có hướng 2D (cross product) với GeoVector khác (trả về thành phần Z).
        /// </summary>
        public double CrossProduct(GeoVector other)
        {
            return X * other.Y - Y * other.X;
        }

        /// <summary>
        /// Lấy GeoVector vuông góc bằng cách xoay ngược chiều kim đồng hồ 90 độ.
        /// </summary>
        public GeoVector GetPerpendicularVector()
        {
            return new GeoVector(-Y, X);
        }

        /// <summary>
        /// Xoay GeoVector một góc tính theo radian (ngược chiều kim đồng hồ).
        /// </summary>
        public GeoVector RotateBy(double angleRad)
        {
            double cos = Math.Cos(angleRad);
            double sin = Math.Sin(angleRad);
            return new GeoVector(X * cos - Y * sin, X * sin + Y * cos);
        }

        /// <summary>
        /// Lấy góc không dấu tới một GeoVector khác tính theo radian, trong đoạn [0, PI].
        /// </summary>
        public double GetAngleTo(GeoVector other)
        {
            if (IsZeroLength() || other.IsZeroLength())
            {
                throw new InvalidOperationException("Không thể tính góc đến hoặc từ GeoVector có độ dài bằng không.");
            }

            // Dùng Atan2(|cross|, dot) thay cho Acos(dot). Acos mất chính xác nghiêm trọng khi hai
            // GeoVector gần cùng phương hoặc gần ngược phương: đạo hàm của nó tiến ra vô cực ở lân cận
            // ±1, nên sai số làm tròn cỡ 1e-16 của tích vô hướng bị khuếch đại thành sai số góc cỡ 1e-8.
            // Atan2 ổn định trên toàn dải và không cần chuẩn hóa: cả hai đối số đều tỷ lệ với |a|*|b|.
            return Math.Atan2(Math.Abs(CrossProduct(other)), DotProduct(other));
        }

        /// <summary>
        /// Lấy góc có dấu tới một GeoVector khác tính theo radian, trong khoảng (-PI, PI].
        /// </summary>
        public double GetSignedAngleTo(GeoVector other)
        {
            if (IsZeroLength() || other.IsZeroLength())
            {
                throw new InvalidOperationException("Không thể tính góc đến hoặc từ GeoVector có độ dài bằng không.");
            }

            return Math.Atan2(CrossProduct(other), DotProduct(other));
        }

        /// <summary>
        /// Kiểm tra xem GeoVector có độ dài bằng không sử dụng dung sai mặc định hay không.
        /// </summary>
        public bool IsZeroLength()
        {
            return IsZeroLength(Tolerance.Global);
        }

        /// <summary>
        /// Kiểm tra xem GeoVector có độ dài bằng không trong khoảng dung sai hay không.
        /// </summary>
        public bool IsZeroLength(Tolerance tolerance)
        {
            return LengthSquared <= tolerance.EqualVector * tolerance.EqualVector;
        }

        /// <summary>
        /// So sánh GeoVector này có bằng GeoVector khác sử dụng dung sai mặc định hay không.
        /// </summary>
        public bool IsEqualTo(GeoVector other)
        {
            return IsEqualTo(other, Tolerance.Global);
        }

        /// <summary>
        /// So sánh GeoVector này có bằng GeoVector khác trong khoảng dung sai hay không.
        /// </summary>
        public bool IsEqualTo(GeoVector other, Tolerance tolerance)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            return dx * dx + dy * dy <= tolerance.EqualVector * tolerance.EqualVector;
        }

        /// <summary>
        /// Kiểm tra xem GeoVector này có song song với GeoVector khác sử dụng dung sai mặc định hay không.
        /// </summary>
        public bool IsParallelTo(GeoVector other)
        {
            return IsParallelTo(other, Tolerance.Global);
        }

        /// <summary>
        /// Kiểm tra xem GeoVector này có song song với GeoVector khác hay không.
        /// </summary>
        public bool IsParallelTo(GeoVector other, Tolerance tolerance)
        {
            if (!TryGetNormal(out GeoVector a, tolerance) || !other.TryGetNormal(out GeoVector b, tolerance))
            {
                return false;
            }
            return Math.Abs(a.CrossProduct(b)) <= tolerance.EqualVector;
        }

        /// <summary>
        /// Kiểm tra xem GeoVector này có vuông góc với GeoVector khác sử dụng dung sai mặc định hay không.
        /// </summary>
        public bool IsPerpendicularTo(GeoVector other)
        {
            return IsPerpendicularTo(other, Tolerance.Global);
        }

        /// <summary>
        /// Kiểm tra xem GeoVector này có vuông góc với GeoVector khác hay không.
        /// </summary>
        public bool IsPerpendicularTo(GeoVector other, Tolerance tolerance)
        {
            if (!TryGetNormal(out GeoVector a, tolerance) || !other.TryGetNormal(out GeoVector b, tolerance))
            {
                return false;
            }
            return Math.Abs(a.DotProduct(b)) <= tolerance.EqualVector;
        }

        public static GeoVector operator +(GeoVector v1, GeoVector v2) => v1.Add(v2);
        public static GeoVector operator -(GeoVector v1, GeoVector v2) => v1.Subtract(v2);
        public static GeoVector operator -(GeoVector v) => new GeoVector(-v.X, -v.Y);
        public static GeoVector operator *(GeoVector v, double s) => v.Multiply(s);
        public static GeoVector operator *(double s, GeoVector v) => v.Multiply(s);
        public static GeoVector operator /(GeoVector v, double s) => new GeoVector(v.X / s, v.Y / s);

        public bool Equals(GeoVector other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override bool Equals(object obj)
        {
            return obj is GeoVector other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        public static bool operator ==(GeoVector left, GeoVector right) => left.Equals(right);
        public static bool operator !=(GeoVector left, GeoVector right) => !left.Equals(right);

        public override string ToString() => $"({X:0.000}, {Y:0.000})";
    }
}

