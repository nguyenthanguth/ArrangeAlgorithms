using System;

namespace ArrangeAlgorithms.Geometry
{
    /// <summary>
    /// Represents a 2D Oriented Bounding Box (OBB) that can be rotated.
    /// </summary>
    public readonly struct GeoRectangle : IEquatable<GeoRectangle>
    {
        /// <summary>
        /// Gets the center point of the rectangle.
        /// </summary>
        public GeoPoint Center { get; }

        /// <summary>
        /// Gets the width of the rectangle (along its local X-axis).
        /// </summary>
        public double Width { get; }

        /// <summary>
        /// Gets the height of the rectangle (along its local Y-axis).
        /// </summary>
        public double Height { get; }

        /// <summary>
        /// Gets the rotation angle in radians (counter-clockwise).
        /// </summary>
        public double AngleRad { get; }

        /// <summary>
        /// Gets a value indicating whether the rectangle is rotated (non-zero rotation angle).
        /// </summary>
        public bool IsRotated => Math.Abs(AngleRad) > Tolerance.Global.EqualVector;

        /// <summary>
        /// Initializes a new GeoRectangle instance from center, width, height, and rotation angle.
        /// </summary>
        /// <param name="center">Center point of the rectangle.</param>
        /// <param name="width">Rectangle width.</param>
        /// <param name="height">Rectangle height.</param>
        /// <param name="angleRad">Rotation angle in radians.</param>
        public GeoRectangle(GeoPoint center, double width, double height, double angleRad = 0.0)
        {
            Center = center;
            Width = width;
            Height = height;
            AngleRad = angleRad;
        }

        /// <summary>
        /// Initializes a new GeoRectangle instance from the bottom-left corner position (unrotated) and dimensions.
        /// </summary>
        /// <param name="x">X coordinate of the bottom-left corner when unrotated.</param>
        /// <param name="y">Y coordinate of the bottom-left corner when unrotated.</param>
        /// <param name="width">Rectangle width.</param>
        /// <param name="height">Rectangle height.</param>
        public GeoRectangle(double x, double y, double width, double height)
            : this(new GeoPoint(x + width * 0.5, y + height * 0.5), width, height, 0.0)
        {
        }

        /// <summary>
        /// Gets the bottom-left corner coordinates.
        /// </summary>
        public GeoPoint LowerLeft
        {
            get
            {
                double cos = Math.Cos(AngleRad);
                double sin = Math.Sin(AngleRad);
                double halfW = Width * 0.5;
                double halfH = Height * 0.5;
                return new GeoPoint(
                    Center.X - halfW * cos + halfH * sin,
                    Center.Y - halfW * sin - halfH * cos);
            }
        }

        /// <summary>
        /// Gets the bottom-right corner coordinates.
        /// </summary>
        public GeoPoint LowerRight
        {
            get
            {
                double cos = Math.Cos(AngleRad);
                double sin = Math.Sin(AngleRad);
                double halfW = Width * 0.5;
                double halfH = Height * 0.5;
                return new GeoPoint(
                    Center.X + halfW * cos + halfH * sin,
                    Center.Y + halfW * sin - halfH * cos);
            }
        }

        /// <summary>
        /// Gets the top-left corner coordinates.
        /// </summary>
        public GeoPoint UpperLeft
        {
            get
            {
                double cos = Math.Cos(AngleRad);
                double sin = Math.Sin(AngleRad);
                double halfW = Width * 0.5;
                double halfH = Height * 0.5;
                return new GeoPoint(
                    Center.X - halfW * cos - halfH * sin,
                    Center.Y - halfW * sin + halfH * cos);
            }
        }

        /// <summary>
        /// Gets the top-right corner coordinates.
        /// </summary>
        public GeoPoint UpperRight
        {
            get
            {
                double cos = Math.Cos(AngleRad);
                double sin = Math.Sin(AngleRad);
                double halfW = Width * 0.5;
                double halfH = Height * 0.5;
                return new GeoPoint(
                    Center.X + halfW * cos - halfH * sin,
                    Center.Y + halfW * sin + halfH * cos);
            }
        }

        /// <summary>
        /// Gets the 4 vertices of the rectangle in counter-clockwise order: LowerLeft, LowerRight, UpperRight, UpperLeft.
        /// </summary>
        public GeoPoint[] GetVertices()
        {
            return new[] { LowerLeft, LowerRight, UpperRight, UpperLeft };
        }

        /// <summary>
        /// Gets the 4 closed edges of the rectangle, sequentially connecting the vertices returned by <see cref="GetVertices"/>.
        /// </summary>
        public GeoLine[] GetEdges()
        {
            GeoPoint[] v = GetVertices();
            return new[]
            {
                new GeoLine(v[0], v[1]),
                new GeoLine(v[1], v[2]),
                new GeoLine(v[2], v[3]),
                new GeoLine(v[3], v[0])
            };
        }

        /// <summary>
        /// Checks whether the rectangle contains a point.
        /// </summary>
        public bool Contains(GeoPoint GeoPoint)
        {
            double dx = GeoPoint.X - Center.X;
            double dy = GeoPoint.Y - Center.Y;

            double cos = Math.Cos(AngleRad);
            double sin = Math.Sin(AngleRad);

            // Project point onto the local coordinate system around Center
            double localX = dx * cos + dy * sin;
            double localY = -dx * sin + dy * cos;

            double halfW = Width * 0.5;
            double halfH = Height * 0.5;

            return localX >= -halfW && localX <= halfW &&
                   localY >= -halfH && localY <= halfH;
        }

        /// <summary>
        /// Checks whether this rectangle intersects with another rectangle using default tolerance.
        /// </summary>
        public bool IntersectsWith(GeoRectangle other)
        {
            return IntersectsWith(other, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether this rectangle intersects with another rectangle using the Separating Axis Theorem (SAT).
        /// Clearance smaller than the tolerance is still considered a collision.
        /// </summary>
        public bool IntersectsWith(GeoRectangle other, Tolerance tolerance)
        {
            GeoPoint[] r1 = GetVertices();
            GeoPoint[] r2 = other.GetVertices();

            // Axes to check (perpendicular to the edges of both rectangles)
            GeoVector[] axes = new GeoVector[4];
            axes[0] = new GeoVector(r1[1].X - r1[0].X, r1[1].Y - r1[0].Y).Normalize(); // X-axis of R1
            axes[1] = new GeoVector(r1[3].X - r1[0].X, r1[3].Y - r1[0].Y).Normalize(); // Y-axis of R1
            axes[2] = new GeoVector(r2[1].X - r2[0].X, r2[1].Y - r2[0].Y).Normalize(); // X-axis of R2
            axes[3] = new GeoVector(r2[3].X - r2[0].X, r2[3].Y - r2[0].Y).Normalize(); // Y-axis of R2

            foreach (var axis in axes)
            {
                if (axis.X == 0 && axis.Y == 0) continue;

                // Project R1 onto axis
                double min1 = double.MaxValue;
                double max1 = double.MinValue;
                foreach (var p in r1)
                {
                    double proj = p.X * axis.X + p.Y * axis.Y;
                    if (proj < min1) min1 = proj;
                    if (proj > max1) max1 = proj;
                }

                // Project R2 onto axis
                double min2 = double.MaxValue;
                double max2 = double.MinValue;
                foreach (var p in r2)
                {
                    double proj = p.X * axis.X + p.Y * axis.Y;
                    if (proj < min2) min2 = proj;
                    if (proj > max2) max2 = proj;
                }

                // Check separation on this axis. The two shapes are only truly disjoint when the clearance
                // between projections exceeds tolerance; anything narrower is considered contact.
                if (min2 - max1 > tolerance.EqualPoint || min1 - max2 > tolerance.EqualPoint)
                {
                    return false; // Separating axis found, the two shapes do not intersect
                }
            }

            return true; // No separating axis found, the two shapes intersect
        }

        /// <summary>
        /// Checks whether this rectangle intersects with a line segment using default tolerance.
        /// </summary>
        public bool IntersectsWith(GeoLine geoLine)
        {
            return IntersectsWith(geoLine, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether this rectangle intersects with a line segment.
        /// </summary>
        public bool IntersectsWith(GeoLine geoLine, Tolerance tolerance)
        {
            // If the line segment intersects with any edge of the rectangle
            foreach (var edge in GetEdges())
            {
                if (geoLine.TryIntersectWith(edge, out _, tolerance))
                {
                    return true;
                }
            }

            // Or if the line segment lies entirely inside the rectangle.
            // Just check the start point: if the segment does not intersect any edge
            // and one endpoint is inside, then the entire segment is inside.
            if (Contains(geoLine.StartPoint))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks whether this rectangle intersects with a polygon using default tolerance.
        /// </summary>
        public bool IntersectsWith(GeoPolygon poly)
        {
            return IntersectsWith(poly, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether this rectangle intersects with a polygon.
        /// </summary>
        public bool IntersectsWith(GeoPolygon poly, Tolerance tolerance)
        {
            if (poly == null) throw new ArgumentNullException(nameof(poly));

            return poly.IntersectsWith(this, tolerance);
        }

        /// <summary>
        /// Calculates the shortest boundary distance from this rectangle to a polygon.
        /// </summary>
        public double DistanceTo(GeoPolygon poly)
        {
            if (poly == null) throw new ArgumentNullException(nameof(poly));

            double minDistance = double.MaxValue;
            GeoPoint[] rectVertices = GetVertices();
            GeoLine[] rectEdges = GetEdges();

            foreach (var rv in rectVertices)
            {
                for (int i = 0; i < poly.EdgeCount; i++)
                {
                    double d = poly.GetEdgeAt(i).DistanceTo(rv);
                    if (d < minDistance) minDistance = d;
                }
            }

            for (int i = 0; i < poly.VertexCount; i++)
            {
                var pv = poly[i];
                foreach (var re in rectEdges)
                {
                    double d = re.DistanceTo(pv);
                    if (d < minDistance) minDistance = d;
                }
            }

            return minDistance;
        }

        /// <summary>
        /// Calculates the shortest boundary distance from this rectangle to a line segment.
        /// </summary>
        public double DistanceTo(GeoLine GeoLine)
        {
            double minDistance = double.MaxValue;
            GeoPoint[] rectVertices = GetVertices();
            GeoLine[] rectEdges = GetEdges();

            foreach (var rv in rectVertices)
            {
                double d = GeoLine.DistanceTo(rv);
                if (d < minDistance) minDistance = d;
            }

            foreach (var re in rectEdges)
            {
                double d1 = re.DistanceTo(GeoLine.StartPoint);
                double d2 = re.DistanceTo(GeoLine.EndPoint);
                if (d1 < minDistance) minDistance = d1;
                if (d2 < minDistance) minDistance = d2;
            }

            return minDistance;
        }

        /// <summary>
        /// Calculates the shortest boundary distance from this rectangle to another rectangle.
        /// </summary>
        public double DistanceTo(GeoRectangle other)
        {
            double minDistance = double.MaxValue;
            GeoPoint[] r1Vertices = GetVertices();
            GeoPoint[] r2Vertices = other.GetVertices();
            GeoLine[] r1Edges = GetEdges();
            GeoLine[] r2Edges = other.GetEdges();

            foreach (var r1v in r1Vertices)
            {
                foreach (var r2e in r2Edges)
                {
                    double d = r2e.DistanceTo(r1v);
                    if (d < minDistance) minDistance = d;
                }
            }

            foreach (var r2v in r2Vertices)
            {
                foreach (var r1e in r1Edges)
                {
                    double d = r1e.DistanceTo(r2v);
                    if (d < minDistance) minDistance = d;
                }
            }

            return minDistance;
        }

        /// <summary>
        /// Indicates whether the current rectangle is equal to another rectangle.
        /// </summary>
        /// <param name="other">A rectangle to compare with this rectangle.</param>
        /// <returns>true if the current rectangle is equal to the other parameter; otherwise, false.</returns>
        public bool Equals(GeoRectangle other)
        {
            return Center.Equals(other.Center) &&
                   Width.Equals(other.Width) &&
                   Height.Equals(other.Height) &&
                   AngleRad.Equals(other.AngleRad);
        }

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>true if obj and this instance are the same type and represent the same value; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return obj is GeoRectangle other && Equals(other);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Center.GetHashCode();
                hash = (hash * 397) ^ Width.GetHashCode();
                hash = (hash * 397) ^ Height.GetHashCode();
                hash = (hash * 397) ^ AngleRad.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Compares two GeoRectangle instances for equality.
        /// </summary>
        /// <param name="left">The first rectangle.</param>
        /// <param name="right">The second rectangle.</param>
        /// <returns>true if they are equal; otherwise, false.</returns>
        public static bool operator ==(GeoRectangle left, GeoRectangle right) => left.Equals(right);

        /// <summary>
        /// Compares two GeoRectangle instances for inequality.
        /// </summary>
        /// <param name="left">The first rectangle.</param>
        /// <param name="right">The second rectangle.</param>
        /// <returns>true if they are not equal; otherwise, false.</returns>
        public static bool operator !=(GeoRectangle left, GeoRectangle right) => !left.Equals(right);

        /// <summary>
        /// Returns the string representation of the rectangle.
        /// </summary>
        /// <returns>A string representation detailing center, width, height, and angle.</returns>
        public override string ToString()
        {
            return $"GeoRectangle[Center:{Center}, Width:{Width:0.000}, Height:{Height:0.000}, AngleRad:{AngleRad:0.000}]";
        }
    }
}
