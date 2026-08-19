using System;
using System.Collections.Generic;
using ArrangeAlgorithms.Geometry;

namespace ArrangeAlgorithms.Operations
{
    /// <summary>
    /// Provides static calculation methods for checking spatial collisions and geometric overlaps.
    /// </summary>
    public static class Collision
    {
        #region Line - Line

        /// <summary>
        /// Checks whether two line segments collide / intersect using default tolerance.
        /// </summary>
        public static bool CollidesWith(GeoLine line1, GeoLine line2)
        {
            return CollidesWith(line1, line2, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether two line segments collide / intersect within tolerance.
        /// </summary>
        public static bool CollidesWith(GeoLine line1, GeoLine line2, Tolerance tolerance)
        {
            if (Intersection.TryIntersectWith(line1, line2, out _, tolerance))
            {
                return true;
            }
            return Distance.DistanceTo(line1, line2, tolerance) <= tolerance.EqualPoint;
        }

        #endregion

        #region Circle - Shapes

        /// <summary>
        /// Checks whether two circles collide or overlap using default tolerance.
        /// </summary>
        public static bool CollidesWith(GeoCircle c1, GeoCircle c2)
        {
            return CollidesWith(c1, c2, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether two circles collide or overlap within tolerance.
        /// </summary>
        public static bool CollidesWith(GeoCircle c1, GeoCircle c2, Tolerance tolerance)
        {
            return Distance.DistanceTo(c1.Center, c2.Center) <= c1.Radius + c2.Radius + tolerance.EqualPoint;
        }

        /// <summary>
        /// Checks whether a circle collides with a line segment using default tolerance.
        /// </summary>
        public static bool CollidesWith(GeoCircle circle, GeoLine line)
        {
            return CollidesWith(circle, line, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether a circle collides with a line segment within tolerance.
        /// </summary>
        public static bool CollidesWith(GeoCircle circle, GeoLine line, Tolerance tolerance)
        {
            return Distance.DistanceTo(line, circle.Center) <= circle.Radius + tolerance.EqualPoint;
        }

        /// <summary>
        /// Checks whether a circle collides with a rotated rectangle using default tolerance.
        /// </summary>
        public static bool CollidesWith(GeoCircle circle, GeoRectangle rect)
        {
            return CollidesWith(circle, rect, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether a circle collides with a rotated rectangle within tolerance.
        /// </summary>
        public static bool CollidesWith(GeoCircle circle, GeoRectangle rect, Tolerance tolerance)
        {
            if (Containment.Contains(rect, circle.Center))
            {
                return true;
            }
            return Distance.DistanceTo(rect, circle.Center) <= circle.Radius + tolerance.EqualPoint;
        }

        /// <summary>
        /// Checks whether a circle collides with a polygon using default tolerance.
        /// </summary>
        public static bool CollidesWith(GeoCircle circle, GeoPolygon poly)
        {
            return CollidesWith(circle, poly, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether a circle collides with a polygon within tolerance.
        /// </summary>
        public static bool CollidesWith(GeoCircle circle, GeoPolygon poly, Tolerance tolerance)
        {
            if (poly == null) throw new ArgumentNullException(nameof(poly));

            if (Containment.Contains(poly, circle.Center, tolerance))
            {
                return true;
            }
            return Distance.DistanceTo(poly, circle.Center) <= circle.Radius + tolerance.EqualPoint;
        }

        /// <summary>
        /// Checks whether a circle collides with a polyline using default tolerance.
        /// </summary>
        public static bool CollidesWith(GeoCircle circle, GeoPolyline polyline)
        {
            return CollidesWith(circle, polyline, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether a circle collides with a polyline within tolerance.
        /// </summary>
        public static bool CollidesWith(GeoCircle circle, GeoPolyline polyline, Tolerance tolerance)
        {
            if (polyline == null) throw new ArgumentNullException(nameof(polyline));

            for (int i = 0; i < polyline.EdgeCount; i++)
            {
                if (CollidesWith(circle, polyline.GetEdgeAt(i), tolerance))
                {
                    return true;
                }
            }

            // The circle may sit entirely inside a closed polyline without touching any edge.
            return Containment.Contains(polyline, circle.Center, tolerance);
        }

        #endregion

        #region Rectangle - Shapes (SAT & Bounding)

        /// <summary>
        /// Checks whether two rotated rectangles (GeoRectangle OBB) collide using default tolerance.
        /// </summary>
        public static bool CollidesWith(GeoRectangle rect1, GeoRectangle rect2)
        {
            return CollidesWith(rect1, rect2, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether two rotated rectangles (GeoRectangle OBB) collide using the Separating Axis Theorem (SAT).
        /// </summary>
        public static bool CollidesWith(GeoRectangle rect1, GeoRectangle rect2, Tolerance tolerance)
        {
            GeoPoint[] r1 = rect1.GetVertices();
            GeoPoint[] r2 = rect2.GetVertices();

            GeoVector[] edgeDirections =
            {
                r1[0].GetVectorTo(r1[1]),
                r1[0].GetVectorTo(r1[3]),
                r2[0].GetVectorTo(r2[1]),
                r2[0].GetVectorTo(r2[3])
            };

            foreach (var edgeDirection in edgeDirections)
            {
                // A rectangle with zero width or height has a degenerate edge. TryGetNormal skips that
                // axis instead of throwing; the remaining axes still separate the two boxes correctly.
                if (!edgeDirection.TryGetNormal(out GeoVector axis, tolerance)) continue;

                double min1 = double.MaxValue;
                double max1 = double.MinValue;
                foreach (var p in r1)
                {
                    double proj = p.X * axis.X + p.Y * axis.Y;
                    if (proj < min1) min1 = proj;
                    if (proj > max1) max1 = proj;
                }

                double min2 = double.MaxValue;
                double max2 = double.MinValue;
                foreach (var p in r2)
                {
                    double proj = p.X * axis.X + p.Y * axis.Y;
                    if (proj < min2) min2 = proj;
                    if (proj > max2) max2 = proj;
                }

                if (min2 - max1 > tolerance.EqualPoint || min1 - max2 > tolerance.EqualPoint)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks whether a rectangle collides with a line segment using default tolerance.
        /// </summary>
        public static bool CollidesWith(GeoRectangle rect, GeoLine line)
        {
            return CollidesWith(rect, line, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether a rectangle collides with a line segment within tolerance.
        /// </summary>
        public static bool CollidesWith(GeoRectangle rect, GeoLine line, Tolerance tolerance)
        {
            foreach (var edge in rect.GetEdges())
            {
                if (Intersection.TryIntersectWith(line, edge, out _, tolerance))
                {
                    return true;
                }
            }

            if (Containment.Contains(rect, line.StartPoint))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks whether a rectangle collides with a polygon using default tolerance.
        /// </summary>
        public static bool CollidesWith(GeoRectangle rect, GeoPolygon poly)
        {
            return CollidesWith(rect, poly, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether a rectangle collides with a polygon within tolerance.
        /// </summary>
        public static bool CollidesWith(GeoRectangle rect, GeoPolygon poly, Tolerance tolerance)
        {
            if (poly == null) throw new ArgumentNullException(nameof(poly));

            GeoLine[] rectEdges = rect.GetEdges();

            foreach (var polyEdge in poly.GetEdges())
            {
                foreach (var rectEdge in rectEdges)
                {
                    if (Intersection.TryIntersectWith(polyEdge, rectEdge, out _, tolerance))
                    {
                        return true;
                    }
                }
            }

            if (Containment.Contains(rect, poly.Vertices[0]))
            {
                return true;
            }

            if (Containment.Contains(poly, rect.Center, tolerance))
            {
                return true;
            }

            return false;
        }

        #endregion

        #region Polygon - Shapes

        /// <summary>
        /// Checks whether a polygon collides with a line segment using default tolerance.
        /// </summary>
        public static bool CollidesWith(GeoPolygon poly, GeoLine line)
        {
            return CollidesWith(poly, line, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether a polygon collides with a line segment within tolerance.
        /// </summary>
        public static bool CollidesWith(GeoPolygon poly, GeoLine line, Tolerance tolerance)
        {
            if (poly == null) throw new ArgumentNullException(nameof(poly));

            foreach (var polyEdge in poly.GetEdges())
            {
                if (Intersection.TryIntersectWith(line, polyEdge, out _, tolerance))
                {
                    return true;
                }
            }

            return Containment.Contains(poly, line.StartPoint, tolerance);
        }

        /// <summary>
        /// Checks whether two polygons collide using default tolerance.
        /// </summary>
        public static bool CollidesWith(GeoPolygon poly1, GeoPolygon poly2)
        {
            return CollidesWith(poly1, poly2, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether two polygons collide within tolerance.
        /// </summary>
        public static bool CollidesWith(GeoPolygon poly1, GeoPolygon poly2, Tolerance tolerance)
        {
            if (poly1 == null) throw new ArgumentNullException(nameof(poly1));
            if (poly2 == null) throw new ArgumentNullException(nameof(poly2));

            foreach (var edge1 in poly1.GetEdges())
            {
                foreach (var edge2 in poly2.GetEdges())
                {
                    if (Intersection.TryIntersectWith(edge1, edge2, out _, tolerance))
                    {
                        return true;
                    }
                }
            }

            return Containment.Contains(poly1, poly2.Vertices[0], tolerance) ||
                   Containment.Contains(poly2, poly1.Vertices[0], tolerance);
        }

        #endregion

        #region Polyline - Shapes

        /// <summary>
        /// Checks whether a polyline collides with a line segment using default tolerance.
        /// </summary>
        public static bool CollidesWith(GeoPolyline polyline, GeoLine line)
        {
            return CollidesWith(polyline, line, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether a polyline collides with a line segment within tolerance.
        /// </summary>
        public static bool CollidesWith(GeoPolyline polyline, GeoLine line, Tolerance tolerance)
        {
            if (polyline == null) throw new ArgumentNullException(nameof(polyline));

            for (int i = 0; i < polyline.EdgeCount; i++)
            {
                if (Intersection.TryIntersectWith(polyline.GetEdgeAt(i), line, out _, tolerance))
                {
                    return true;
                }
            }

            // A closed polyline encloses a region, so a segment lying entirely inside it collides even
            // though it crosses no edge. An open polyline encloses nothing and Contains reduces to
            // "lies on the path", which the edge loop above has already ruled out.
            return Containment.Contains(polyline, line.StartPoint, tolerance);
        }

        /// <summary>
        /// Checks whether a polyline collides with a rectangle using default tolerance.
        /// </summary>
        public static bool CollidesWith(GeoPolyline polyline, GeoRectangle rect)
        {
            return CollidesWith(polyline, rect, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether a polyline collides with a rectangle within tolerance.
        /// </summary>
        public static bool CollidesWith(GeoPolyline polyline, GeoRectangle rect, Tolerance tolerance)
        {
            if (polyline == null) throw new ArgumentNullException(nameof(polyline));

            for (int i = 0; i < polyline.EdgeCount; i++)
            {
                if (CollidesWith(rect, polyline.GetEdgeAt(i), tolerance))
                {
                    return true;
                }
            }

            // The rectangle may sit entirely inside a closed polyline without touching any edge.
            return Containment.Contains(polyline, rect.Center, tolerance);
        }

        /// <summary>
        /// Checks whether a polyline collides with a polygon using default tolerance.
        /// </summary>
        public static bool CollidesWith(GeoPolyline polyline, GeoPolygon poly)
        {
            return CollidesWith(polyline, poly, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether a polyline collides with a polygon within tolerance.
        /// </summary>
        public static bool CollidesWith(GeoPolyline polyline, GeoPolygon poly, Tolerance tolerance)
        {
            if (polyline == null) throw new ArgumentNullException(nameof(polyline));
            if (poly == null) throw new ArgumentNullException(nameof(poly));

            for (int i = 0; i < polyline.EdgeCount; i++)
            {
                if (CollidesWith(poly, polyline.GetEdgeAt(i), tolerance))
                {
                    return true;
                }
            }

            // The polygon may sit entirely inside a closed polyline without touching any edge.
            return Containment.Contains(polyline, poly.Vertices[0], tolerance);
        }

        /// <summary>
        /// Checks whether two polylines collide using default tolerance.
        /// </summary>
        public static bool CollidesWith(GeoPolyline pl1, GeoPolyline pl2)
        {
            return CollidesWith(pl1, pl2, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether two polylines collide within tolerance.
        /// </summary>
        public static bool CollidesWith(GeoPolyline pl1, GeoPolyline pl2, Tolerance tolerance)
        {
            if (pl1 == null) throw new ArgumentNullException(nameof(pl1));
            if (pl2 == null) throw new ArgumentNullException(nameof(pl2));

            for (int i = 0; i < pl1.EdgeCount; i++)
            {
                GeoLine edge1 = pl1.GetEdgeAt(i);
                for (int j = 0; j < pl2.EdgeCount; j++)
                {
                    GeoLine edge2 = pl2.GetEdgeAt(j);
                    if (Intersection.TryIntersectWith(edge1, edge2, out _, tolerance))
                    {
                        return true;
                    }
                }
            }

            // No edges cross, but one closed polyline may still fully enclose the other, which is the
            // same situation CollidesWith(poly1, poly2) reports as a collision.
            return Containment.Contains(pl1, pl2[0], tolerance) ||
                   Containment.Contains(pl2, pl1[0], tolerance);
        }

        #endregion
    }
}
