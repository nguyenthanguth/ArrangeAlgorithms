using System;
using ArrangeAlgorithms.Operations;

namespace ArrangeAlgorithms.Geometry
{
    /// <summary>
    /// Represents a 2D vector with double precision coordinates.
    /// </summary>
    public readonly struct GeoVector : IEquatable<GeoVector>
    {
        /// <summary>
        /// Gets the X coordinate of the vector.
        /// </summary>
        public double X { get; }

        /// <summary>
        /// Gets the Y coordinate of the vector.
        /// </summary>
        public double Y { get; }

        /// <summary>
        /// Gets the zero vector (0, 0).
        /// </summary>
        public static GeoVector Zero => new GeoVector(0.0, 0.0);

        /// <summary>
        /// Gets the unit vector along the X-axis (1, 0).
        /// </summary>
        public static GeoVector XAxis => new GeoVector(1.0, 0.0);

        /// <summary>
        /// Gets the unit vector along the Y-axis (0, 1).
        /// </summary>
        public static GeoVector YAxis => new GeoVector(0.0, 1.0);

        /// <summary>
        /// Initializes a new vector.
        /// </summary>
        /// <param name="x">X component.</param>
        /// <param name="y">Y component.</param>
        public GeoVector(double x, double y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Creates a copy of this vector.
        /// </summary>
        /// <remarks>
        /// Vector is a readonly struct, so plain assignment already produces an independent copy and
        /// this method is not needed to avoid sharing. It exists so that every geometry type offers the
        /// same way to ask for a copy.
        /// </remarks>
        /// <returns>A new vector with the same components.</returns>
        public GeoVector Clone() => new GeoVector(X, Y);

        /// <summary>
        /// Gets the length of the vector.
        /// </summary>
        public double Length => Math.Sqrt(LengthSquared);

        /// <summary>
        /// Gets the squared length of the vector.
        /// </summary>
        public double LengthSquared => X * X + Y * Y;

        /// <summary>
        /// Gets the normalized unit vector. Throws InvalidOperationException if the vector length is zero.
        /// </summary>
        public GeoVector Normalize()
        {
            if (!TryGetNormal(out GeoVector normal))
            {
                throw new InvalidOperationException("Cannot normalize a zero-length vector.");
            }
            return normal;
        }

        /// <summary>
        /// Tries to normalize the vector without throwing an exception using default tolerance.
        /// </summary>
        public bool TryGetNormal(out GeoVector normal)
        {
            return TryGetNormal(out normal, Tolerance.Global);
        }

        /// <summary>
        /// Tries to normalize the vector with a specific tolerance.
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
        /// Adds this vector to another vector.
        /// </summary>
        public GeoVector Add(GeoVector other) => new GeoVector(X + other.X, Y + other.Y);

        /// <summary>
        /// Subtracts another vector from this vector.
        /// </summary>
        public GeoVector Subtract(GeoVector other) => new GeoVector(X - other.X, Y - other.Y);

        /// <summary>
        /// Multiplies vector components by a scalar.
        /// </summary>
        public GeoVector Multiply(double scalar) => new GeoVector(X * scalar, Y * scalar);

        /// <summary>
        /// Calculates the dot product with another vector.
        /// </summary>
        public double DotProduct(GeoVector other) => X * other.X + Y * other.Y;

        /// <summary>
        /// Calculates the 2D cross product with another vector (returns the Z component).
        /// </summary>
        public double CrossProduct(GeoVector other) => X * other.Y - Y * other.X;

        /// <summary>
        /// Gets the perpendicular vector by rotating 90 degrees counter-clockwise.
        /// </summary>
        public GeoVector GetPerpendicularVector() => new GeoVector(-Y, X);

        /// <summary>
        /// Rotates the vector by an angle in radians (counter-clockwise).
        /// </summary>
        public GeoVector RotateBy(double angleRad)
        {
            double cos = Math.Cos(angleRad);
            double sin = Math.Sin(angleRad);
            return new GeoVector(X * cos - Y * sin, X * sin + Y * cos);
        }

        /// <summary>
        /// Gets the unsigned angle to another vector in radians, in the range [0, PI].
        /// </summary>
        public double GetAngleTo(GeoVector other)
        {
            if (IsZeroLength() || other.IsZeroLength())
            {
                throw new InvalidOperationException("Cannot calculate the angle to or from a zero-length vector.");
            }

            // Use Atan2(|cross|, dot) instead of Acos(dot). Acos loses accuracy severely when two
            // vectors are nearly collinear or nearly opposite: its derivative approaches infinity near
            // ±1, so rounding errors around 1e-16 of the dot product are amplified to angle errors around 1e-8.
            // Atan2 is stable across the entire range and does not require normalization: both arguments are proportional to |a|*|b|.
            return Math.Atan2(Math.Abs(CrossProduct(other)), DotProduct(other));
        }

        /// <summary>
        /// Gets the signed angle to another vector in radians, in the range (-PI, PI].
        /// </summary>
        public double GetSignedAngleTo(GeoVector other)
        {
            if (IsZeroLength() || other.IsZeroLength())
            {
                throw new InvalidOperationException("Cannot calculate the angle to or from a zero-length vector.");
            }

            return Math.Atan2(CrossProduct(other), DotProduct(other));
        }

        /// <summary>
        /// Checks whether the vector has zero length using default tolerance.
        /// </summary>
        public bool IsZeroLength() => IsZeroLength(Tolerance.Global);

        /// <summary>
        /// Checks whether the vector has zero length within tolerance.
        /// </summary>
        public bool IsZeroLength(Tolerance tolerance) => LengthSquared <= tolerance.EqualVector * tolerance.EqualVector;

        /// <summary>
        /// Compares whether this vector equals another vector using default tolerance.
        /// </summary>
        public bool IsEqualTo(GeoVector other) => IsEqualTo(other, Tolerance.Global);

        /// <summary>
        /// Compares whether this vector equals another vector within tolerance.
        /// </summary>
        public bool IsEqualTo(GeoVector other, Tolerance tolerance)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            return dx * dx + dy * dy <= tolerance.EqualVector * tolerance.EqualVector;
        }

        /// <summary>
        /// Checks whether this vector is parallel to another vector using default tolerance.
        /// </summary>
        public bool IsParallelTo(GeoVector other) => Parallel.IsParallel(this, other, Tolerance.Global);

        /// <summary>
        /// Checks whether this vector is parallel to another vector within angular tolerance.
        /// </summary>
        public bool IsParallelTo(GeoVector other, Tolerance tolerance) => Parallel.IsParallel(this, other, tolerance);

        /// <summary>
        /// Checks whether this vector is perpendicular to another vector using default tolerance.
        /// </summary>
        public bool IsPerpendicularTo(GeoVector other) => Parallel.IsPerpendicular(this, other, Tolerance.Global);

        /// <summary>
        /// Checks whether this vector is perpendicular to another vector within angular tolerance.
        /// </summary>
        public bool IsPerpendicularTo(GeoVector other, Tolerance tolerance) => Parallel.IsPerpendicular(this, other, tolerance);

        /// <summary>
        /// Checks whether this vector is parallel to a line segment using default tolerance.
        /// </summary>
        public bool IsParallelTo(GeoLine line) => Parallel.IsParallel(line, this, Tolerance.Global);

        /// <summary>
        /// Checks whether this vector is parallel to a line segment within angular tolerance.
        /// </summary>
        public bool IsParallelTo(GeoLine line, Tolerance tolerance) => Parallel.IsParallel(line, this, tolerance);

        /// <summary>
        /// Checks whether this vector is perpendicular to a line segment using default tolerance.
        /// </summary>
        public bool IsPerpendicularTo(GeoLine line) => Parallel.IsPerpendicular(line, this, Tolerance.Global);

        /// <summary>
        /// Checks whether this vector is perpendicular to a line segment within angular tolerance.
        /// </summary>
        public bool IsPerpendicularTo(GeoLine line, Tolerance tolerance) => Parallel.IsPerpendicular(line, this, tolerance);

        /// <summary>
        /// Projects this vector onto another axis vector.
        /// </summary>
        public GeoVector ProjectOnto(GeoVector axis) => Projection.Project(this, axis);

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="v1">The first vector.</param>
        /// <param name="v2">The second vector.</param>
        /// <returns>A new GeoVector representing the sum of the two vectors.</returns>
        public static GeoVector operator +(GeoVector v1, GeoVector v2) => v1.Add(v2);

        /// <summary>
        /// Subtracts one vector from another.
        /// </summary>
        /// <param name="v1">The vector to subtract from.</param>
        /// <param name="v2">The vector to subtract.</param>
        /// <returns>A new GeoVector representing the difference of the two vectors.</returns>
        public static GeoVector operator -(GeoVector v1, GeoVector v2) => v1.Subtract(v2);

        /// <summary>
        /// Negates a vector.
        /// </summary>
        /// <param name="v">The vector to negate.</param>
        /// <returns>A new GeoVector with negated coordinates.</returns>
        public static GeoVector operator -(GeoVector v) => new GeoVector(-v.X, -v.Y);

        /// <summary>
        /// Multiplies a vector by a scalar.
        /// </summary>
        /// <param name="v">The vector.</param>
        /// <param name="s">The scalar value.</param>
        /// <returns>A new GeoVector scaled by s.</returns>
        public static GeoVector operator *(GeoVector v, double s) => v.Multiply(s);

        /// <summary>
        /// Multiplies a vector by a scalar.
        /// </summary>
        /// <param name="s">The scalar value.</param>
        /// <param name="v">The vector.</param>
        /// <returns>A new GeoVector scaled by s.</returns>
        public static GeoVector operator *(double s, GeoVector v) => v.Multiply(s);

        /// <summary>
        /// Divides a vector by a scalar.
        /// </summary>
        /// <param name="v">The vector.</param>
        /// <param name="s">The scalar value.</param>
        /// <returns>A new GeoVector divided by s.</returns>
        public static GeoVector operator /(GeoVector v, double s) => new GeoVector(v.X / s, v.Y / s);

        /// <summary>
        /// Indicates whether the current vector is equal to another vector.
        /// </summary>
        /// <param name="other">A vector to compare with this vector.</param>
        /// <returns>true if the current vector is equal to the other parameter; otherwise, false.</returns>
        public bool Equals(GeoVector other) => X.Equals(other.X) && Y.Equals(other.Y);

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>true if obj and this instance are the same type and represent the same value; otherwise, false.</returns>
        public override bool Equals(object obj) => obj is GeoVector other && Equals(other);

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        /// <summary>
        /// Compares two GeoVector instances for equality.
        /// </summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>true if they are equal; otherwise, false.</returns>
        public static bool operator ==(GeoVector left, GeoVector right) => left.Equals(right);

        /// <summary>
        /// Compares two GeoVector instances for inequality.
        /// </summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>true if they are not equal; otherwise, false.</returns>
        public static bool operator !=(GeoVector left, GeoVector right) => !left.Equals(right);

        /// <summary>
        /// Returns the string representation of the vector.
        /// </summary>
        /// <returns>A string representation formatted as (X, Y).</returns>
        public override string ToString() => $"({X:0.000}, {Y:0.000})";
    }
}
