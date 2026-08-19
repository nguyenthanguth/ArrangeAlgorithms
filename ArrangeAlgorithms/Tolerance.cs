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
        /// Default tolerance when comparing angles for parallelism / perpendicularity, in radians (1 degree in radians).
        /// </summary>
        public const double DefaultEqualAngleRad = Math.PI / 180.0;

        /// <summary>
        /// Tolerance applied for overloads without explicit tolerance.
        /// </summary>
        public static Tolerance Global { get; set; } = new Tolerance(DefaultEqualPoint, DefaultEqualVector, DefaultEqualAngleRad);

        /// <summary>
        /// Initializes a tolerance instance with two thresholds for points and GeoVectors, using the default angle threshold.
        /// </summary>
        /// <param name="equalPoint">Distance threshold when comparing two points.</param>
        /// <param name="equalVector">Threshold when comparing two GeoVectors.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when one of the thresholds is negative.</exception>
        public Tolerance(double equalPoint, double equalVector)
            : this(equalPoint, equalVector, DefaultEqualAngleRad)
        {
        }

        /// <summary>
        /// Initializes a tolerance instance with thresholds for points, GeoVectors, and angles.
        /// </summary>
        /// <param name="equalPoint">Distance threshold when comparing two points.</param>
        /// <param name="equalVector">Threshold when comparing two GeoVectors.</param>
        /// <param name="equalAngleRad">Angular threshold when comparing angles or parallelism, in radians.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when one of the thresholds is negative.</exception>
        public Tolerance(double equalPoint, double equalVector, double equalAngleRad)
        {
            if (equalPoint < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(equalPoint), "Tolerance cannot be negative.");
            }

            if (equalVector < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(equalVector), "Tolerance cannot be negative.");
            }

            if (equalAngleRad < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(equalAngleRad), "Tolerance cannot be negative.");
            }

            EqualPoint = equalPoint;
            EqualVector = equalVector;
            EqualAngleRad = equalAngleRad;
            EqualAngleSin = Math.Sin(equalAngleRad);
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
        /// Angular threshold to consider two directions / lines as parallel or perpendicular, in radians.
        /// </summary>
        public double EqualAngleRad { get; }

        /// <summary>
        /// Sine of <see cref="EqualAngleRad"/>, computed once here because the angular comparisons in
        /// Intersection and Parallel sit inside nested edge loops, where a transcendental call per
        /// comparison is a measurable share of the total cost.
        /// <para>
        /// The default struct value leaves this at 0, which is exactly Sin(0) for the matching
        /// EqualAngleRad of 0, so an uninitialized Tolerance stays self-consistent.
        /// </para>
        /// </summary>
        internal double EqualAngleSin { get; }

        /// <summary>
        /// Compares two tolerance instances exactly.
        /// </summary>
        public bool Equals(Tolerance other)
        {
            return EqualPoint.Equals(other.EqualPoint) &&
                   EqualVector.Equals(other.EqualVector) &&
                   EqualAngleRad.Equals(other.EqualAngleRad);
        }

        /// <summary>
        /// Compares with an arbitrary object.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is Tolerance other && Equals(other);
        }

        /// <summary>
        /// Hash code built from thresholds.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + EqualPoint.GetHashCode();
                hash = hash * 31 + EqualVector.GetHashCode();
                hash = hash * 31 + EqualAngleRad.GetHashCode();
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
        /// Represents the thresholds as a string.
        /// </summary>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "(EqualPoint: {0}, EqualVector: {1}, EqualAngleRad: {2:0.000})",
                EqualPoint,
                EqualVector,
                EqualAngleRad);
        }
    }
}
