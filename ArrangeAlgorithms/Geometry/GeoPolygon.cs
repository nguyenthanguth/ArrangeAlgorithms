using System;
using System.Collections.Generic;
using System.Linq;

namespace ArrangeAlgorithms.Geometry
{
    /// <summary>
    /// Represents a 2D simple closed polygon.
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
        /// Initializes a new polygon from a list of vertices. Duplicate vertex at the end to close the loop will be automatically removed.
        /// </summary>
        /// <param name="vertices">List of vertices.</param>
        public GeoPolygon(IEnumerable<GeoPoint> vertices)
        {
            if (vertices == null) throw new ArgumentNullException(nameof(vertices));

            List<GeoPoint> list = vertices.ToList();
            // Remove duplicate last vertex if it equals the first vertex
            while (list.Count > 1 && list[list.Count - 1].Equals(list[0]))
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
        /// Checks whether the polygon contains a point using default tolerance (accepts points on the boundary).
        /// </summary>
        public bool Contains(GeoPoint GeoPoint)
        {
            return Contains(GeoPoint, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether the polygon contains a point (accepts points on the boundary).
        /// Uses the Ray Casting algorithm (shoots a horizontal ray and counts intersections).
        /// </summary>
        public bool Contains(GeoPoint GeoPoint, Tolerance tolerance)
        {
            // Check if point is on the polygon boundary first
            for (int i = 0; i < EdgeCount; i++)
            {
                if (GetEdgeAt(i).IsPointOn(GeoPoint, tolerance))
                {
                    return true;
                }
            }

            bool inside = false;
            int n = _vertices.Length;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                GeoPoint p1 = _vertices[i];
                GeoPoint p2 = _vertices[j];

                if (((p1.Y > GeoPoint.Y) != (p2.Y > GeoPoint.Y)) &&
                    (GeoPoint.X < (p2.X - p1.X) * (GeoPoint.Y - p1.Y) / (p2.Y - p1.Y) + p1.X))
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        /// <summary>
        /// Checks whether the polygon intersects with a rotated rectangle (GeoRectangle OBB) using default tolerance.
        /// </summary>
        public bool IntersectsWith(GeoRectangle rect)
        {
            return IntersectsWith(rect, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether the polygon intersects with a rotated rectangle (GeoRectangle OBB).
        /// </summary>
        public bool IntersectsWith(GeoRectangle rect, Tolerance tolerance)
        {
            // 1. Check if any polygon edge intersects with any rectangle edge
            GeoLine[] rectEdges = rect.GetEdges();

            foreach (var polyEdge in GetEdges())
            {
                foreach (var rectEdge in rectEdges)
                {
                    if (polyEdge.TryIntersectWith(rectEdge, out _, tolerance))
                    {
                        return true;
                    }
                }
            }

            // 2. Check if the polygon lies entirely inside the rectangle
            if (rect.Contains(_vertices[0]))
            {
                return true;
            }

            // 3. Check if the rectangle lies entirely inside the polygon
            if (Contains(rect.Center, tolerance))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks whether the polygon intersects with a line segment using default tolerance.
        /// </summary>
        public bool IntersectsWith(GeoLine geoLine)
        {
            return IntersectsWith(geoLine, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether the polygon intersects with a line segment.
        /// </summary>
        public bool IntersectsWith(GeoLine geoLine, Tolerance tolerance)
        {
            // 1. Check if the line segment intersects with any polygon edge
            foreach (var polyEdge in GetEdges())
            {
                if (geoLine.TryIntersectWith(polyEdge, out _, tolerance))
                {
                    return true;
                }
            }

            // 2. Or the line segment lies entirely inside the polygon.
            // Just check the start point: if the segment does not intersect any edge
            // and one endpoint is inside, then the entire segment is inside.
            return Contains(geoLine.StartPoint, tolerance);
        }

        /// <summary>
        /// Checks whether the polygon intersects with another polygon using default tolerance.
        /// </summary>
        public bool IntersectsWith(GeoPolygon other)
        {
            return IntersectsWith(other, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether the polygon intersects with another polygon.
        /// </summary>
        public bool IntersectsWith(GeoPolygon other, Tolerance tolerance)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            // 1. Check if edges of the two polygons intersect
            foreach (var edge in GetEdges())
            {
                foreach (var otherEdge in other.GetEdges())
                {
                    if (edge.TryIntersectWith(otherEdge, out _, tolerance))
                    {
                        return true;
                    }
                }
            }

            // 2. No edges intersect: either the two polygons are disjoint, or one polygon lies
            // entirely inside the other. Checking a single vertex on each side is sufficient to distinguish the two cases.
            return Contains(other._vertices[0], tolerance) || other.Contains(_vertices[0], tolerance);
        }

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

        public override bool Equals(object obj)
        {
            return obj is GeoPolygon other && Equals(other);
        }

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

        public override string ToString() => $"GeoPolygon[{VertexCount} vertices, Area:{GetArea():0.000}]";
    }
}
