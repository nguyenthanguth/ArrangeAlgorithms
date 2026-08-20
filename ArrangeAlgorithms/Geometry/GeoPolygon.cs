using System;
using System.Collections.Generic;
using System.Linq;
using ArrangeAlgorithms.Core;
using ArrangeAlgorithms.Enums;

namespace ArrangeAlgorithms.Geometry
{
    /// <summary>
    /// Represents a 2D closed polygon.
    /// <para>
    /// A polygon is expected to be simple — no edge crossing another — but this is not checked, because
    /// checking costs more than every other thing the constructor does put together. A polygon whose
    /// edges do cross is still usable rather than undefined: containment reads it under the even-odd
    /// rule, so a region enclosed an even number of times counts as outside, and every operation built
    /// on containment follows suit. What does not survive is area. <see cref="GetSignedArea"/> sums
    /// lobes with their own signs, so a figure-eight reports zero however large its lobes are, and
    /// <see cref="GetArea"/> reports zero with it.
    /// </para>
    /// </summary>
    public sealed class GeoPolygon : IEquatable<GeoPolygon>
    {
        private readonly GeoPoint[] _vertices;

        /// <summary>
        /// Gets the read-only list of vertices of the polygon.
        /// </summary>
        public IReadOnlyList<GeoPoint> Vertices => _vertices;

        /// <summary>
        /// Gets the number of vertices of the polygon.
        /// </summary>
        public int VertexCount => _vertices.Length;

        /// <summary>
        /// Gets the number of edges of the polygon.
        /// </summary>
        public int EdgeCount => _vertices.Length;

        /// <summary>
        /// Gets the total perimeter length of the polygon.
        /// </summary>
        public double Length
        {
            get
            {
                double total = 0.0;
                for (int i = 0; i < EdgeCount; i++)
                {
                    total += GetEdgeAt(i).Length;
                }
                return total;
            }
        }

        /// <summary>
        /// Initializes a new polygon from a list of vertices. Duplicate vertex at the end to close the loop will be automatically removed.
        /// </summary>
        /// <param name="vertices">List of vertices.</param>
        public GeoPolygon(IEnumerable<GeoPoint> vertices)
        {
            if (vertices == null) throw new ArgumentNullException(nameof(vertices));

            // Drop consecutive duplicates within tolerance so no zero-length edge survives. GeoPolyline
            // filters the same way; comparing exactly would let (0,0),(0,0),(1,1) through as a valid
            // polygon despite the promise of distinct vertices below.
            List<GeoPoint> list = new List<GeoPoint>();
            foreach (var vertex in vertices)
            {
                if (list.Count == 0 || !list[list.Count - 1].IsEqualTo(vertex))
                {
                    list.Add(vertex);
                }
            }

            // Remove duplicate last vertex if it closes the loop back onto the first vertex
            while (list.Count > 1 && list[list.Count - 1].IsEqualTo(list[0]))
            {
                list.RemoveAt(list.Count - 1);
            }

            if (list.Count < 3)
            {
                throw new ArgumentException("A polygon must have at least 3 distinct vertices.");
            }

            _vertices = list.ToArray();
        }

        /// <summary>
        /// Initializes a new polygon directly from parameter vertices.
        /// </summary>
        public GeoPolygon(params GeoPoint[] vertices) : this((IEnumerable<GeoPoint>)vertices)
        {
        }

        /// <summary>
        /// Initializes a polygon from vertices that have already been filtered and validated.
        /// </summary>
        /// <param name="validatedVertices">Source array, already free of consecutive duplicates.</param>
        /// <param name="count">Number of leading entries to copy.</param>
        /// <remarks>
        /// Clone uses this instead of the public constructor. The public one re-filters vertices against
        /// Tolerance.Global, so a clone taken after that global was widened could silently come back with
        /// fewer vertices than the original, or fail validation outright.
        /// </remarks>
        private GeoPolygon(GeoPoint[] validatedVertices, int count)
        {
            _vertices = new GeoPoint[count];
            Array.Copy(validatedVertices, _vertices, count);
        }

