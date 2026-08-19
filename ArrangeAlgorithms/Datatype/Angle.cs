using System;
using System.Globalization;

namespace ArrangeAlgorithms.Datatype
{
    /// <summary>
    /// Represents a planar angle as a single value that can be read as either radians or degrees.
    /// <para>
    /// The value is stored in radians because every angular API in this library works in radians:
    /// <c>GeoRectangle.AngleRad</c>, <c>GeoPoint.RotateBy</c>, <c>GeoVector.GetAngleTo</c>, and
    /// <c>Tolerance.EqualAngleRad</c>. Degrees are a view over that value, converted on read.
    /// </para>
    /// <para>
    /// There is deliberately no public constructor taking a bare double. The reason this type exists is
    /// that a number on its own does not say which unit it is in, so the unit has to be named at the
    /// point of creation: use <see cref="FromRadians"/> or <see cref="FromDegrees"/>.
    /// </para>
    /// </summary>
    public readonly struct Angle : IEquatable<Angle>, IComparable<Angle>
    {
        #region Constants

        /// <summary>
        /// Number of degrees in one radian.
        /// </summary>
        public const double DegreesPerRadian = 180.0 / Math.PI;

        /// <summary>
        /// Number of radians in one degree.
        /// </summary>
        public const double RadiansPerDegree = Math.PI / 180.0;

        /// <summary>
        /// One full turn expressed in radians.
        /// </summary>
        public const double FullTurnRadians = 2.0 * Math.PI;

        /// <summary>
        /// One full turn expressed in degrees.
        /// </summary>
        public const double FullTurnDegrees = 360.0;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes an angle from a value already expressed in radians.
        /// </summary>
        private Angle(double radians)
        {
            Radians = radians;
        }

        /// <summary>
        /// Creates an angle from a value in radians.
        /// </summary>
        /// <param name="radians">The angle in radians.</param>
        /// <returns>The corresponding angle.</returns>
        public static Angle FromRadians(double radians) => new Angle(radians);

        /// <summary>
        /// Creates an angle from a value in degrees.
        /// </summary>
        /// <param name="degrees">The angle in degrees.</param>
        /// <returns>The corresponding angle.</returns>
        public static Angle FromDegrees(double degrees) => new Angle(degrees * RadiansPerDegree);

        #endregion

        #region Well-known values

        /// <summary>
        /// The zero angle (0 degrees).
        /// </summary>
        public static Angle Zero => new Angle(0.0);

        /// <summary>
        /// A right angle (90 degrees).
        /// </summary>
        public static Angle Right => new Angle(Math.PI * 0.5);

        /// <summary>
        /// A straight angle (180 degrees).
        /// </summary>
        public static Angle Straight => new Angle(Math.PI);

        /// <summary>
        /// A full turn (360 degrees).
        /// </summary>
        public static Angle FullTurn => new Angle(FullTurnRadians);

        #endregion

        #region Values

        /// <summary>
        /// Gets the angle in radians. This is the stored value.
        /// </summary>
        public double Radians { get; }

        /// <summary>
        /// Gets the angle in degrees, converted from the stored radian value on each read.
        /// </summary>
        public double Degrees => Radians * DegreesPerRadian;

        #endregion

        #region Static conversion of raw values

        /// <summary>
        /// Converts a raw radian value to degrees, without going through an <see cref="Angle"/> instance.
        /// </summary>
        /// <param name="radians">The angle in radians.</param>
        /// <returns>The same angle in degrees.</returns>
        public static double ToDegrees(double radians) => radians * DegreesPerRadian;

        /// <summary>
        /// Converts a raw degree value to radians, without going through an <see cref="Angle"/> instance.
        /// </summary>
        /// <param name="degrees">The angle in degrees.</param>
        /// <returns>The same angle in radians.</returns>
        public static double ToRadians(double degrees) => degrees * RadiansPerDegree;

        #endregion

        #region Normalization

        /// <summary>
        /// Wraps the angle into the range [0, 2*PI), the form used to express a rotation as a full turn.
        /// A rotation of -90 degrees normalizes to 270 degrees.
        /// </summary>
        /// <returns>The equivalent angle in [0, 2*PI).</returns>
        public Angle Normalize()
        {
            double wrapped = Radians % FullTurnRadians;

            if (wrapped < 0.0)
            {
                wrapped += FullTurnRadians;

                // A tiny negative input rounds up to exactly one full turn when shifted, which would fall
                // outside the half-open range this method promises.
                if (wrapped >= FullTurnRadians)
                {
                    wrapped = 0.0;
                }
            }

            return new Angle(wrapped);
        }

        /// <summary>
        /// Wraps the angle into the range (-PI, PI], the form used to express the shortest rotation to a
        /// direction. A rotation of 270 degrees normalizes to -90 degrees.
        /// </summary>
        /// <returns>The equivalent angle in (-PI, PI].</returns>
        public Angle NormalizeSigned()
        {
            double wrapped = Radians % FullTurnRadians;

            if (wrapped > Math.PI)
            {
                wrapped -= FullTurnRadians;
            }
            else if (wrapped <= -Math.PI)
            {
                wrapped += FullTurnRadians;
            }

            return new Angle(wrapped);
        }

        #endregion

        #region Arithmetic

        /// <summary>
        /// Adds another angle to this one.
        /// </summary>
        public Angle Add(Angle other) => new Angle(Radians + other.Radians);

        /// <summary>
        /// Subtracts another angle from this one.
        /// </summary>
        public Angle Subtract(Angle other) => new Angle(Radians - other.Radians);

        /// <summary>
        /// Multiplies the angle by a scalar factor.
        /// </summary>
        public Angle Multiply(double factor) => new Angle(Radians * factor);

        /// <summary>
        /// Divides the angle by a scalar divisor.
        /// </summary>
        public Angle Divide(double divisor) => new Angle(Radians / divisor);

        /// <summary>
        /// Gets the angle with the opposite sign.
        /// </summary>
        public Angle Negate() => new Angle(-Radians);

        /// <summary>
        /// Gets the angle with its sign removed.
        /// </summary>
        public Angle Abs() => new Angle(Math.Abs(Radians));

        #endregion

        #region Comparison

        /// <summary>
        /// Compares whether this angle equals another within the default angular tolerance.
        /// <para>
        /// The raw values are compared, not their normalized forms, so 0 degrees and 360 degrees are not
        /// equal here. Call <see cref="Normalize"/> on both sides first when comparing directions rather
        /// than rotation amounts.
        /// </para>
        /// </summary>
        public bool IsEqualTo(Angle other) => IsEqualTo(other, Tolerance.Global);

        /// <summary>
        /// Compares whether this angle equals another within the angular tolerance of the supplied
        /// <see cref="Tolerance"/>.
        /// <para>
        /// The raw values are compared, not their normalized forms, so 0 degrees and 360 degrees are not
        /// equal here. Call <see cref="Normalize"/> on both sides first when comparing directions rather
        /// than rotation amounts.
        /// </para>
        /// </summary>
        public bool IsEqualTo(Angle other, Tolerance tolerance)
        {
            return Math.Abs(Radians - other.Radians) <= tolerance.EqualAngleRad;
        }

        /// <summary>
        /// Compares the raw radian values of two angles. A larger rotation compares as greater, so a full
        /// turn is greater than a quarter turn rather than being treated as the same direction.
        /// </summary>
        /// <param name="other">The angle to compare with.</param>
        /// <returns>A signed number indicating the relative order of the two angles.</returns>
        public int CompareTo(Angle other) => Radians.CompareTo(other.Radians);

        #endregion

        #region Operators

        /// <summary>
        /// Adds two angles.
        /// </summary>
        public static Angle operator +(Angle left, Angle right) => left.Add(right);

        /// <summary>
        /// Subtracts one angle from another.
        /// </summary>
        public static Angle operator -(Angle left, Angle right) => left.Subtract(right);

        /// <summary>
        /// Negates an angle.
        /// </summary>
        public static Angle operator -(Angle angle) => angle.Negate();

        /// <summary>
        /// Multiplies an angle by a scalar.
        /// </summary>
        public static Angle operator *(Angle angle, double factor) => angle.Multiply(factor);

        /// <summary>
        /// Multiplies an angle by a scalar.
        /// </summary>
        public static Angle operator *(double factor, Angle angle) => angle.Multiply(factor);

        /// <summary>
        /// Divides an angle by a scalar.
        /// </summary>
        public static Angle operator /(Angle angle, double divisor) => angle.Divide(divisor);

        /// <summary>
        /// Compares two angles for exact equality.
        /// </summary>
        public static bool operator ==(Angle left, Angle right) => left.Equals(right);

        /// <summary>
        /// Compares two angles for exact inequality.
        /// </summary>
        public static bool operator !=(Angle left, Angle right) => !left.Equals(right);

        /// <summary>
        /// Determines whether one angle is smaller than another.
        /// </summary>
        public static bool operator <(Angle left, Angle right) => left.Radians < right.Radians;

        /// <summary>
        /// Determines whether one angle is greater than another.
        /// </summary>
        public static bool operator >(Angle left, Angle right) => left.Radians > right.Radians;

        /// <summary>
        /// Determines whether one angle is smaller than or equal to another.
        /// </summary>
        public static bool operator <=(Angle left, Angle right) => left.Radians <= right.Radians;

        /// <summary>
        /// Determines whether one angle is greater than or equal to another.
        /// </summary>
        public static bool operator >=(Angle left, Angle right) => left.Radians >= right.Radians;

        #endregion

        #region Equality

        /// <summary>
        /// Indicates whether the current angle is exactly equal to another angle. Use
        /// <see cref="IsEqualTo(Angle)"/> for a comparison that allows for tolerance.
        /// </summary>
        /// <param name="other">An angle to compare with this angle.</param>
        /// <returns>true if the two angles hold the same radian value; otherwise, false.</returns>
        public bool Equals(Angle other) => Radians.Equals(other.Radians);

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>true if obj is an Angle holding the same radian value; otherwise, false.</returns>
        public override bool Equals(object obj) => obj is Angle other && Equals(other);

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
        public override int GetHashCode() => Radians.GetHashCode();

        #endregion

        /// <summary>
        /// Returns the string representation of the angle in both units.
        /// </summary>
        /// <returns>A string such as "90.000 deg (1.5708 rad)".</returns>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:0.000} deg ({1:0.0000} rad)",
                Degrees,
                Radians);
        }
    }
}
