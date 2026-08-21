using System;
using System.Collections.Generic;
using ArrangeAlgorithms.Core;
using ArrangeAlgorithms.Enums;
using ArrangeAlgorithms.Datatype;

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
        /// Gets a value indicating whether the rectangle is rotated, that is whether its rotation angle
        /// differs from zero by more than the angular tolerance once full turns are removed.
        /// </summary>
        public bool IsRotated => Math.Abs(Angle.FromRadians(AngleRad).NormalizeSigned().Radians) > Tolerance.Global.EqualAngleRad;

        /// <summary>
        /// Gets the perimeter (total boundary length) of the rectangle.
        /// </summary>
        public double Length => 2.0 * (Width + Height);

        /// <summary>
        /// Initializes a new GeoRectangle instance from center, width, height, and rotation angle.
        /// </summary>
        /// <param name="center">Center point of the rectangle.</param>
        /// <param name="width">Rectangle width.</param>
        /// <param name="height">Rectangle height.</param>
        /// <param name="angleRad">Rotation angle in radians.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when width or height is negative.</exception>
        public GeoRectangle(GeoPoint center, double width, double height, double angleRad = 0.0)
        {
            if (width < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Width cannot be negative.");
            }

            if (height < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), "Height cannot be negative.");
            }

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
        /// Creates a copy of this rectangle.
        /// </summary>
        /// <remarks>
        /// Rectangle is a readonly struct, so plain assignment already produces an independent copy and
        /// this method is not needed to avoid sharing. It exists so that every geometry type offers the
        /// same way to ask for a copy.
        /// </remarks>
        /// <returns>A new rectangle with the same center, size, and rotation.</returns>
        public GeoRectangle Clone() => new GeoRectangle(Center, Width, Height, AngleRad);

        /// <summary>
        /// Converts this rectangle into a solid 2D GeoPolygon.
        /// </summary>
        /// <returns>A new GeoPolygon instance representing this rectangle.</returns>
        public GeoPolygon ToPolygon()
        {
            return new GeoPolygon(GetVertices());
        }

        /// <summary>
        /// Converts this rectangle's boundary into a closed 2D GeoPolyline.
        /// The boundary is closed by repeating the first vertex at the end of the chain.
        /// </summary>
        /// <returns>A new GeoPolyline instance representing the rectangle boundary.</returns>
        public GeoPolyline ToPolyline()
        {
            GeoPoint[] v = GetVertices();
            var polylineVertices = new GeoPoint[5];
            Array.Copy(v, polylineVertices, 4);
            polylineVertices[4] = v[0];
            return new GeoPolyline(polylineVertices);
        }

        /// <summary>
        /// Gets the point at a normalized parameter along this rectangle perimeter, where 0 is the LowerLeft corner and 1 is the end.
        /// Values outside [0, 1] wrap around, so 1.25 is the same position as 0.25.
        /// </summary>
        public GeoPoint GetPointAtParameter(double parameter) => Parametrization.GetPointAtParameter(this, parameter);

        /// <summary>
        /// Gets the normalized parameter of the point on this rectangle perimeter closest to the supplied point.
        /// </summary>
        public double GetParameterAtPoint(GeoPoint point) => Parametrization.GetParameterAtPoint(this, point);

        /// <summary>
        /// Gets the point at an arc length measured from the LowerLeft corner of this rectangle perimeter.
        /// </summary>
        public GeoPoint GetPointAtDistance(double distance) => Parametrization.GetPointAtDistance(this, distance);

        /// <summary>
        /// Gets the arc length from the LowerLeft corner of this rectangle perimeter to the point on it closest to the supplied point.
        /// </summary>
        public double GetDistanceAtPoint(GeoPoint point) => Parametrization.GetDistanceAtPoint(this, point);

        /// <summary>
        /// Gets the arc length from the LowerLeft corner of this rectangle perimeter to a normalized parameter.
        /// </summary>
        public double GetDistanceAtParameter(double parameter) => Parametrization.GetDistanceAtParameter(this, parameter);

        /// <summary>
        /// Gets the normalized parameter at an arc length measured from the LowerLeft corner of this rectangle perimeter.
        /// </summary>
        public double GetParameterAtDistance(double distance) => Parametrization.GetParameterAtDistance(this, distance);

        /// <summary>
        /// Translates the rectangle by a displacement vector.
        /// </summary>
        /// <param name="vector">The displacement vector.</param>
        /// <returns>A new translated GeoRectangle keeping the same size and rotation.</returns>
        public GeoRectangle Translate(GeoVector vector) => new GeoRectangle(Center.Add(vector), Width, Height, AngleRad);

        /// <summary>
        /// Rotates the rectangle around a center point by an angle in radians (counter-clockwise).
        /// </summary>
        /// <param name="angleRad">Rotation angle in radians.</param>
        /// <param name="center">Center of rotation.</param>
        /// <returns>A new rotated GeoRectangle.</returns>
        public GeoRectangle RotateBy(double angleRad, GeoPoint center) => new GeoRectangle(Center.RotateBy(angleRad, center), Width, Height, AngleRad + angleRad);

        /// <summary>
        /// Combines this rectangle with another rectangle, returning a new rectangle that encloses both.
        /// The resulting rectangle maintains the same orientation (angle) as this rectangle.
        /// </summary>
        /// <param name="other">The other rectangle to combine with.</param>
        /// <returns>A new GeoRectangle containing both rectangles, oriented along this rectangle's axis.</returns>
        public GeoRectangle Combine(GeoRectangle other) => Combine((IEnumerable<GeoPoint>)other.GetVertices());

        /// <summary>
        /// Combines this rectangle with a point, returning a new rectangle that encloses both.
        /// The resulting rectangle maintains the same orientation (angle) as this rectangle.
        /// </summary>
        /// <param name="point">The point to combine with.</param>
        /// <returns>A new GeoRectangle containing this rectangle and the point, oriented along this rectangle's axis.</returns>
        public GeoRectangle Combine(GeoPoint point)
        {
            double cos = Math.Cos(AngleRad);
            double sin = Math.Sin(AngleRad);

            double minX = Width * -0.5;
            double maxX = Width * 0.5;
            double minY = Height * -0.5;
            double maxY = Height * 0.5;

            ExpandLocalBounds(point, cos, sin, ref minX, ref maxX, ref minY, ref maxY);

            return FromLocalBounds(minX, maxX, minY, maxY, cos, sin);
        }

        /// <summary>
        /// Combines this rectangle with an array of points, returning a new rectangle that encloses this rectangle and all points.
        /// The resulting rectangle maintains the same orientation (angle) as this rectangle.
        /// </summary>
        /// <param name="points">The points to combine with. A null or empty array leaves the rectangle unchanged.</param>
        /// <returns>A new GeoRectangle containing this rectangle and all points, oriented along this rectangle's axis.</returns>
        public GeoRectangle Combine(params GeoPoint[] points) => Combine((IEnumerable<GeoPoint>)points);

        /// <summary>
        /// Combines this rectangle with a collection of points, returning a new rectangle that encloses this rectangle and all points.
        /// The resulting rectangle maintains the same orientation (angle) as this rectangle.
        /// </summary>
        /// <param name="points">The points to combine with. A null or empty sequence leaves the rectangle unchanged.</param>
        /// <returns>A new GeoRectangle containing this rectangle and all points, oriented along this rectangle's axis.</returns>
        public GeoRectangle Combine(IEnumerable<GeoPoint> points)
        {
            if (points == null)
            {
                return this;
            }

            double cos = Math.Cos(AngleRad);
            double sin = Math.Sin(AngleRad);

            double minX = Width * -0.5;
            double maxX = Width * 0.5;
            double minY = Height * -0.5;
            double maxY = Height * 0.5;

            bool anyPoint = false;
            foreach (var pt in points)
            {
                anyPoint = true;
                ExpandLocalBounds(pt, cos, sin, ref minX, ref maxX, ref minY, ref maxY);
            }

            if (!anyPoint)
            {
                return this;
            }

            return FromLocalBounds(minX, maxX, minY, maxY, cos, sin);
        }

        /// <summary>
        /// Projects a point into this rectangle's local axes and widens the running bounds to reach it.
        /// </summary>
        private void ExpandLocalBounds(GeoPoint point, double cos, double sin, ref double minX, ref double maxX, ref double minY, ref double maxY)
        {
            double dx = point.X - Center.X;
            double dy = point.Y - Center.Y;

            double localX = dx * cos + dy * sin;
            double localY = -dx * sin + dy * cos;

            if (localX < minX) minX = localX;
            if (localX > maxX) maxX = localX;
            if (localY < minY) minY = localY;
            if (localY > maxY) maxY = localY;
        }

        /// <summary>
        /// Rebuilds a rectangle on this one's axes from bounds expressed in its local coordinate system.
        /// </summary>
        private GeoRectangle FromLocalBounds(double minX, double maxX, double minY, double maxY, double cos, double sin)
        {
            double localCenterX = (minX + maxX) * 0.5;
            double localCenterY = (minY + maxY) * 0.5;

            double worldCenterX = Center.X + localCenterX * cos - localCenterY * sin;
            double worldCenterY = Center.Y + localCenterX * sin + localCenterY * cos;

            return new GeoRectangle(new GeoPoint(worldCenterX, worldCenterY), maxX - minX, maxY - minY, AngleRad);
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
        /// Gets the middle point of the bottom edge (between LowerLeft and LowerRight).
        /// </summary>
        public GeoPoint LowerMiddle => LowerLeft.GetMiddlePoint(LowerRight);

        /// <summary>
        /// Gets the middle point of the right edge (between LowerRight and UpperRight).
        /// </summary>
        public GeoPoint RightMiddle => LowerRight.GetMiddlePoint(UpperRight);

        /// <summary>
        /// Gets the middle point of the top edge (between UpperRight and UpperLeft).
        /// </summary>
        public GeoPoint UpperMiddle => UpperRight.GetMiddlePoint(UpperLeft);

        /// <summary>
        /// Gets the middle point of the left edge (between UpperLeft and LowerLeft).
        /// </summary>
        public GeoPoint LeftMiddle => UpperLeft.GetMiddlePoint(LowerLeft);

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
        /// Gets the closest point on the boundary of this rectangle to a target point, including for points
        /// inside the rectangle.
        /// </summary>
        public GeoPoint GetClosestPointOnBoundary(GeoPoint point) => Projection.ProjectToRectangle(this, point);

        /// <summary>
        /// Finds the shortest line segment connecting a point on the boundary of this rectangle to a point on a line segment using default tolerance.
        /// </summary>
        /// <remarks>
        /// Both ends of the returned segment sit on a boundary, so a shape lying entirely inside another
        /// still reports the gap out to its outline rather than zero. <see cref="ArrangeAlgorithms.Core.Distance"/>
        /// takes the opposite view and treats a closed shape as a filled region, returning zero for that
        /// same pair.
        /// </remarks>
        public GeoLine GetClosestOnBoundary(GeoLine line) => Projection.GetClosestSegment(this, line, Tolerance.Global);

        /// <summary>
        /// Finds the shortest line segment connecting a point on the boundary of this rectangle to a point on a line segment within tolerance.
        /// </summary>
        /// <remarks>
        /// Both ends of the returned segment sit on a boundary, so a shape lying entirely inside another
        /// still reports the gap out to its outline rather than zero. <see cref="ArrangeAlgorithms.Core.Distance"/>
        /// takes the opposite view and treats a closed shape as a filled region, returning zero for that
        /// same pair.
        /// </remarks>
        public GeoLine GetClosestOnBoundary(GeoLine line, Tolerance tolerance) => Projection.GetClosestSegment(this, line, tolerance);

        /// <summary>
        /// Finds the shortest line segment connecting a point on the boundary of this rectangle to a point on the circumference of a circle using default tolerance.
        /// </summary>
        /// <remarks>
        /// Both ends of the returned segment sit on a boundary, so a shape lying entirely inside another
        /// still reports the gap out to its outline rather than zero. <see cref="ArrangeAlgorithms.Core.Distance"/>
        /// takes the opposite view and treats a closed shape as a filled region, returning zero for that
        /// same pair.
        /// </remarks>
        public GeoLine GetClosestOnBoundary(GeoCircle circle) => Projection.GetClosestSegment(this, circle, Tolerance.Global);

        /// <summary>
        /// Finds the shortest line segment connecting a point on the boundary of this rectangle to a point on the circumference of a circle within tolerance.
        /// </summary>
        /// <remarks>
        /// Both ends of the returned segment sit on a boundary, so a shape lying entirely inside another
        /// still reports the gap out to its outline rather than zero. <see cref="ArrangeAlgorithms.Core.Distance"/>
        /// takes the opposite view and treats a closed shape as a filled region, returning zero for that
        /// same pair.
        /// </remarks>
        public GeoLine GetClosestOnBoundary(GeoCircle circle, Tolerance tolerance) => Projection.GetClosestSegment(this, circle, tolerance);

        /// <summary>
        /// Finds the shortest line segment connecting a point on the boundary of this rectangle to a point on the boundary of another rectangle using default tolerance.
        /// </summary>
        /// <remarks>
        /// Both ends of the returned segment sit on a boundary, so a shape lying entirely inside another
        /// still reports the gap out to its outline rather than zero. <see cref="ArrangeAlgorithms.Core.Distance"/>
        /// takes the opposite view and treats a closed shape as a filled region, returning zero for that
        /// same pair.
        /// </remarks>
        public GeoLine GetClosestOnBoundary(GeoRectangle other) => Projection.GetClosestSegment(this, other, Tolerance.Global);

        /// <summary>
        /// Finds the shortest line segment connecting a point on the boundary of this rectangle to a point on the boundary of another rectangle within tolerance.
        /// </summary>
        /// <remarks>
        /// Both ends of the returned segment sit on a boundary, so a shape lying entirely inside another
        /// still reports the gap out to its outline rather than zero. <see cref="ArrangeAlgorithms.Core.Distance"/>
        /// takes the opposite view and treats a closed shape as a filled region, returning zero for that
        /// same pair.
        /// </remarks>
        public GeoLine GetClosestOnBoundary(GeoRectangle other, Tolerance tolerance) => Projection.GetClosestSegment(this, other, tolerance);

        /// <summary>
        /// Finds the shortest line segment connecting a point on the boundary of this rectangle to a point on a polyline using default tolerance.
        /// </summary>
        /// <remarks>
        /// Both ends of the returned segment sit on a boundary, so a shape lying entirely inside another
        /// still reports the gap out to its outline rather than zero. <see cref="ArrangeAlgorithms.Core.Distance"/>
        /// takes the opposite view and treats a closed shape as a filled region, returning zero for that
        /// same pair.
        /// </remarks>
        public GeoLine GetClosestOnBoundary(GeoPolyline polyline) => Projection.GetClosestSegment(this, polyline, Tolerance.Global);

        /// <summary>
        /// Finds the shortest line segment connecting a point on the boundary of this rectangle to a point on a polyline within tolerance.
        /// </summary>
        /// <remarks>
        /// Both ends of the returned segment sit on a boundary, so a shape lying entirely inside another
        /// still reports the gap out to its outline rather than zero. <see cref="ArrangeAlgorithms.Core.Distance"/>
        /// takes the opposite view and treats a closed shape as a filled region, returning zero for that
        /// same pair.
        /// </remarks>
        public GeoLine GetClosestOnBoundary(GeoPolyline polyline, Tolerance tolerance) => Projection.GetClosestSegment(this, polyline, tolerance);

        /// <summary>
        /// Finds the shortest line segment connecting a point on the boundary of this rectangle to a point on the boundary of a polygon using default tolerance.
        /// </summary>
        /// <remarks>
        /// Both ends of the returned segment sit on a boundary, so a shape lying entirely inside another
        /// still reports the gap out to its outline rather than zero. <see cref="ArrangeAlgorithms.Core.Distance"/>
        /// takes the opposite view and treats a closed shape as a filled region, returning zero for that
        /// same pair.
        /// </remarks>
        public GeoLine GetClosestOnBoundary(GeoPolygon poly) => Projection.GetClosestSegment(this, poly, Tolerance.Global);

        /// <summary>
        /// Finds the shortest line segment connecting a point on the boundary of this rectangle to a point on the boundary of a polygon within tolerance.
        /// </summary>
        /// <remarks>
        /// Both ends of the returned segment sit on a boundary, so a shape lying entirely inside another
        /// still reports the gap out to its outline rather than zero. <see cref="ArrangeAlgorithms.Core.Distance"/>
        /// takes the opposite view and treats a closed shape as a filled region, returning zero for that
        /// same pair.
        /// </remarks>
        public GeoLine GetClosestOnBoundary(GeoPolygon poly, Tolerance tolerance) => Projection.GetClosestSegment(this, poly, tolerance);

        /// <summary>
        /// Calculates the shortest Euclidean distance from this rectangle to a point.
        /// </summary>
        public double DistanceTo(GeoPoint point) => Distance.DistanceTo(this, point);

        /// <summary>
        /// Calculates the shortest boundary distance from this rectangle to a polygon.
        /// </summary>
        public double DistanceTo(GeoPolygon poly) => Distance.DistanceTo(this, poly);

        /// <summary>
        /// Calculates the shortest boundary distance from this rectangle to a line segment.
        /// </summary>
        public double DistanceTo(GeoLine GeoLine) => Distance.DistanceTo(this, GeoLine);

        /// <summary>
        /// Calculates the shortest boundary distance from this rectangle to a circle.
        /// </summary>
        public double DistanceTo(GeoCircle circle) => Distance.DistanceTo(circle, this);

        /// <summary>
        /// Calculates the shortest distance from this rectangle to a polyline.
        /// </summary>
        public double DistanceTo(GeoPolyline polyline) => Distance.DistanceTo(polyline, this);

        /// <summary>
        /// Calculates the shortest boundary distance from this rectangle to another rectangle.
        /// </summary>
        public double DistanceTo(GeoRectangle other) => Distance.DistanceTo(this, other);

        /// <summary>
        /// Checks whether the rectangle contains a point.
        /// </summary>
        public bool Contains(GeoPoint GeoPoint) => Containment.Contains(this, GeoPoint);

        /// <summary>
        /// Checks whether the rectangle entirely contains a line segment using default tolerance.
        /// </summary>
        public bool Contains(GeoLine line) => Containment.Contains(this, line, Tolerance.Global);

        /// <summary>
        /// Checks whether the rectangle entirely contains a line segment within tolerance.
        /// </summary>
        public bool Contains(GeoLine line, Tolerance tolerance) => Containment.Contains(this, line, tolerance);

        /// <summary>
        /// Checks whether the rectangle entirely contains a polyline using default tolerance.
        /// </summary>
        public bool Contains(GeoPolyline polyline) => Containment.Contains(this, polyline, Tolerance.Global);

        /// <summary>
        /// Checks whether the rectangle entirely contains a polyline within tolerance.
        /// </summary>
        public bool Contains(GeoPolyline polyline, Tolerance tolerance) => Containment.Contains(this, polyline, tolerance);

        /// <summary>
        /// Classifies the location of a point relative to this rectangle (Inside, OutSide, or OnSide) using default tolerance.
        /// </summary>
        public PointLocation Locate(GeoPoint point) => Containment.Locate(this, point, Tolerance.Global);

        /// <summary>
        /// Classifies the location of a point relative to this rectangle (Inside, OutSide, or OnSide) within tolerance.
        /// </summary>
        public PointLocation Locate(GeoPoint point, Tolerance tolerance) => Containment.Locate(this, point, tolerance);

        /// <summary>
        /// Checks whether this rectangle collides with another rectangle using default tolerance.
        /// </summary>
        public bool CollidesWith(GeoRectangle other) => Collision.CollidesWith(this, other, Tolerance.Global);

        /// <summary>
        /// Checks whether this rectangle collides with another rectangle within tolerance.
        /// </summary>
        public bool CollidesWith(GeoRectangle other, Tolerance tolerance) => Collision.CollidesWith(this, other, tolerance);

        /// <summary>
        /// Checks whether this rectangle collides with a line segment using default tolerance.
        /// </summary>
        public bool CollidesWith(GeoLine geoLine) => Collision.CollidesWith(this, geoLine, Tolerance.Global);

        /// <summary>
        /// Checks whether this rectangle collides with a line segment within tolerance.
        /// </summary>
        public bool CollidesWith(GeoLine geoLine, Tolerance tolerance) => Collision.CollidesWith(this, geoLine, tolerance);

        /// <summary>
        /// Checks whether this rectangle collides with a polygon using default tolerance.
        /// </summary>
        public bool CollidesWith(GeoPolygon poly) => Collision.CollidesWith(this, poly, Tolerance.Global);

        /// <summary>
        /// Checks whether this rectangle collides with a polygon within tolerance.
        /// </summary>
        public bool CollidesWith(GeoPolygon poly, Tolerance tolerance) => Collision.CollidesWith(this, poly, tolerance);

        /// <summary>
        /// Checks whether this rectangle collides with a circle using default tolerance.
        /// </summary>
        public bool CollidesWith(GeoCircle circle) => Collision.CollidesWith(circle, this, Tolerance.Global);

        /// <summary>
        /// Checks whether this rectangle collides with a circle within tolerance.
        /// </summary>
        public bool CollidesWith(GeoCircle circle, Tolerance tolerance) => Collision.CollidesWith(circle, this, tolerance);

        /// <summary>
        /// Checks whether this rectangle collides with a polyline using default tolerance.
        /// </summary>
        public bool CollidesWith(GeoPolyline polyline) => Collision.CollidesWith(polyline, this, Tolerance.Global);

        /// <summary>
        /// Checks whether this rectangle collides with a polyline within tolerance.
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
        /// Gets all intersection points with another rectangle using default tolerance.
        /// </summary>
        public GeoPoint[] GetIntersections(GeoRectangle other) => Intersection.GetIntersections(this, other, Tolerance.Global);

        /// <summary>
        /// Gets all intersection points with another rectangle within tolerance.
        /// </summary>
        public GeoPoint[] GetIntersections(GeoRectangle other, Tolerance tolerance) => Intersection.GetIntersections(this, other, tolerance);

        /// <summary>
        /// Gets all intersection points with a polygon using default tolerance.
        /// </summary>
        public GeoPoint[] GetIntersections(GeoPolygon poly) => Intersection.GetIntersections(poly, this, Tolerance.Global);

        /// <summary>
        /// Gets all intersection points with a polygon within tolerance.
        /// </summary>
        public GeoPoint[] GetIntersections(GeoPolygon poly, Tolerance tolerance) => Intersection.GetIntersections(poly, this, tolerance);

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
        /// Checks whether a line segment is parallel to this rectangle's axes using default tolerance.
        /// </summary>
        public bool IsParallelTo(GeoLine line) => Parallel.IsParallel(this, line, Tolerance.Global);

        /// <summary>
        /// Checks whether a line segment is parallel to this rectangle's axes within angular tolerance.
        /// </summary>
        public bool IsParallelTo(GeoLine line, Tolerance tolerance) => Parallel.IsParallel(this, line, tolerance);

        /// <summary>
        /// Checks whether another rectangle is parallel in orientation to this rectangle using default tolerance.
        /// </summary>
        public bool IsParallelTo(GeoRectangle other) => Parallel.IsParallel(this, other, Tolerance.Global);

        /// <summary>
        /// Checks whether another rectangle is parallel in orientation to this rectangle within angular tolerance.
        /// </summary>
        public bool IsParallelTo(GeoRectangle other, Tolerance tolerance) => Parallel.IsParallel(this, other, tolerance);

        /// <summary>
        /// Translates a rectangle by a vector.
        /// </summary>
        public static GeoRectangle operator +(GeoRectangle rect, GeoVector vector) => rect.Translate(vector);

        /// <summary>
        /// Translates a rectangle backwards by a vector.
        /// </summary>
        public static GeoRectangle operator -(GeoRectangle rect, GeoVector vector) => rect.Translate(-vector);

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
        public override bool Equals(object obj) => obj is GeoRectangle other && Equals(other);

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
        public override string ToString() => $"GeoRectangle[Center:{Center}, Width:{Width:0.000}, Height:{Height:0.000}, AngleRad:{AngleRad:0.000}]";
    }
}