        /// <summary>
        /// Gets the vertex at a given index.
        /// </summary>
        public GeoPoint this[int index]
        {
            get
            {
                if (index < 0 || index >= _vertices.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                return _vertices[index];
            }
        }

        /// <summary>
        /// Gets the line segment edge of the polygon at a given index.
        /// </summary>
        public GeoLine GetEdgeAt(int index)
        {
            if (index < 0 || index >= EdgeCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            return new GeoLine(_vertices[index], _vertices[(index + 1) % _vertices.Length]);
        }

        /// <summary>
        /// Enumerates all edges of the polygon.
        /// </summary>
        public IEnumerable<GeoLine> GetEdges()
        {
            for (int i = 0; i < EdgeCount; i++)
            {
                yield return GetEdgeAt(i);
            }
        }

        /// <summary>
        /// Calculates the signed area of the polygon using the Shoelace formula.
        /// Positive if vertices are counter-clockwise (CCW), negative if clockwise (CW).
        /// </summary>
        public double GetSignedArea()
        {
            double area = 0.0;
            int n = _vertices.Length;
            for (int i = 0; i < n; i++)
            {
                GeoPoint p1 = _vertices[i];
                GeoPoint p2 = _vertices[(i + 1) % n];
                area += p1.X * p2.Y - p2.X * p1.Y;
            }
            return area * 0.5;
        }

        /// <summary>
        /// Calculates the absolute area of the polygon.
        /// </summary>
        public double GetArea() => Math.Abs(GetSignedArea());

        /// <summary>
        /// Checks whether the polygon is oriented clockwise.
        /// </summary>
        public bool IsClockwise() => GetSignedArea() < 0.0;

        /// <summary>
        /// Calculates the geometric centroid of the polygon.
        /// </summary>
        public GeoPoint GetCentroid()
        {
            double signedArea = GetSignedArea();
            if (Math.Abs(signedArea) <= Tolerance.Global.EqualPoint * Tolerance.Global.EqualPoint)
            {
                // Fallback: Calculate the average of vertex coordinates if the polygon is degenerate
                double sumX = 0.0;
                double sumY = 0.0;
                foreach (var v in _vertices)
                {
                    sumX += v.X;
                    sumY += v.Y;
                }
                return new GeoPoint(sumX / _vertices.Length, sumY / _vertices.Length);
            }

            double cx = 0.0;
            double cy = 0.0;
            int n = _vertices.Length;
            for (int i = 0; i < n; i++)
            {
                GeoPoint p1 = _vertices[i];
                GeoPoint p2 = _vertices[(i + 1) % n];
                double factor = p1.X * p2.Y - p2.X * p1.Y;
                cx += (p1.X + p2.X) * factor;
                cy += (p1.Y + p2.Y) * factor;
            }

            double areaFactor = 1.0 / (6.0 * signedArea);
            return new GeoPoint(cx * areaFactor, cy * areaFactor);
        }

        /// <summary>
        /// Creates a copy of this polygon holding its own vertex array.
        /// </summary>
        /// <returns>A new GeoPolygon independent of this one.</returns>
        public GeoPolygon Clone() => new GeoPolygon(_vertices, _vertices.Length);

        /// <summary>
        /// Converts this polygon's boundary into a closed 2D GeoPolyline.
        /// The boundary is closed by repeating the first vertex at the end of the chain.
        /// </summary>
        /// <returns>A new GeoPolyline instance representing the polygon boundary.</returns>
        public GeoPolyline ToPolyline()
        {
            var polylineVertices = new GeoPoint[_vertices.Length + 1];
            Array.Copy(_vertices, polylineVertices, _vertices.Length);
            polylineVertices[_vertices.Length] = _vertices[0];
            return new GeoPolyline(polylineVertices);
        }

        /// <summary>
        /// Gets the point at a normalized parameter along this polygon perimeter, where 0 is the first vertex and 1 is the end.
        /// Values outside [0, 1] wrap around, so 1.25 is the same position as 0.25.
        /// </summary>
        public GeoPoint GetPointAtParameter(double parameter) => Parametrization.GetPointAtParameter(this, parameter);

        /// <summary>
        /// Gets the normalized parameter of the point on this polygon perimeter closest to the supplied point.
        /// </summary>
        public double GetParameterAtPoint(GeoPoint point) => Parametrization.GetParameterAtPoint(this, point);

        /// <summary>
        /// Gets the point at an arc length measured from the first vertex of this polygon perimeter.
        /// </summary>
        public GeoPoint GetPointAtDistance(double distance) => Parametrization.GetPointAtDistance(this, distance);

        /// <summary>
        /// Gets the arc length from the first vertex of this polygon perimeter to the point on it closest to the supplied point.
        /// </summary>
        public double GetDistanceAtPoint(GeoPoint point) => Parametrization.GetDistanceAtPoint(this, point);

        /// <summary>
        /// Gets the arc length from the first vertex of this polygon perimeter to a normalized parameter.
        /// </summary>
        public double GetDistanceAtParameter(double parameter) => Parametrization.GetDistanceAtParameter(this, parameter);

        /// <summary>
        /// Gets the normalized parameter at an arc length measured from the first vertex of this polygon perimeter.
        /// </summary>
        public double GetParameterAtDistance(double distance) => Parametrization.GetParameterAtDistance(this, distance);

        /// <summary>
        /// Translates the polygon by a displacement vector.
        /// </summary>
        /// <param name="vector">The displacement vector.</param>
        /// <returns>A new translated GeoPolygon.</returns>
        public GeoPolygon Translate(GeoVector vector)
        {
            GeoPoint[] moved = new GeoPoint[_vertices.Length];
            for (int i = 0; i < _vertices.Length; i++)
            {
                moved[i] = _vertices[i].Add(vector);
            }
            return new GeoPolygon(moved);
        }

        /// <summary>
        /// Rotates the polygon around a center point by an angle in radians (counter-clockwise).
        /// </summary>
        /// <param name="angleRad">Rotation angle in radians.</param>
        /// <param name="center">Center of rotation.</param>
        /// <returns>A new rotated GeoPolygon.</returns>
        public GeoPolygon RotateBy(double angleRad, GeoPoint center)
        {
            GeoPoint[] rotated = new GeoPoint[_vertices.Length];
            for (int i = 0; i < _vertices.Length; i++)
            {
                rotated[i] = _vertices[i].RotateBy(angleRad, center);
            }
            return new GeoPolygon(rotated);
        }

        /// <summary>
        /// Calculates the shortest boundary distance from this polygon to a circle.
        /// </summary>
        public double DistanceTo(GeoCircle circle) => Distance.DistanceTo(circle, this);

        /// <summary>
        /// Calculates the shortest boundary distance from this polygon to a polyline.
        /// </summary>
        public double DistanceTo(GeoPolyline polyline) => Distance.DistanceTo(polyline, this);

        /// <summary>
        /// Calculates the shortest boundary distance from this polygon to another polygon.
        /// </summary>
        public double DistanceTo(GeoPolygon other) => Distance.DistanceTo(this, other);

        /// <summary>
        /// Calculates the shortest boundary distance from this polygon to a rectangle.
        /// </summary>
        public double DistanceTo(GeoRectangle rect) => Distance.DistanceTo(rect, this);

        /// <summary>
        /// Calculates the shortest boundary distance from this polygon to a line segment.
        /// </summary>
        public double DistanceTo(GeoLine line) => Distance.DistanceTo(this, line);

        /// <summary>
        /// Calculates the shortest distance from this polygon boundary to a point.
        /// </summary>
        public double DistanceTo(GeoPoint point) => Distance.DistanceTo(this, point);

        /// <summary>
        /// Gets the closest point on the boundary of this polygon to a target point, including for points
        /// inside the polygon.
        /// </summary>
        public GeoPoint GetClosestPointOnBoundary(GeoPoint point) => Projection.ProjectToPolygon(this, point);

        /// <summary>
        /// Checks whether the polygon contains a point using default tolerance (accepts points on the boundary).
        /// </summary>
        public bool Contains(GeoPoint GeoPoint) => Contains(GeoPoint, Tolerance.Global);

        /// <summary>
        /// Checks whether the polygon contains a point (accepts points on the boundary).
        /// Uses the Ray Casting algorithm (shoots a horizontal ray and counts intersections).
        /// </summary>
        public bool Contains(GeoPoint GeoPoint, Tolerance tolerance) => Containment.Contains(this, GeoPoint, tolerance);

        /// <summary>
        /// Classifies the location of a point relative to this polygon (Inside, OutSide, or OnSide) using default tolerance.
        /// </summary>
        public PointLocation Locate(GeoPoint point) => Containment.Locate(this, point, Tolerance.Global);

        /// <summary>
        /// Classifies the location of a point relative to this polygon (Inside, OutSide, or OnSide) within tolerance.
        /// </summary>
        public PointLocation Locate(GeoPoint point, Tolerance tolerance) => Containment.Locate(this, point, tolerance);

        /// <summary>
        /// Checks whether the polygon entirely contains a polyline using default tolerance.
        /// </summary>
        public bool Contains(GeoPolyline polyline) => Containment.Contains(this, polyline, Tolerance.Global);

        /// <summary>
        /// Checks whether the polygon entirely contains a polyline within tolerance.
        /// </summary>
        public bool Contains(GeoPolyline polyline, Tolerance tolerance) => Containment.Contains(this, polyline, tolerance);

        /// <summary>
        /// Checks whether the polygon collides with a rotated rectangle using default tolerance.
        /// </summary>
        public bool CollidesWith(GeoRectangle rect) => Collision.CollidesWith(rect, this, Tolerance.Global);

        /// <summary>
        /// Checks whether the polygon collides with a rotated rectangle within tolerance.
        /// </summary>
        public bool CollidesWith(GeoRectangle rect, Tolerance tolerance) => Collision.CollidesWith(rect, this, tolerance);

        /// <summary>
        /// Checks whether the polygon collides with a line segment using default tolerance.
        /// </summary>
        public bool CollidesWith(GeoLine geoLine) => Collision.CollidesWith(this, geoLine, Tolerance.Global);

        /// <summary>
        /// Checks whether the polygon collides with a line segment within tolerance.
        /// </summary>
        public bool CollidesWith(GeoLine geoLine, Tolerance tolerance) => Collision.CollidesWith(this, geoLine, tolerance);

        /// <summary>
        /// Checks whether the polygon collides with another polygon using default tolerance.
        /// </summary>
        public bool CollidesWith(GeoPolygon other) => Collision.CollidesWith(this, other, Tolerance.Global);

        /// <summary>
        /// Checks whether the polygon collides with another polygon within tolerance.
        /// </summary>
        public bool CollidesWith(GeoPolygon other, Tolerance tolerance) => Collision.CollidesWith(this, other, tolerance);

        /// <summary>
        /// Checks whether the polygon collides with a circle using default tolerance.
        /// </summary>
        public bool CollidesWith(GeoCircle circle) => Collision.CollidesWith(circle, this, Tolerance.Global);

        /// <summary>
        /// Checks whether the polygon collides with a circle within tolerance.
        /// </summary>
        public bool CollidesWith(GeoCircle circle, Tolerance tolerance) => Collision.CollidesWith(circle, this, tolerance);

        /// <summary>
        /// Checks whether the polygon collides with a polyline using default tolerance.
        /// </summary>
        public bool CollidesWith(GeoPolyline polyline) => Collision.CollidesWith(polyline, this, Tolerance.Global);

        /// <summary>
        /// Checks whether the polygon collides with a polyline within tolerance.
        /// </summary>
        public bool CollidesWith(GeoPolyline polyline, Tolerance tolerance) => Collision.CollidesWith(polyline, this, tolerance);

        /// <summary>
        /// Gets all intersection points with a line segment using default tolerance.
        /// </summary>
        public GeoPoint[] GetIntersections(GeoLine line) => Intersection.GetIntersections(this, line, Tolerance.Global);

        /// <summary>
        /// Gets all intersection points with a line segment within tolerance.
        /// </summary>
        public GeoPoint[] GetIntersections(GeoLine line, Tolerance tolerance) => Intersection.GetIntersections(this, line, tolerance);

        /// <summary>
        /// Gets all intersection points with another polygon using default tolerance.
        /// </summary>
        public GeoPoint[] GetIntersections(GeoPolygon other) => Intersection.GetIntersections(this, other, Tolerance.Global);

        /// <summary>
        /// Gets all intersection points with another polygon within tolerance.
        /// </summary>
        public GeoPoint[] GetIntersections(GeoPolygon other, Tolerance tolerance) => Intersection.GetIntersections(this, other, tolerance);

        /// <summary>
        /// Gets all intersection points with a rectangle using default tolerance.
        /// </summary>
        public GeoPoint[] GetIntersections(GeoRectangle rect) => Intersection.GetIntersections(this, rect, Tolerance.Global);

        /// <summary>
        /// Gets all intersection points with a rectangle within tolerance.
        /// </summary>
        public GeoPoint[] GetIntersections(GeoRectangle rect, Tolerance tolerance) => Intersection.GetIntersections(this, rect, tolerance);

        /// <summary>
        /// Gets all intersection points with a circle using default tolerance.
        /// </summary>
        public GeoPoint[] GetIntersections(GeoCircle circle) => Intersection.GetIntersections(this, circle, Tolerance.Global);

        /// <summary>
        /// Gets all intersection points with a circle within tolerance.
        /// </summary>
        public GeoPoint[] GetIntersections(GeoCircle circle, Tolerance tolerance) => Intersection.GetIntersections(this, circle, tolerance);

        /// <summary>
        /// Gets all intersection points with a polyline using default tolerance.
        /// </summary>
        public GeoPoint[] GetIntersections(GeoPolyline polyline) => Intersection.GetIntersections(polyline, this, Tolerance.Global);

        /// <summary>
        /// Gets all intersection points with a polyline within tolerance.
        /// </summary>
        public GeoPoint[] GetIntersections(GeoPolyline polyline, Tolerance tolerance) => Intersection.GetIntersections(polyline, this, tolerance);

        /// <summary>
        /// Translates a polygon by a vector.
        /// </summary>
        public static GeoPolygon operator +(GeoPolygon poly, GeoVector vector) => poly.Translate(vector);

        /// <summary>
        /// Translates a polygon backwards by a vector.
        /// </summary>
        public static GeoPolygon operator -(GeoPolygon poly, GeoVector vector) => poly.Translate(-vector);

        /// <summary>
        /// Indicates whether the current polygon is equal to another polygon by comparing vertex arrays in order.
        /// </summary>
        /// <param name="other">A polygon to compare with this polygon.</param>
        /// <returns>true if the current polygon is equal to the other parameter; otherwise, false.</returns>
        public bool Equals(GeoPolygon other)
        {
            if (other == null || other.VertexCount != VertexCount) return false;
            for (int i = 0; i < _vertices.Length; i++)
            {
                if (!_vertices[i].Equals(other._vertices[i]))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>true if obj and this instance are the same type and represent the same value; otherwise, false.</returns>
        public override bool Equals(object obj) => obj is GeoPolygon other && Equals(other);

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                foreach (var v in _vertices)
                {
                    hash = hash * 31 + v.GetHashCode();
                }
                return hash;
            }
        }

        /// <summary>
        /// Compares two GeoPolygon instances for equality.
        /// </summary>
        /// <param name="left">The first polygon.</param>
        /// <param name="right">The second polygon.</param>
        /// <returns>true if they are equal; otherwise, false.</returns>
        public static bool operator ==(GeoPolygon left, GeoPolygon right) => Equals(left, right);

        /// <summary>
        /// Compares two GeoPolygon instances for inequality.
        /// </summary>
        /// <param name="left">The first polygon.</param>
        /// <param name="right">The second polygon.</param>
        /// <returns>true if they are not equal; otherwise, false.</returns>
        public static bool operator !=(GeoPolygon left, GeoPolygon right) => !Equals(left, right);

        /// <summary>
        /// Returns the string representation of the polygon.
        /// </summary>
        /// <returns>A string representation of vertex count and area.</returns>
        public override string ToString() => $"GeoPolygon[{VertexCount} vertices, Area:{GetArea():0.000}]";
    }
}
