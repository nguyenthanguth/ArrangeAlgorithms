using System;
using ArrangeAlgorithms.Geometry;

namespace ArrangeAlgorithms.Core
{
    /// <summary>
    /// Provides static calculation methods for geometric projections of points and vectors onto lines,
    /// circles, rectangles, polygons, and polylines.
    /// <para>
    /// Projecting onto a closed shape lands on its <b>boundary</b>: the circumference of a circle, or one
    /// of the edges of a rectangle, polygon, or polyline. That holds even when the point is already inside,
    /// which is carried out to the nearest edge rather than returned unchanged. The geometry types expose
    /// the same operation as <c>GetClosestPointOnBoundary</c>.
    /// </para>
    /// <para>
    /// <see cref="Distance"/> does not follow this rule at all: it treats a closed shape as a filled
    /// region, so the distance to a point inside it is zero.
    /// </para>
    /// </summary>
    public static class Projection
    {
        #region Point on Line

        /// <summary>
        /// Projects a point orthogonally onto a line segment using default tolerance, clamping the result
        /// to [StartPoint, EndPoint].
        /// </summary>
        /// <param name="line">The line segment.</param>
        /// <param name="point">The target point.</param>
        /// <returns>The closest point on the line segment.</returns>
        public static GeoPoint ProjectToLine(GeoLine line, GeoPoint point)
        {
            return ProjectToLine(line, point, Tolerance.Global);
        }

        /// <summary>
        /// Projects a point orthogonally onto a line segment within tolerance, clamping the result to
        /// [StartPoint, EndPoint]. The result never lies beyond either endpoint.
        /// </summary>
        /// <param name="line">The line segment.</param>
        /// <param name="point">The target point.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>The closest point on the line segment.</returns>
        public static GeoPoint ProjectToLine(GeoLine line, GeoPoint point, Tolerance tolerance)
        {
            double t = Parametrization.GetParameterAtPoint(line, point, tolerance);
            if (t <= 0.0) return line.StartPoint;
            if (t >= 1.0) return line.EndPoint;
            return line.GetPointAtParameter(t);
        }

        #endregion

        #region Point on Circle

        /// <summary>
        /// Projects a point onto the circumference of a circle using default tolerance.
        /// </summary>
        /// <param name="circle">The circle.</param>
        /// <param name="point">The target point.</param>
        /// <returns>The closest point on the circle boundary.</returns>
        public static GeoPoint ProjectToCircle(GeoCircle circle, GeoPoint point)
        {
            return ProjectToCircle(circle, point, Tolerance.Global);
        }

        /// <summary>
        /// Projects a point onto the circumference of a circle within tolerance. The result always lies on
        /// the circumference, including for points inside the circle.
        /// </summary>
        /// <param name="circle">The circle.</param>
        /// <param name="point">The target point.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>The closest point on the circle boundary.</returns>
        public static GeoPoint ProjectToCircle(GeoCircle circle, GeoPoint point, Tolerance tolerance)
        {
            GeoVector dir = circle.Center.GetVectorTo(point);
            if (!dir.TryGetNormal(out GeoVector normal, tolerance))
            {
                // If point is coincident with circle center, project to angle 0 on circumference
                return new GeoPoint(circle.Center.X + circle.Radius, circle.Center.Y);
            }

            return circle.Center.Add(normal.Multiply(circle.Radius));
        }

        #endregion

        #region Point on Rectangle

        /// <summary>
        /// Projects a point onto the boundary of a rotated rectangle (GeoRectangle OBB). The result always
        /// lies on one of the four edges, including for points inside the rectangle.
        /// </summary>
        /// <param name="rect">The rectangle.</param>
        /// <param name="point">The target point.</param>
        /// <returns>The closest point on the rectangle boundary.</returns>
        public static GeoPoint ProjectToRectangle(GeoRectangle rect, GeoPoint point)
        {
            double dx = point.X - rect.Center.X;
            double dy = point.Y - rect.Center.Y;

            double cos = Math.Cos(rect.AngleRad);
            double sin = Math.Sin(rect.AngleRad);

            // Project point onto local rectangle coordinate system
            double localX = dx * cos + dy * sin;
            double localY = -dx * sin + dy * cos;

            double halfW = rect.Width * 0.5;
            double halfH = rect.Height * 0.5;

            if (Math.Abs(localX) > halfW || Math.Abs(localY) > halfH)
            {
                // Outside: clamping into the box lands on the nearest edge or corner.
                localX = Math.Max(-halfW, Math.Min(halfW, localX));
                localY = Math.Max(-halfH, Math.Min(halfH, localY));
            }
            else
            {
                // Inside: clamping would return the point unchanged, so push it out to whichever edge is
                // nearest instead. A point on the diagonal is equidistant from two edges; snapping to the
                // horizontal one keeps the choice deterministic.
                double toVerticalEdge = halfW - Math.Abs(localX);
                double toHorizontalEdge = halfH - Math.Abs(localY);

                if (toVerticalEdge < toHorizontalEdge)
                {
                    localX = localX >= 0.0 ? halfW : -halfW;
                }
                else
                {
                    localY = localY >= 0.0 ? halfH : -halfH;
                }
            }

            // Transform back to world space
            double worldX = rect.Center.X + localX * cos - localY * sin;
            double worldY = rect.Center.Y + localX * sin + localY * cos;

            return new GeoPoint(worldX, worldY);
        }

