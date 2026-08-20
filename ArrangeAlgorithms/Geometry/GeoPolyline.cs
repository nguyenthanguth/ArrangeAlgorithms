using System;
using System.Collections.Generic;
using System.Linq;
using ArrangeAlgorithms.Core;
using ArrangeAlgorithms.Enums;

namespace ArrangeAlgorithms.Geometry
{
    /// <summary>
    /// Represents a 2D polyline: an open chain of connected line segments.
    /// <para>
    /// A polyline is always a path, never a region. Geometry that closes back on itself is a
    /// <see cref="GeoPolygon"/>, which is where area, containment and winding live; <see cref="ToPolygon"/>
    /// converts a chain whose shape is meant to enclose something.
    /// </para>
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
        /// Gets the number of line segment edges in the polyline, which is always one less than the
        /// number of vertices: the chain stops at the last vertex instead of running back to the first.
        /// </summary>
        public int EdgeCount => _vertices.Length - 1;

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
        /// Gets the point half way along the polyline, which can be used to probe the location of the whole polyline.
        /// </summary>
        public GeoPoint MidPoint => GetPointAtDistance(Length * 0.5);

        /// <summary>
        /// Initializes a new GeoPolyline instance from a collection of vertices.
        /// Consecutive duplicate vertices are automatically filtered out.
        /// </summary>
        /// <param name="vertices">The sequence of vertices.</param>
        public GeoPolyline(IEnumerable<GeoPoint> vertices)
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

            if (list.Count < 2)
            {
                throw new ArgumentException("A polyline must have at least 2 distinct vertices.");
            }

