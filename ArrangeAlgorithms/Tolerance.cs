using System;
using System.Globalization;

namespace ArrangeAlgorithms
{
    /// <summary>
    /// Tolerance used for geometric comparisons.
    /// </summary>
    public readonly struct Tolerance : IEquatable<Tolerance>
    {
        /// <summary>
        /// Default tolerance when comparing points.
        /// </summary>
        public const double DefaultEqualPoint = 1E-4;

        /// <summary>
        /// Default tolerance when comparing GeoVectors.
        /// </summary>
        public const double DefaultEqualVector = 1E-4;

        /// <summary>
        /// Tolerance applied for overloads without explicit tolerance.
        /// </summary>
        public static Tolerance Global { get; set; } = new Tolerance(DefaultEqualPoint, DefaultEqualVector);

        /// <summary>
        /// Initializes a tolerance instance with two thresholds for points and GeoVectors.
        /// </summary>
        /// <param name="equalPoint">Distance threshold when comparing two points.</param>
        /// <param name="equalVector">Threshold when comparing two GeoVectors.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when one of the thresholds is negative.</exception>
        public Tolerance(double equalPoint, double equalVector)
        {
            if (equalPoint < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(equalPoint), "Tolerance cannot be negative.");
            }

            if (equalVector < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(equalVector), "Tolerance cannot be negative.");
            }

            EqualPoint = equalPoint;
            EqualVector = equalVector;
        }

        /// <summary>
        /// Distance threshold to consider two points as coincident, in drawing units.
        /// </summary>
        public double EqualPoint { get; }

        /// <summary>
        /// Threshold to consider two GeoVectors as equal.
        /// </summary>
        public double EqualVector { get; }

        /// <summary>
        /// Compares two tolerance instances exactly.
        /// </summary>
        public bool Equals(Tolerance other)
        {
            return EqualPoint.Equals(other.EqualPoint) && EqualVector.Equals(other.EqualVector);
        }

        /// <summary>
        /// Compares with an arbitrary object.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is Tolerance other && Equals(other);
        }

        /// <summary>
        /// Hash code built from both thresholds.
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

        /// <summary>
        /// Compares two Tolerance instances for equality.
        /// </summary>
        /// <param name="tolerance1">The first tolerance.</param>
        /// <param name="tolerance2">The second tolerance.</param>
        /// <returns>true if they are equal; otherwise, false.</returns>
        public static bool operator ==(Tolerance tolerance1, Tolerance tolerance2)
        {
            return tolerance1.Equals(tolerance2);
        }

        /// <summary>
        /// Compares two Tolerance instances for inequality.
        /// </summary>
        /// <param name="tolerance1">The first tolerance.</param>
        /// <param name="tolerance2">The second tolerance.</param>
        /// <returns>true if they are not equal; otherwise, false.</returns>
        public static bool operator !=(Tolerance tolerance1, Tolerance tolerance2)
        {
            return !tolerance1.Equals(tolerance2);
        }

        /// <summary>
        /// Represents the two thresholds as a string.
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