        #endregion

        #region Point on Polygon

        /// <summary>
        /// Projects a point onto the boundary of a polygon using default tolerance.
        /// </summary>
        /// <param name="poly">The polygon.</param>
        /// <param name="point">The target point.</param>
        /// <returns>The closest point on the polygon boundary.</returns>
        public static GeoPoint ProjectToPolygon(GeoPolygon poly, GeoPoint point)
        {
            return ProjectToPolygon(poly, point, Tolerance.Global);
        }

        /// <summary>
        /// Projects a point onto the boundary of a polygon within tolerance (finds the closest point on any
        /// edge). The result always lies on the boundary, including for points inside the polygon.
        /// </summary>
        /// <param name="poly">The polygon.</param>
        /// <param name="point">The target point.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>The closest point on the polygon boundary.</returns>
        public static GeoPoint ProjectToPolygon(GeoPolygon poly, GeoPoint point, Tolerance tolerance)
        {
            if (poly == null) throw new ArgumentNullException(nameof(poly));

            double minDistanceSq = double.MaxValue;
            GeoPoint closestPoint = poly[0];

            for (int i = 0; i < poly.EdgeCount; i++)
            {
                GeoLine edge = poly.GetEdgeAt(i);
                GeoPoint proj = ProjectToLine(edge, point, tolerance);
                double dSq = Distance.GetDistanceSquaredTo(point, proj);
                if (dSq < minDistanceSq)
                {
                    minDistanceSq = dSq;
                    closestPoint = proj;
                }
            }

            return closestPoint;
        }

        #endregion

        #region Point on Polyline

        /// <summary>
        /// Projects a point orthogonally onto a polyline using default tolerance.
        /// </summary>
        /// <param name="polyline">The polyline.</param>
        /// <param name="point">The target point.</param>
        /// <returns>The closest point on the polyline.</returns>
        public static GeoPoint ProjectToPolyline(GeoPolyline polyline, GeoPoint point)
        {
            return ProjectToPolyline(polyline, point, Tolerance.Global);
        }

        /// <summary>
        /// Projects a point orthogonally onto a polyline within tolerance (finds the closest point across
        /// all segments). The result always lies on the path.
        /// </summary>
        /// <param name="polyline">The polyline.</param>
        /// <param name="point">The target point.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>The closest point on the polyline.</returns>
        public static GeoPoint ProjectToPolyline(GeoPolyline polyline, GeoPoint point, Tolerance tolerance)
        {
            if (polyline == null) throw new ArgumentNullException(nameof(polyline));

            double minDistanceSq = double.MaxValue;
            GeoPoint closestPoint = polyline[0];

            for (int i = 0; i < polyline.EdgeCount; i++)
            {
                GeoLine edge = polyline.GetEdgeAt(i);
                GeoPoint proj = ProjectToLine(edge, point, tolerance);
                double dSq = Distance.GetDistanceSquaredTo(point, proj);
                if (dSq < minDistanceSq)
                {
                    minDistanceSq = dSq;
                    closestPoint = proj;
                }
            }

            return closestPoint;
        }

        #endregion

        #region Vector Projection

        /// <summary>
        /// Projects a vector onto another axis vector using default tolerance.
        /// </summary>
        /// <param name="vector">The vector to project.</param>
        /// <param name="axis">The axis vector.</param>
        /// <returns>The vector projection along the axis.</returns>
        public static GeoVector Project(GeoVector vector, GeoVector axis)
        {
            return Project(vector, axis, Tolerance.Global);
        }

        /// <summary>
        /// Projects a vector onto another axis vector within tolerance.
        /// </summary>
        /// <param name="vector">The vector to project.</param>
        /// <param name="axis">The axis vector.</param>
        /// <param name="tolerance">The tolerance used to detect a zero-length axis.</param>
        /// <returns>The vector projection along the axis, or the zero vector for a degenerate axis.</returns>
        public static GeoVector Project(GeoVector vector, GeoVector axis, Tolerance tolerance)
        {
            double lenSq = axis.LengthSquared;
            if (lenSq <= tolerance.EqualVector * tolerance.EqualVector)
            {
                return GeoVector.Zero;
            }

            double scale = vector.DotProduct(axis) / lenSq;
            return axis.Multiply(scale);
        }

        #endregion
    }
}
