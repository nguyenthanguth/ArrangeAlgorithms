using System;
using System.Globalization;

namespace ArrangeAlgorithms
{
    /// <summary>
    /// Dung sai dùng cho các phép so sánh hình học.
    /// </summary>
    public readonly struct Tolerance : IEquatable<Tolerance>
    {
        /// <summary>
        /// Dung sai mặc định khi so sánh điểm.
        /// </summary>
        public const double DefaultEqualPoint = 1E-4;

        /// <summary>
        /// Dung sai mặc định khi so sánh GeoVector.
        /// </summary>
        public const double DefaultEqualVector = 1E-4;

        /// <summary>
        /// Dung sai áp dụng cho các overload không truyền dung sai tường minh.
        /// </summary>
        public static Tolerance Global { get; set; } = new Tolerance(DefaultEqualPoint, DefaultEqualVector);

        /// <summary>
        /// Khởi tạo một bộ dung sai với hai ngưỡng cho điểm và cho GeoVector.
        /// </summary>
        /// <param name="equalPoint">Ngưỡng khoảng cách khi so sánh hai điểm.</param>
        /// <param name="equalVector">Ngưỡng khi so sánh hai GeoVector.</param>
        /// <exception cref="ArgumentOutOfRangeException">Khi một trong hai ngưỡng mang giá trị âm.</exception>
        public Tolerance(double equalPoint, double equalVector)
        {
            if (equalPoint < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(equalPoint), "Dung sai không được âm.");
            }

            if (equalVector < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(equalVector), "Dung sai không được âm.");
            }

            EqualPoint = equalPoint;
            EqualVector = equalVector;
        }

        /// <summary>
        /// Ngưỡng khoảng cách để coi hai điểm là trùng nhau, tính theo đơn vị bản vẽ.
        /// </summary>
        public double EqualPoint { get; }

        /// <summary>
        /// Ngưỡng để coi hai GeoVector là bằng nhau.
        /// </summary>
        public double EqualVector { get; }

        /// <summary>
        /// So sánh chính xác hai bộ dung sai.
        /// </summary>
        public bool Equals(Tolerance other)
        {
            return EqualPoint.Equals(other.EqualPoint) && EqualVector.Equals(other.EqualVector);
        }

        /// <summary>
        /// So sánh với một đối tượng bất kỳ.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is Tolerance other && Equals(other);
        }

        /// <summary>
        /// Mã băm dựng từ hai ngưỡng.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + EqualPoint.GetHashCode();
                hash = hash * 31 + EqualVector.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(Tolerance tolerance1, Tolerance tolerance2)
        {
            return tolerance1.Equals(tolerance2);
        }

        public static bool operator !=(Tolerance tolerance1, Tolerance tolerance2)
        {
            return !tolerance1.Equals(tolerance2);
        }

        /// <summary>
        /// Biểu diễn hai ngưỡng dưới dạng chuỗi.
        /// </summary>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "(EqualPoint: {0}, EqualVector: {1})",
                EqualPoint,
                EqualVector);
        }
    }
}