            _vertices = list.ToArray();
        }

        /// <summary>
        /// Initializes a new GeoPolyline from parameter vertices.
        /// </summary>
        /// <param name="vertices">The sequence of vertices.</param>
        public GeoPolyline(params GeoPoint[] vertices)
            : this((IEnumerable<GeoPoint>)vertices)
        {
        }

        /// <summary>
        /// Initializes a polyline from vertices that have already been filtered and validated.
        /// </summary>
        /// <param name="validatedVertices">Source array, already free of consecutive duplicates.</param>
        /// <param name="count">Number of leading entries to copy.</param>
        /// <remarks>
        /// Clone and <see cref="Core.Splition"/> use this instead of the public constructor. The
        /// public one re-filters vertices against Tolerance.Global, so a clone taken after that global was
        /// widened could silently come back with fewer vertices than the original, or fail validation
        /// outright. Splitting has the same need for a different reason: it receives an explicit tolerance
        /// from its caller and must not have its results re-filtered against an unrelated global one.
        /// <para>
        /// What this skips is the tolerance dependent work: filtering duplicates. The vertex count is
        /// still checked, because it costs nothing, reads no global state, and is what lets EdgeCount
        /// subtract one without guarding the result.
        /// </para>
        /// </remarks>
        internal GeoPolyline(GeoPoint[] validatedVertices, int count)
        {
            if (count < 2)
            {
                throw new ArgumentException("A polyline must have at least 2 distinct vertices.", nameof(count));
            }

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
        /// is the end. Values outside [0, 1] are clamped to the endpoints.
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
            return new GeoPolyline(rev);
        }

        /// <summary>
        /// Creates a copy of this polyline holding its own vertex array.
        /// </summary>
        /// <returns>A new GeoPolyline independent of this one.</returns>
        public GeoPolyline Clone() => new GeoPolyline(_vertices, _vertices.Length);

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
            return new GeoPolyline(moved);
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
            return new GeoPolyline(rotated);
        }

        /// <summary>
        /// Converts this polyline into a solid 2D GeoPolygon.
        /// The chain is closed by connecting the last vertex back to the first.
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
        /// Gets the closest point on the path of this polyline to a target point.
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
        /// Splits this polyline at a point lying on it, using the default tolerance.
        /// </summary>
        /// <param name="point">The point to split at, which must lie on this polyline.</param>
        /// <param name="first">The piece holding the start point.</param>
        /// <param name="second">The piece holding the end point.</param>
        /// <returns>true if the polyline was split; otherwise, false.</returns>
        public bool TrySplitBy(GeoPoint point, out GeoPolyline first, out GeoPolyline second)
            => Splition.TrySplitBy(this, point, out first, out second, Tolerance.Global);

        /// <summary>
        /// Splits this polyline at a point lying on it, within tolerance.
        /// </summary>
        /// <param name="point">The point to split at, which must lie on this polyline.</param>
        /// <param name="first">The piece holding the start point.</param>
        /// <param name="second">The piece holding the end point.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>true if the polyline was split; otherwise, false.</returns>
        public bool TrySplitBy(GeoPoint point, out GeoPolyline first, out GeoPolyline second, Tolerance tolerance)
            => Splition.TrySplitBy(this, point, out first, out second, tolerance);

        /// <summary>
        /// Splits this polyline everywhere a cutting line segment crosses it, using the default tolerance.
        /// </summary>
        /// <param name="cutter">The cutting line segment.</param>
        /// <param name="pieces">The pieces in order along this polyline if split succeeds.</param>
        /// <returns>true if the polyline was split; otherwise, false.</returns>
        public bool TrySplitBy(GeoLine cutter, out GeoPolyline[] pieces)
            => Splition.TrySplitBy(this, cutter, out pieces, Tolerance.Global);

        /// <summary>
        /// Splits this polyline everywhere a cutting line segment crosses it, within tolerance.
        /// </summary>
        /// <param name="cutter">The cutting line segment.</param>
        /// <param name="pieces">The pieces in order along this polyline if split succeeds.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>true if the polyline was split; otherwise, false.</returns>
        public bool TrySplitBy(GeoLine cutter, out GeoPolyline[] pieces, Tolerance tolerance)
            => Splition.TrySplitBy(this, cutter, out pieces, tolerance);

        /// <summary>
        /// Splits this polyline everywhere a list of points lies on it, using the default tolerance.
        /// </summary>
        /// <param name="points">The points to split at.</param>
        /// <param name="pieces">The resulting pieces in order along this polyline.</param>
        /// <returns>true if this polyline was split; otherwise, false.</returns>
        public bool TrySplitBy(GeoPoint[] points, out GeoPolyline[] pieces)
            => Splition.TrySplitBy(this, points, out pieces, Tolerance.Global);

        /// <summary>
        /// Splits this polyline everywhere a list of points lies on it, within tolerance.
        /// </summary>
        /// <param name="points">The points to split at.</param>
        /// <param name="pieces">The resulting pieces in order along this polyline.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>true if this polyline was split; otherwise, false.</returns>
        public bool TrySplitBy(GeoPoint[] points, out GeoPolyline[] pieces, Tolerance tolerance)
            => Splition.TrySplitBy(this, points, out pieces, tolerance);

        /// <summary>
        /// Splits this polyline everywhere a list of cutting line segments crosses it, using the default tolerance.
        /// </summary>
        /// <param name="cutters">The cutting line segments.</param>
        /// <param name="pieces">The resulting pieces in order along this polyline.</param>
        /// <returns>true if this polyline was split; otherwise, false.</returns>
        public bool TrySplitBy(GeoLine[] cutters, out GeoPolyline[] pieces)
            => Splition.TrySplitBy(this, cutters, out pieces, Tolerance.Global);

        /// <summary>
        /// Splits this polyline everywhere a list of cutting line segments crosses it, within tolerance.
        /// </summary>
        /// <param name="cutters">The cutting line segments.</param>
        /// <param name="pieces">The resulting pieces in order along this polyline.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>true if this polyline was split; otherwise, false.</returns>
        public bool TrySplitBy(GeoLine[] cutters, out GeoPolyline[] pieces, Tolerance tolerance)
            => Splition.TrySplitBy(this, cutters, out pieces, tolerance);

        /// <summary>
        /// Splits this polyline everywhere a list of cutting polylines crosses it, using the default tolerance.
        /// </summary>
        /// <param name="cutters">The cutting polylines.</param>
        /// <param name="pieces">The resulting pieces in order along this polyline.</param>
        /// <returns>true if this polyline was split; otherwise, false.</returns>
        public bool TrySplitBy(GeoPolyline[] cutters, out GeoPolyline[] pieces)
            => Splition.TrySplitBy(this, cutters, out pieces, Tolerance.Global);

        /// <summary>
        /// Splits this polyline everywhere a list of cutting polylines crosses it, within tolerance.
        /// </summary>
        /// <param name="cutters">The cutting polylines.</param>
        /// <param name="pieces">The resulting pieces in order along this polyline.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>true if this polyline was split; otherwise, false.</returns>
        public bool TrySplitBy(GeoPolyline[] cutters, out GeoPolyline[] pieces, Tolerance tolerance)
            => Splition.TrySplitBy(this, cutters, out pieces, tolerance);

        /// <summary>
        /// Splits this polyline everywhere a list of polygon boundaries crosses it, separating the parts that fall
        /// inside the polygons from those that fall outside, using the default tolerance.
        /// </summary>
        /// <param name="cutters">The polygons to split against.</param>
        /// <param name="inside">The sub-polylines lying inside the polygons, in order along this polyline.</param>
        /// <param name="outside">The sub-polylines lying outside the polygons, in order along this polyline.</param>
        /// <returns>true if this polyline was split; otherwise, false.</returns>
        public bool TrySplitBy(GeoPolygon[] cutters, out GeoPolyline[] inside, out GeoPolyline[] outside)
            => Splition.TrySplitBy(this, cutters, out inside, out outside, Tolerance.Global);

        /// <summary>
        /// Splits this polyline everywhere a list of polygon boundaries crosses it, separating the parts that fall
        /// inside the polygons from those that fall outside, within tolerance.
        /// </summary>
        /// <param name="cutters">The polygons to split against.</param>
        /// <param name="inside">The sub-polylines lying inside the polygons, in order along this polyline.</param>
        /// <param name="outside">The sub-polylines lying outside the polygons, in order along this polyline.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>true if this polyline was split; otherwise, false.</returns>
        public bool TrySplitBy(GeoPolygon[] cutters, out GeoPolyline[] inside, out GeoPolyline[] outside, Tolerance tolerance)
            => Splition.TrySplitBy(this, cutters, out inside, out outside, tolerance);

        /// <summary>
        /// Splits this polyline against a polygon, sorting the sub-polylines by which side of its boundary
        /// they fall on, using the default tolerance.
        /// </summary>
        /// <param name="cutter">The polygon to split against.</param>
        /// <param name="inside">The sub-polylines lying inside the polygon, in order along this polyline.</param>
        /// <param name="outside">The sub-polylines lying outside the polygon, in order along this polyline.</param>
        /// <returns>true if the polygon boundary crosses this polyline; otherwise, false.</returns>
        public bool TrySplitBy(GeoPolygon cutter, out GeoPolyline[] inside, out GeoPolyline[] outside)
            => Splition.TrySplitBy(this, cutter, out inside, out outside, Tolerance.Global);

        /// <summary>
        /// Splits this polyline against a polygon, sorting the sub-polylines by which side of its boundary
        /// they fall on, within tolerance.
        /// </summary>
        /// <param name="cutter">The polygon to split against.</param>
        /// <param name="inside">The sub-polylines lying inside the polygon, in order along this polyline.</param>
        /// <param name="outside">The sub-polylines lying outside the polygon, in order along this polyline.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>true if the polygon boundary crosses this polyline; otherwise, false.</returns>
        public bool TrySplitBy(GeoPolygon cutter, out GeoPolyline[] inside, out GeoPolyline[] outside, Tolerance tolerance)
            => Splition.TrySplitBy(this, cutter, out inside, out outside, tolerance);

        /// <summary>
        /// Splits this polyline at an arc length measured from its first vertex, using the default
        /// tolerance.
        /// </summary>
        /// <param name="distance">Arc length from the first vertex.</param>
        /// <param name="first">The piece holding the start point.</param>
        /// <param name="second">The piece holding the end point.</param>
        /// <returns>true if the polyline was split; otherwise, false.</returns>
        public bool TrySplitAtDistance(double distance, out GeoPolyline first, out GeoPolyline second)
            => Splition.TrySplitAtDistance(this, distance, out first, out second, Tolerance.Global);

        /// <summary>
        /// Splits this polyline at an arc length measured from its first vertex, within tolerance.
        /// </summary>
        /// <param name="distance">Arc length from the first vertex.</param>
        /// <param name="first">The piece holding the start point.</param>
        /// <param name="second">The piece holding the end point.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>true if the polyline was split; otherwise, false.</returns>
        public bool TrySplitAtDistance(double distance, out GeoPolyline first, out GeoPolyline second, Tolerance tolerance)
            => Splition.TrySplitAtDistance(this, distance, out first, out second, tolerance);

        /// <summary>
        /// Splits this polyline at several arc lengths measured from its first vertex, using the default
        /// tolerance.
        /// </summary>
        /// <param name="distances">Arc lengths from the first vertex, in any order.</param>
        /// <returns>The pieces in order along this polyline.</returns>
        public GeoPolyline[] SplitAtDistances(IEnumerable<double> distances)
            => Splition.SplitAtDistances(this, distances, Tolerance.Global);

        /// <summary>
        /// Splits this polyline at several arc lengths measured from its first vertex, within tolerance.
        /// </summary>
        /// <param name="distances">Arc lengths from the first vertex, in any order.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>The pieces in order along this polyline.</returns>
        public GeoPolyline[] SplitAtDistances(IEnumerable<double> distances, Tolerance tolerance)
            => Splition.SplitAtDistances(this, distances, tolerance);

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
        public override bool Equals(object obj) => obj is GeoPolyline other && Equals(other);

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
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
        public override string ToString() => $"GeoPolyline[{VertexCount} vertices, Length:{Length:0.000}]";

    }
}
