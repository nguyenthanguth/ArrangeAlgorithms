using System;
using System.Collections.Generic;
using System.Linq;
using ArrangeAlgorithms.Operations;
using ArrangeAlgorithms.Enums;

namespace ArrangeAlgorithms.Geometry
{
    /// <summary>
    /// Represents a 2D polyline consisting of a sequence of connected line segments, which can be open or closed.
    /// </summary>
    public sealed class GeoPolyline : IEquatable<GeoPolyline>
    {
        private readonly GeoPoint[] _vertices;

        /// <summary>
        /// Gets the read-only list of vertices of the polyline.
        /// </summary>
        public IReadOnlyList<GeoPoint> Vertices => _vertices;

        /// <summary>
        /// Gets the number of vertices.
        /// </summary>
        public int VertexCount => _vertices.Length;

        /// <summary>
        /// Gets a value indicating whether the polyline is closed.
        /// </summary>
        public bool IsClosed { get; }

        /// <summary>
        /// Gets the number of line segment edges in the polyline.
        /// </summary>
        public int EdgeCount => IsClosed ? _vertices.Length : Math.Max(0, _vertices.Length - 1);

        /// <summary>
        /// Gets the total cumulative length of all segments in the polyline.
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
        /// Initializes a new GeoPolyline instance from a collection of vertices.
        /// Consecutive duplicate vertices are automatically filtered out.
        /// </summary>
        /// <param name="vertices">The sequence of vertices.</param>
        /// <param name="isClosed">Indicates whether the polyline should be closed (connecting the last vertex to the first).</param>
        public GeoPolyline(IEnumerable<GeoPoint> vertices, bool isClosed = false)
        {
            if (vertices == null) throw new ArgumentNullException(nameof(vertices));

            List<GeoPoint> list = new List<GeoPoint>();
            foreach (var pt in vertices)
            {
                if (list.Count == 0 || !list[list.Count - 1].IsEqualTo(pt))
                {
                    list.Add(pt);
                }
            }

            if (isClosed && list.Count > 1 && list[list.Count - 1].IsEqualTo(list[0]))
            {
                list.RemoveAt(list.Count - 1);
            }

            if (list.Count < 2)
            {
                throw new ArgumentException("A polyline must have at least 2 distinct vertices.");
            }

            if (isClosed && list.Count < 3)
            {
                throw new ArgumentException("A closed polyline must have at least 3 distinct vertices.");
            }

            _vertices = list.ToArray();
            IsClosed = isClosed;
        }

        /// <summary>
        /// Initializes a new open GeoPolyline from parameter vertices.
        /// </summary>
        /// <param name="vertices">The sequence of vertices.</param>
        public GeoPolyline(params GeoPoint[] vertices)
            : this((IEnumerable<GeoPoint>)vertices, false)
        {
        }

        /// <summary>
        /// Initializes a new GeoPolyline with a specified closed flag and parameter vertices.
        /// </summary>
        /// <param name="isClosed">Indicates whether the polyline is closed.</param>
        /// <param name="vertices">The sequence of vertices.</param>
        public GeoPolyline(bool isClosed, params GeoPoint[] vertices)
            : this((IEnumerable<GeoPoint>)vertices, isClosed)
        {
        }

