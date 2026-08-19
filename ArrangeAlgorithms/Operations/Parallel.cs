using System;
using ArrangeAlgorithms.Geometry;

namespace ArrangeAlgorithms.Operations
{
    /// <summary>
    /// Provides static calculation methods for checking parallelism and perpendicularity between geometric entities.
    /// </summary>
    public static class Parallel
    {
        #region Vector - Vector

        /// <summary>
        /// Checks whether two vectors are parallel (or anti-parallel) using default tolerance.
        /// </summary>
        /// <param name="v1">The first vector.</param>
        /// <param name="v2">The second vector.</param>
        /// <returns>true if the vectors are parallel; otherwise, false.</returns>
        public static bool IsParallel(GeoVector v1, GeoVector v2)
        {
            return IsParallel(v1, v2, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether two vectors are parallel (or anti-parallel) within angular tolerance.
        /// </summary>
        /// <param name="v1">The first vector.</param>
        /// <param name="v2">The second vector.</param>
        /// <param name="tolerance">The tolerance containing the angular threshold in radians.</param>
        /// <returns>true if the vectors are parallel within tolerance; otherwise, false.</returns>
        public static bool IsParallel(GeoVector v1, GeoVector v2, Tolerance tolerance)
        {
            if (!v1.TryGetNormal(out GeoVector u1, tolerance) || !v2.TryGetNormal(out GeoVector u2, tolerance))
            {
                return false;
            }

            // Acos loses precision exactly where this test lives: near parallel the dot product sits at
            // ~1 and Acos amplifies its rounding error. Atan2(|cross|, |dot|) is stable across the whole
            // range, which is the same reasoning GeoVector.GetAngleTo documents.
            double angle = Math.Atan2(Math.Abs(u1.CrossProduct(u2)), Math.Abs(u1.DotProduct(u2)));

            return angle <= tolerance.EqualAngleRad;
        }

        /// <summary>
        /// Checks whether two vectors are perpendicular using default tolerance.
        /// </summary>
        /// <param name="v1">The first vector.</param>
        /// <param name="v2">The second vector.</param>
        /// <returns>true if the vectors are perpendicular; otherwise, false.</returns>
        public static bool IsPerpendicular(GeoVector v1, GeoVector v2)
        {
            return IsPerpendicular(v1, v2, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether two vectors are perpendicular within angular tolerance.
        /// </summary>
        /// <param name="v1">The first vector.</param>
        /// <param name="v2">The second vector.</param>
        /// <param name="tolerance">The tolerance containing the angular threshold in radians.</param>
        /// <returns>true if the vectors are perpendicular within tolerance; otherwise, false.</returns>
        public static bool IsPerpendicular(GeoVector v1, GeoVector v2, Tolerance tolerance)
        {
            if (!v1.TryGetNormal(out GeoVector u1, tolerance) || !v2.TryGetNormal(out GeoVector u2, tolerance))
            {
                return false;
            }

            double dot = Math.Abs(u1.DotProduct(u2));
            return dot <= tolerance.EqualAngleSin;
        }

        #endregion

        #region Line - Line

        /// <summary>
        /// Checks whether two line segments are parallel using default tolerance.
        /// </summary>
        /// <param name="line1">The first line segment.</param>
        /// <param name="line2">The second line segment.</param>
        /// <returns>true if the line segments are parallel; otherwise, false.</returns>
        public static bool IsParallel(GeoLine line1, GeoLine line2)
        {
            return IsParallel(line1, line2, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether two line segments are parallel within angular tolerance.
        /// </summary>
        /// <param name="line1">The first line segment.</param>
        /// <param name="line2">The second line segment.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>true if the line segments are parallel within tolerance; otherwise, false.</returns>
        public static bool IsParallel(GeoLine line1, GeoLine line2, Tolerance tolerance)
        {
            return IsParallel(line1.Direction, line2.Direction, tolerance);
        }

        /// <summary>
        /// Checks whether two line segments are perpendicular using default tolerance.
        /// </summary>
        /// <param name="line1">The first line segment.</param>
        /// <param name="line2">The second line segment.</param>
        /// <returns>true if the line segments are perpendicular; otherwise, false.</returns>
        public static bool IsPerpendicular(GeoLine line1, GeoLine line2)
        {
            return IsPerpendicular(line1, line2, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether two line segments are perpendicular within angular tolerance.
        /// </summary>
        /// <param name="line1">The first line segment.</param>
        /// <param name="line2">The second line segment.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>true if the line segments are perpendicular within tolerance; otherwise, false.</returns>
        public static bool IsPerpendicular(GeoLine line1, GeoLine line2, Tolerance tolerance)
        {
            return IsPerpendicular(line1.Direction, line2.Direction, tolerance);
        }

        #endregion

        #region Line - Vector

        /// <summary>
        /// Checks whether a line segment is parallel to a vector using default tolerance.
        /// </summary>
        /// <param name="line">The line segment.</param>
        /// <param name="vector">The vector.</param>
        /// <returns>true if parallel; otherwise, false.</returns>
        public static bool IsParallel(GeoLine line, GeoVector vector)
        {
            return IsParallel(line.Direction, vector, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether a line segment is parallel to a vector within angular tolerance.
        /// </summary>
        /// <param name="line">The line segment.</param>
        /// <param name="vector">The vector.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>true if parallel; otherwise, false.</returns>
        public static bool IsParallel(GeoLine line, GeoVector vector, Tolerance tolerance)
        {
            return IsParallel(line.Direction, vector, tolerance);
        }

        /// <summary>
        /// Checks whether a line segment is perpendicular to a vector using default tolerance.
        /// </summary>
        public static bool IsPerpendicular(GeoLine line, GeoVector vector)
        {
            return IsPerpendicular(line.Direction, vector, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether a line segment is perpendicular to a vector within angular tolerance.
        /// </summary>
        public static bool IsPerpendicular(GeoLine line, GeoVector vector, Tolerance tolerance)
        {
            return IsPerpendicular(line.Direction, vector, tolerance);
        }

        #endregion

        #region Rectangle - Line / Rectangle - Rectangle

        /// <summary>
        /// Checks whether a line segment is parallel to any edge of a rotated rectangle using default tolerance.
        /// </summary>
        /// <param name="rect">The rectangle.</param>
        /// <param name="line">The line segment.</param>
        /// <returns>true if parallel to the rectangle width or height axes; otherwise, false.</returns>
        public static bool IsParallel(GeoRectangle rect, GeoLine line)
        {
            return IsParallel(rect, line, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether a line segment is parallel to any edge of a rotated rectangle within angular tolerance.
        /// </summary>
        /// <param name="rect">The rectangle.</param>
        /// <param name="line">The line segment.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>true if parallel to the rectangle width or height axes; otherwise, false.</returns>
        public static bool IsParallel(GeoRectangle rect, GeoLine line, Tolerance tolerance)
        {
            GeoVector rectXAxis = new GeoVector(Math.Cos(rect.AngleRad), Math.Sin(rect.AngleRad));
            GeoVector rectYAxis = new GeoVector(-Math.Sin(rect.AngleRad), Math.Cos(rect.AngleRad));

            return IsParallel(line.Direction, rectXAxis, tolerance) || IsParallel(line.Direction, rectYAxis, tolerance);
        }

        /// <summary>
        /// Checks whether two rotated rectangles have parallel orientation (parallel axes) using default tolerance.
        /// </summary>
        /// <param name="rect1">The first rectangle.</param>
        /// <param name="rect2">The second rectangle.</param>
        /// <returns>true if their axes are parallel; otherwise, false.</returns>
        public static bool IsParallel(GeoRectangle rect1, GeoRectangle rect2)
        {
            return IsParallel(rect1, rect2, Tolerance.Global);
        }

        /// <summary>
        /// Checks whether two rotated rectangles have parallel orientation within angular tolerance.
        /// </summary>
        /// <param name="rect1">The first rectangle.</param>
        /// <param name="rect2">The second rectangle.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>true if their axes are parallel within tolerance; otherwise, false.</returns>
        public static bool IsParallel(GeoRectangle rect1, GeoRectangle rect2, Tolerance tolerance)
        {
            GeoVector axis1 = new GeoVector(Math.Cos(rect1.AngleRad), Math.Sin(rect1.AngleRad));
            GeoVector axis2 = new GeoVector(Math.Cos(rect2.AngleRad), Math.Sin(rect2.AngleRad));
            GeoVector axis2Perp = new GeoVector(-Math.Sin(rect2.AngleRad), Math.Cos(rect2.AngleRad));

            return IsParallel(axis1, axis2, tolerance) || IsParallel(axis1, axis2Perp, tolerance);
        }

        #endregion
    }
}
