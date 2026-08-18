using System;

namespace ArrangeAlgorithms.Geometry
{
    /// <summary>
    /// Represents a 2D line segment between start and end points.
    /// </summary>
    public readonly struct GeoLine : IEquatable<GeoLine>
    {
        /// <summary>
        /// Gets the start point of the line segment.
        /// </summary>
        public GeoPoint StartPoint { get; }

        /// <summary>
        /// Gets the end point of the line segment.
        /// </summary>
        public GeoPoint EndPoint { get; }

        /// <summary>
        /// Initializes a new GeoLine instance from start and end points.
        /// </summary>
        /// <param name="startPoint">Start point.</param>
        /// <param name="endPoint">End point.</param>
        public GeoLine(GeoPoint startPoint, GeoPoint endPoint)
        {
            StartPoint = startPoint;
            EndPoint = endPoint;
        }

        /// <summary>
        /// Initializes a new GeoLine instance from the coordinates of the endpoints.
        /// </summary>
        public GeoLine(double startX, double startY, double endX, double endY)
            : this(new GeoPoint(startX, startY), new GeoPoint(endX, endY))
        {
        }

        /// <summary>
        /// Gets the GeoVector pointing from start point to end point.
        /// </summary>
        public GeoVector Direction => StartPoint.GetVectorTo(EndPoint);

        /// <summary>
        /// Gets the length of the line segment.
        /// </summary>
        public double Length => StartPoint.DistanceTo(EndPoint);

        /// <summary>
        /// Gets the squared length of the line segment.
        /// </summary>
        public double LengthSquared => StartPoint.GetDistanceSquaredTo(EndPoint);

        /// <summary>
        /// Gets the midpoint of the line segment.
        /// </summary>
        public GeoPoint MidPoint => StartPoint.GetMiddlePoint(EndPoint);

        /// <summary>
        /// Gets the point on the line segment at parameter t (t=0 is the start point, t=1 is the end point).
        /// </summary>
        public GeoPoint GetPointAtParameter(double parameter)
        {
            return StartPoint.Add(Direction.Multiply(parameter));
        }

        /// <summary>
        /// Projects a point onto the line and gets the parameter value along the segment.
        /// </summary>
        public double GetParameterOf(GeoPoint GeoPoint)
        {
            GeoVector dir = Direction;
            double lenSq = dir.LengthSquared;
            if (lenSq <= Tolerance.Global.EqualPoint * Tolerance.Global.EqualPoint) return 0.0;
            return StartPoint.GetVectorTo(GeoPoint).DotProduct(dir) / lenSq;
        }

        /// <summary>
        /// Gets the closest point on the line segment to a given point.
        /// </summary>
        public GeoPoint GetClosestPointTo(GeoPoint GeoPoint)
        {
            double t = GetParameterOf(GeoPoint);
            if (t <= 0.0) return StartPoint;
            if (t >= 1.0) return EndPoint;
            return GetPointAtParameter(t);
        }

        /// <summary>
        /// Calculates the distance from a point to the closest point on this line segment.
        /// </summary>
        public double DistanceTo(GeoPoint GeoPoint)
        {
            return GeoPoint.DistanceTo(GetClosestPointTo(GeoPoint));
        }

        /// <summary>
        /// Checks whether a point lies on the line segment using default tolerance.
        /// </summary>
        public bool IsPointOn(GeoPoint GeoPoint)
        {
            return IsPointOn(GeoPoint, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether a point lies on the line segment within tolerance.
        /// </summary>
        public bool IsPointOn(GeoPoint GeoPoint, Tolerance tolerance)
        {
            return DistanceTo(GeoPoint) <= tolerance.EqualPoint;
        }

        /// <summary>
        /// Tries to calculate the intersection with another line segment using default tolerance.
        /// </summary>
        public bool TryIntersectWith(GeoLine other, out GeoPoint intersection)
        {
            return TryIntersectWith(other, out intersection, Tolerance.Global);
        }

        /// <summary>
        /// Tries to calculate the intersection with another line segment within tolerance.
        /// </summary>
        public bool TryIntersectWith(GeoLine other, out GeoPoint intersection, Tolerance tolerance)
        {
            intersection = new GeoPoint(0, 0);
            GeoVector r = Direction;
            GeoVector s = other.Direction;
            double rCrossS = r.CrossProduct(s);

            if (Math.Abs(rCrossS) <= tolerance.EqualPoint)
            {
                return false; // Parallel or collinear
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
        /// Checks whether this line segment intersects with another line segment using default tolerance.
        /// </summary>
        public bool IntersectsWith(GeoLine other)
        {
            return IntersectsWith(other, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether this line segment intersects with another line segment.
        /// </summary>
        public bool IntersectsWith(GeoLine other, Tolerance tolerance)
        {
            return TryIntersectWith(other, out _, tolerance);
        }

        /// <summary>
        /// Checks whether this line segment intersects with a rotated rectangle (GeoRectangle OBB) using default tolerance.
        /// </summary>
        public bool IntersectsWith(GeoRectangle rect)
        {
            return IntersectsWith(rect, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether this line segment intersects with a rotated rectangle (GeoRectangle OBB).
        /// </summary>
        public bool IntersectsWith(GeoRectangle rect, Tolerance tolerance)
        {
            return rect.IntersectsWith(this, tolerance);
        }

        /// <summary>
        /// Checks whether this line segment intersects with a polygon using default tolerance.
        /// </summary>
        public bool IntersectsWith(GeoPolygon poly)
        {
            return IntersectsWith(poly, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether this line segment intersects with a polygon.
        /// </summary>
        public bool IntersectsWith(GeoPolygon poly, Tolerance tolerance)
        {
            if (poly == null) throw new ArgumentNullException(nameof(poly));

            return poly.IntersectsWith(this, tolerance);
        }

        /// <summary>
        /// Indicates whether the current line segment is equal to another line segment.
        /// </summary>
        /// <param name="other">A line segment to compare with this line segment.</param>
        /// <returns>true if the current line segment is equal to the other parameter; otherwise, false.</returns>
        public bool Equals(GeoLine other)
        {
            return StartPoint.Equals(other.StartPoint) && EndPoint.Equals(other.EndPoint);
        }

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>true if obj and this instance are the same type and represent the same value; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return obj is GeoLine other && Equals(other);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (StartPoint.GetHashCode() * 397) ^ EndPoint.GetHashCode();
            }
        }

        /// <summary>
        /// Compares two GeoLine instances for equality.
        /// </summary>
        /// <param name="left">The first line segment.</param>
        /// <param name="right">The second line segment.</param>
        /// <returns>true if they are equal; otherwise, false.</returns>
        public static bool operator ==(GeoLine left, GeoLine right) => left.Equals(right);

        /// <summary>
        /// Compares two GeoLine instances for inequality.
        /// </summary>
        /// <param name="left">The first line segment.</param>
        /// <param name="right">The second line segment.</param>
        /// <returns>true if they are not equal; otherwise, false.</returns>
        public static bool operator !=(GeoLine left, GeoLine right) => !left.Equals(right);

        /// <summary>
        /// Returns the string representation of the line segment.
        /// </summary>
        /// <returns>A string representation of the start and end points of the line.</returns>
        public override string ToString() => $"GeoLine[{StartPoint} -> {EndPoint}]";
    }
}