        /// <summary>
        /// Initializes a polyline from vertices that have already been filtered and validated.
        /// </summary>
        /// <param name="validatedVertices">Source array, already free of consecutive duplicates.</param>
        /// <param name="count">Number of leading entries to copy.</param>
        /// <param name="isClosed">Whether the polyline is closed.</param>
        /// <remarks>
        /// Clone uses this instead of the public constructor. The public one re-filters vertices against
        /// Tolerance.Global, so a clone taken after that global was widened could silently come back with
        /// fewer vertices than the original, or fail validation outright.
        /// </remarks>
        private GeoPolyline(GeoPoint[] validatedVertices, int count, bool isClosed)
        {
            _vertices = new GeoPoint[count];
            Array.Copy(validatedVertices, _vertices, count);
            IsClosed = isClosed;
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
        /// Gets the line segment edge at a given index.
        /// </summary>
        /// <param name="index">The 0-based edge index.</param>
        /// <returns>The line segment edge.</returns>
        public GeoLine GetEdgeAt(int index)
        {
            if (index < 0 || index >= EdgeCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            return new GeoLine(_vertices[index], _vertices[(index + 1) % _vertices.Length]);
        }

        /// <summary>
        /// Enumerates all line segment edges of the polyline.
        /// </summary>
        public IEnumerable<GeoLine> GetEdges()
        {
            for (int i = 0; i < EdgeCount; i++)
            {
                yield return GetEdgeAt(i);
            }
        }


        /// <summary>
        /// Gets the point at a normalized parameter along this polyline, where 0 is the first vertex and 1
        /// is the end. A closed polyline wraps values outside [0, 1]; an open one clamps them.
        /// </summary>
        public GeoPoint GetPointAtParameter(double parameter) => Parametrization.GetPointAtParameter(this, parameter);

        /// <summary>
        /// Gets the normalized parameter of the point on this polyline closest to the supplied point.
        /// </summary>
        public double GetParameterAtPoint(GeoPoint point) => Parametrization.GetParameterAtPoint(this, point);

        /// <summary>
        /// Gets the point at an arc length measured from the first vertex of this polyline.
        /// </summary>
        public GeoPoint GetPointAtDistance(double distance) => Parametrization.GetPointAtDistance(this, distance);

        /// <summary>
        /// Gets the arc length from the first vertex of this polyline to the point on it closest to the
        /// supplied point.
        /// </summary>
        public double GetDistanceAtPoint(GeoPoint point) => Parametrization.GetDistanceAtPoint(this, point);

        /// <summary>
        /// Gets the arc length from the first vertex of this polyline to a normalized parameter.
        /// </summary>
        public double GetDistanceAtParameter(double parameter) => Parametrization.GetDistanceAtParameter(this, parameter);

        /// <summary>
        /// Gets the normalized parameter at an arc length measured from the first vertex of this polyline.
        /// </summary>
        public double GetParameterAtDistance(double distance) => Parametrization.GetParameterAtDistance(this, distance);

        /// <summary>
        /// Reverses the direction of the polyline.
        /// </summary>
        /// <returns>A new reversed GeoPolyline.</returns>
        public GeoPolyline Reverse()
        {
            GeoPoint[] rev = new GeoPoint[_vertices.Length];
            Array.Copy(_vertices, rev, _vertices.Length);
            Array.Reverse(rev);
            return new GeoPolyline(rev, IsClosed);
        }

        /// <summary>
        /// Creates a copy of this polyline holding its own vertex array.
        /// </summary>
        /// <returns>A new GeoPolyline independent of this one, with the same closed flag.</returns>
        public GeoPolyline Clone() => new GeoPolyline(_vertices, _vertices.Length, IsClosed);

        /// <summary>
        /// Translates the polyline by a displacement vector.
        /// </summary>
        /// <param name="vector">The displacement vector.</param>
        /// <returns>A new translated GeoPolyline.</returns>
        public GeoPolyline Translate(GeoVector vector)
        {
            GeoPoint[] moved = new GeoPoint[_vertices.Length];
            for (int i = 0; i < _vertices.Length; i++)
            {
                moved[i] = _vertices[i].Add(vector);
            }
            return new GeoPolyline(moved, IsClosed);
        }

        /// <summary>
        /// Rotates the polyline around a center point by an angle in radians (counter-clockwise).
        /// </summary>
        /// <param name="angleRad">Rotation angle in radians.</param>
        /// <param name="center">Center of rotation.</param>
        /// <returns>A new rotated GeoPolyline.</returns>
        public GeoPolyline RotateBy(double angleRad, GeoPoint center)
        {
            GeoPoint[] rotated = new GeoPoint[_vertices.Length];
            for (int i = 0; i < _vertices.Length; i++)
            {
                rotated[i] = _vertices[i].RotateBy(angleRad, center);
            }
            return new GeoPolyline(rotated, IsClosed);
        }

        /// <summary>
        /// Converts this polyline into a solid 2D GeoPolygon.
        /// If the polyline is open, it will be automatically closed by connecting the last vertex to the first.
        /// </summary>
        /// <returns>A new GeoPolygon instance.</returns>
        public GeoPolygon ToPolygon() => new GeoPolygon(_vertices);

        /// <summary>
        /// Calculates the shortest Euclidean distance from this polyline to a point.
        /// </summary>
        public double DistanceTo(GeoPoint point) => Distance.DistanceTo(this, point);

        /// <summary>
        /// Calculates the shortest distance from this polyline to a line segment.
        /// </summary>
        public double DistanceTo(GeoLine line) => Distance.DistanceTo(this, line);

        /// <summary>
        /// Calculates the shortest distance from this polyline to a rectangle.
        /// </summary>
        public double DistanceTo(GeoRectangle rect) => Distance.DistanceTo(this, rect);

        /// <summary>
        /// Calculates the shortest distance from this polyline to a circle.
        /// </summary>
        public double DistanceTo(GeoCircle circle) => Distance.DistanceTo(this, circle);

        /// <summary>
        /// Calculates the shortest distance from this polyline to a polygon.
        /// </summary>
        public double DistanceTo(GeoPolygon poly) => Distance.DistanceTo(this, poly);

        /// <summary>
        /// Calculates the shortest distance between two polylines.
        /// </summary>
        public double DistanceTo(GeoPolyline other) => Distance.DistanceTo(this, other);

        /// <summary>
        /// Gets the closest point on the path of this polyline to a target point, including for points
        /// inside a closed polyline.
        /// </summary>
        public GeoPoint GetClosestPointOnBoundary(GeoPoint point) => Projection.ProjectToPolyline(this, point);

        /// <summary>
        /// Checks whether a point lies on this polyline using default tolerance.
        /// </summary>
        public bool IsPointOn(GeoPoint point) => Containment.IsPointOn(this, point);

        /// <summary>
        /// Checks whether a point lies on this polyline within tolerance.
        /// </summary>
        public bool IsPointOn(GeoPoint point, Tolerance tolerance) => Containment.IsPointOn(this, point, tolerance);

        /// <summary>
        /// Classifies the location of a point relative to this polyline (Inside, OutSide, or OnSide) using default tolerance.
        /// </summary>
        public PointLocation Locate(GeoPoint point) => Containment.Locate(this, point, Tolerance.Global);

        /// <summary>
        /// Classifies the location of a point relative to this polyline (Inside, OutSide, or OnSide) within tolerance.
        /// </summary>
        public PointLocation Locate(GeoPoint point, Tolerance tolerance) => Containment.Locate(this, point, tolerance);

        /// <summary>
        /// Checks whether this polyline collides with a line segment using default tolerance.
        /// </summary>
        public bool CollidesWith(GeoLine line) => Collision.CollidesWith(this, line, Tolerance.Global);

        /// <summary>
        /// Checks whether this polyline collides with a line segment within tolerance.
        /// </summary>
        public bool CollidesWith(GeoLine line, Tolerance tolerance) => Collision.CollidesWith(this, line, tolerance);

        /// <summary>
        /// Checks whether this polyline collides with a rectangle using default tolerance.
        /// </summary>
        public bool CollidesWith(GeoRectangle rect) => Collision.CollidesWith(this, rect, Tolerance.Global);

        /// <summary>
        /// Checks whether this polyline collides with a rectangle within tolerance.
        /// </summary>
        public bool CollidesWith(GeoRectangle rect, Tolerance tolerance) => Collision.CollidesWith(this, rect, tolerance);

        /// <summary>
        /// Checks whether this polyline collides with a circle using default tolerance.
        /// </summary>
        public bool CollidesWith(GeoCircle circle) => Collision.CollidesWith(circle, this, Tolerance.Global);

        /// <summary>
        /// Checks whether this polyline collides with a circle within tolerance.
        /// </summary>
        public bool CollidesWith(GeoCircle circle, Tolerance tolerance) => Collision.CollidesWith(circle, this, tolerance);

        /// <summary>
        /// Checks whether this polyline collides with a polygon using default tolerance.
        /// </summary>
        public bool CollidesWith(GeoPolygon poly) => Collision.CollidesWith(this, poly, Tolerance.Global);

        /// <summary>
        /// Checks whether this polyline collides with a polygon within tolerance.
        /// </summary>
        public bool CollidesWith(GeoPolygon poly, Tolerance tolerance) => Collision.CollidesWith(this, poly, tolerance);

        /// <summary>
        /// Checks whether this polyline collides with another polyline using default tolerance.
        /// </summary>
        public bool CollidesWith(GeoPolyline other) => Collision.CollidesWith(this, other, Tolerance.Global);

        /// <summary>
        /// Checks whether this polyline collides with another polyline within tolerance.
        /// </summary>
        public bool CollidesWith(GeoPolyline other, Tolerance tolerance) => Collision.CollidesWith(this, other, tolerance);

        /// <summary>
        /// Gets all intersection points with a line segment using default tolerance.
        /// </summary>
        public GeoPoint[] GetIntersections(GeoLine line) => Intersection.GetIntersections(this, line, Tolerance.Global);

        /// <summary>
        /// Gets all intersection points with a line segment within tolerance.
        /// </summary>
        public GeoPoint[] GetIntersections(GeoLine line, Tolerance tolerance) => Intersection.GetIntersections(this, line, tolerance);

        /// <summary>
        /// Gets all intersection points with another polyline using default tolerance.
        /// </summary>
        public GeoPoint[] GetIntersections(GeoPolyline other) => Intersection.GetIntersections(this, other, Tolerance.Global);

        /// <summary>
        /// Gets all intersection points with another polyline within tolerance.
        /// </summary>
        public GeoPoint[] GetIntersections(GeoPolyline other, Tolerance tolerance) => Intersection.GetIntersections(this, other, tolerance);

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
        /// Gets all intersection points with a polygon using default tolerance.
        /// </summary>
        public GeoPoint[] GetIntersections(GeoPolygon poly) => Intersection.GetIntersections(this, poly, Tolerance.Global);

        /// <summary>
        /// Gets all intersection points with a polygon within tolerance.
        /// </summary>
        public GeoPoint[] GetIntersections(GeoPolygon poly, Tolerance tolerance) => Intersection.GetIntersections(this, poly, tolerance);

        /// <summary>
        /// Translates a polyline by a vector.
        /// </summary>
        public static GeoPolyline operator +(GeoPolyline polyline, GeoVector vector) => polyline.Translate(vector);

        /// <summary>
        /// Translates a polyline backwards by a vector.
        /// </summary>
        public static GeoPolyline operator -(GeoPolyline polyline, GeoVector vector) => polyline.Translate(-vector);

        /// <summary>
        /// Indicates whether the current polyline is equal to another polyline.
        /// </summary>
        public bool Equals(GeoPolyline other)
        {
            if (other == null || other.VertexCount != VertexCount || other.IsClosed != IsClosed) return false;
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
        public override bool Equals(object obj) => obj is GeoPolyline other && Equals(other);

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + IsClosed.GetHashCode();
                foreach (var v in _vertices)
                {
                    hash = hash * 31 + v.GetHashCode();
                }
                return hash;
            }
        }

        /// <summary>
        /// Compares two GeoPolyline instances for equality.
        /// </summary>
        public static bool operator ==(GeoPolyline left, GeoPolyline right) => Equals(left, right);

        /// <summary>
        /// Compares two GeoPolyline instances for inequality.
        /// </summary>
        public static bool operator !=(GeoPolyline left, GeoPolyline right) => !Equals(left, right);

        /// <summary>
        /// Returns the string representation of the polyline.
        /// </summary>
        public override string ToString() => $"GeoPolyline[{(IsClosed ? "Closed" : "Open")}, {VertexCount} vertices, Length:{Length:0.000}]";

    }
}
