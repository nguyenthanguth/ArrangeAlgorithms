using System;

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
        public GeoVector Add(GeoVector other)
        {
            return new GeoVector(X + other.X, Y + other.Y);
        }

        /// <summary>
        /// Subtracts another vector from this vector.
        /// </summary>
        public GeoVector Subtract(GeoVector other)
        {
            return new GeoVector(X - other.X, Y - other.Y);
        }

        /// <summary>
        /// Multiplies vector components by a scalar.
        /// </summary>
        public GeoVector Multiply(double scalar)
        {
            return new GeoVector(X * scalar, Y * scalar);
        }

        /// <summary>
        /// Calculates the dot product with another vector.
        /// </summary>
        public double DotProduct(GeoVector other)
        {
            return X * other.X + Y * other.Y;
        }

        /// <summary>
        /// Calculates the 2D cross product with another vector (returns the Z component).
        /// </summary>
        public double CrossProduct(GeoVector other)
        {
            return X * other.Y - Y * other.X;
        }

        /// <summary>
        /// Gets the perpendicular vector by rotating 90 degrees counter-clockwise.
        /// </summary>
        public GeoVector GetPerpendicularVector()
        {
            return new GeoVector(-Y, X);
        }

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
        public bool IsZeroLength()
        {
            return IsZeroLength(Tolerance.Global);
        }

        /// <summary>
        /// Checks whether the vector has zero length within tolerance.
        /// </summary>
        public bool IsZeroLength(Tolerance tolerance)
        {
            return LengthSquared <= tolerance.EqualVector * tolerance.EqualVector;
        }

        /// <summary>
        /// Compares whether this vector equals another vector using default tolerance.
        /// </summary>
        public bool IsEqualTo(GeoVector other)
        {
            return IsEqualTo(other, Tolerance.Global);
        }

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
        public bool IsParallelTo(GeoVector other)
        {
            return IsParallelTo(other, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether this vector is parallel to another vector.
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
        /// Checks whether this vector is perpendicular to another vector using default tolerance.
        /// </summary>
        public bool IsPerpendicularTo(GeoVector other)
        {
            return IsPerpendicularTo(other, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether this vector is perpendicular to another vector.
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
