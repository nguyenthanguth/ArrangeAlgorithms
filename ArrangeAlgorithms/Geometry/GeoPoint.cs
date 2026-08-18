using System;

namespace ArrangeAlgorithms.Geometry
{
    /// <summary>
    /// Represents a 2D point with double precision coordinates.
    /// </summary>
    public readonly struct GeoPoint : IEquatable<GeoPoint>
    {
        /// <summary>
        /// Gets the X coordinate of the point.
        /// </summary>
        public double X { get; }

        /// <summary>
        /// Gets the Y coordinate of the point.
        /// </summary>
        public double Y { get; }

        /// <summary>
        /// Initializes a new point at the origin (0, 0).
        /// </summary>
        public GeoPoint()
        {
            X = 0.0;
            Y = 0.0;
        }

        /// <summary>
        /// Initializes a new point.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        public GeoPoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Initializes a new point from another point.
        /// </summary>
        /// <param name="geoPoint">Source point.</param>
        public GeoPoint(GeoPoint geoPoint)
        {
            X = geoPoint.X;
            Y = geoPoint.Y;
        }

        /// <summary>
        /// Adds a GeoVector to the point for translation.
        /// </summary>
        public GeoPoint Add(GeoVector geoVector)
        {
            return new GeoPoint(X + geoVector.X, Y + geoVector.Y);
        }

        /// <summary>
        /// Subtracts a GeoVector from the point.
        /// </summary>
        public GeoPoint Subtract(GeoVector geoVector)
        {
            return new GeoPoint(X - geoVector.X, Y - geoVector.Y);
        }

        /// <summary>
        /// Gets the GeoVector pointing from this point to another point.
        /// </summary>
        public GeoVector GetVectorTo(GeoPoint other)
        {
            return new GeoVector(other.X - X, other.Y - Y);
        }

        /// <summary>
        /// Calculates the Euclidean distance to another point.
        /// </summary>
        public double DistanceTo(GeoPoint other)
        {
            return Math.Sqrt(GetDistanceSquaredTo(other));
        }

        /// <summary>
        /// Calculates the squared Euclidean distance to another point.
        /// </summary>
        public double GetDistanceSquaredTo(GeoPoint other)
        {
            double dx = other.X - X;
            double dy = other.Y - Y;
            return dx * dx + dy * dy;
        }

        /// <summary>
        /// Gets the midpoint between this point and another point.
        /// </summary>
        public GeoPoint GetMiddlePoint(GeoPoint other)
        {
            return new GeoPoint((X + other.X) * 0.5, (Y + other.Y) * 0.5);
        }

        /// <summary>
        /// Rotates the point around a center with a specified rotation angle in radians (counter-clockwise).
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
        /// Scales the distance from the center to this point by a factor.
        /// </summary>
        public GeoPoint ScaleBy(double factor, GeoPoint center)
        {
            return new GeoPoint(
                center.X + (X - center.X) * factor,
                center.Y + (Y - center.Y) * factor);
        }

        /// <summary>
        /// Compares whether this point is coincident with another point within the allowed tolerance.
        /// </summary>
        public bool IsEqualTo(GeoPoint other, Tolerance tolerance)
        {
            return GetDistanceSquaredTo(other) <= tolerance.EqualPoint * tolerance.EqualPoint;
        }

        /// <summary>
        /// Compares whether this point is coincident with another point using default tolerance.
        /// </summary>
        public bool IsEqualTo(GeoPoint other) => IsEqualTo(other, Tolerance.Global);

        /// <summary>
        /// Translates a point by a vector.
        /// </summary>
        /// <param name="p">The point to translate.</param>
        /// <param name="v">The vector to apply.</param>
        /// <returns>A new GeoPoint representing the translated position.</returns>
        public static GeoPoint operator +(GeoPoint p, GeoVector v) => p.Add(v);

        /// <summary>
        /// Translates a point back by a vector.
        /// </summary>
        /// <param name="p">The point to translate.</param>
        /// <param name="v">The vector to subtract.</param>
        /// <returns>A new GeoPoint representing the translated position.</returns>
        public static GeoPoint operator -(GeoPoint p, GeoVector v) => p.Subtract(v);

        /// <summary>
        /// Calculates the vector from one point to another.
        /// </summary>
        /// <param name="p2">The destination point.</param>
        /// <param name="p1">The start point.</param>
        /// <returns>A GeoVector representing the direction and distance from p1 to p2.</returns>
        public static GeoVector operator -(GeoPoint p2, GeoPoint p1) => p1.GetVectorTo(p2);

        /// <summary>
        /// Indicates whether the current point is equal to another point.
        /// </summary>
        /// <param name="other">A point to compare with this point.</param>
        /// <returns>true if the current point is equal to the other parameter; otherwise, false.</returns>
        public bool Equals(GeoPoint other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>true if obj and this instance are the same type and represent the same value; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return obj is GeoPoint other && Equals(other);
        }

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
        /// Compares two GeoPoint instances for equality.
        /// </summary>
        /// <param name="left">The first point.</param>
        /// <param name="right">The second point.</param>
        /// <returns>true if they are equal; otherwise, false.</returns>
        public static bool operator ==(GeoPoint left, GeoPoint right) => left.Equals(right);

        /// <summary>
        /// Compares two GeoPoint instances for inequality.
        /// </summary>
        /// <param name="left">The first point.</param>
        /// <param name="right">The second point.</param>
        /// <returns>true if they are not equal; otherwise, false.</returns>
        public static bool operator !=(GeoPoint left, GeoPoint right) => !left.Equals(right);

        /// <summary>
        /// Returns the string representation of the point.
        /// </summary>
        /// <returns>A string representation formatted as (X, Y).</returns>
        public override string ToString() => $"({X:0.000}, {Y:0.000})";
    }
}
