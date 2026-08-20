using System;
using ArrangeAlgorithms.Core;
using ArrangeAlgorithms.Enums;

namespace ArrangeAlgorithms.Geometry
{
    /// <summary>
    /// Represents a 2D circle with a center point and radius.
    /// </summary>
    public readonly struct GeoCircle : IEquatable<GeoCircle>
    {
        /// <summary>
        /// Gets the center point of the circle.
        /// </summary>
        public GeoPoint Center { get; }

        /// <summary>
        /// Gets the radius of the circle.
        /// </summary>
        public double Radius { get; }

        /// <summary>
        /// Gets the diameter of the circle.
        /// </summary>
        public double Diameter => Radius * 2.0;

        /// <summary>
        /// Gets the circumference (perimeter) of the circle.
        /// </summary>
        public double Circumference => 2.0 * Math.PI * Radius;

        /// <summary>
        /// Gets the area of the circle.
        /// </summary>
        public double Area => Math.PI * Radius * Radius;

        /// <summary>
        /// Initializes a new GeoCircle instance from a center point and radius.
        /// </summary>
        /// <param name="center">Center point of the circle.</param>
        /// <param name="radius">Radius of the circle (must be non-negative).</param>
        public GeoCircle(GeoPoint center, double radius)
        {
            if (radius < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(radius), "Radius cannot be negative.");
            }
            Center = center;
            Radius = radius;
        }

        /// <summary>
        /// Initializes a new GeoCircle instance from center coordinates and radius.
        /// </summary>
        /// <param name="centerX">X coordinate of the center point.</param>
        /// <param name="centerY">Y coordinate of the center point.</param>
        /// <param name="radius">Radius of the circle (must be non-negative).</param>
        public GeoCircle(double centerX, double centerY, double radius)
            : this(new GeoPoint(centerX, centerY), radius)
        {
        }

        /// <summary>
        /// Creates a copy of this circle.
        /// </summary>
        /// <remarks>
        /// Circle is a readonly struct, so plain assignment already produces an independent copy and
        /// this method is not needed to avoid sharing. It exists so that every geometry type offers the
        /// same way to ask for a copy.
        /// </remarks>
        /// <returns>A new circle with the same center and radius.</returns>
        public GeoCircle Clone() => new GeoCircle(Center, Radius);

        /// <summary>
        /// Converts this circle into an oriented bounding GeoRectangle with the specified rotation angle.
        /// </summary>
        /// <param name="angleRad">The rotation angle of the resulting rectangle in radians.</param>
        /// <returns>A new GeoRectangle instance representing the oriented bounding box of this circle.</returns>
        public GeoRectangle ToRectangle(double angleRad) => new GeoRectangle(Center, Diameter, Diameter, angleRad);

        /// <summary>
        /// Gets the point at a normalized parameter along this circle, where 0 is angle zero and 1 is the end.
        /// Values outside [0, 1] wrap around, so 1.25 is the same position as 0.25.
        /// </summary>
        public GeoPoint GetPointAtParameter(double parameter) => Parametrization.GetPointAtParameter(this, parameter);

        /// <summary>
        /// Gets the normalized parameter of the point on this circle closest to the supplied point.
        /// </summary>
        public double GetParameterAtPoint(GeoPoint point) => Parametrization.GetParameterAtPoint(this, point);

        /// <summary>
        /// Gets the point at an arc length measured from angle zero of this circle.
        /// </summary>
        public GeoPoint GetPointAtDistance(double distance) => Parametrization.GetPointAtDistance(this, distance);

        /// <summary>
        /// Gets the arc length from angle zero of this circle to the point on it closest to the supplied point.
        /// </summary>
        public double GetDistanceAtPoint(GeoPoint point) => Parametrization.GetDistanceAtPoint(this, point);

        /// <summary>
        /// Gets the arc length from angle zero of this circle to a normalized parameter.
        /// </summary>
        public double GetDistanceAtParameter(double parameter) => Parametrization.GetDistanceAtParameter(this, parameter);

        /// <summary>
        /// Gets the normalized parameter at an arc length measured from angle zero of this circle.
        /// </summary>
        public double GetParameterAtDistance(double distance) => Parametrization.GetParameterAtDistance(this, distance);

        /// <summary>
        /// Translates the circle by a displacement vector.
        /// </summary>
        /// <param name="vector">The displacement vector.</param>
        /// <returns>A new translated GeoCircle.</returns>
        public GeoCircle Translate(GeoVector vector) => new GeoCircle(Center.Add(vector), Radius);

        /// <summary>
        /// Calculates the shortest boundary distance from this circle to a point.
        /// </summary>
        public double DistanceTo(GeoPoint point) => Distance.DistanceTo(this, point);

        /// <summary>
        /// Calculates the shortest boundary distance from this circle to a line segment.
        /// </summary>
        public double DistanceTo(GeoLine line) => Distance.DistanceTo(this, line);

        /// <summary>
        /// Calculates the shortest boundary distance from this circle to another circle.
        /// </summary>
        public double DistanceTo(GeoCircle other) => Distance.DistanceTo(this, other);

        /// <summary>
        /// Calculates the shortest boundary distance from this circle to a rectangle.
        /// </summary>
        public double DistanceTo(GeoRectangle rect) => Distance.DistanceTo(this, rect);

        /// <summary>
        /// Calculates the shortest boundary distance from this circle to a polygon.
        /// </summary>
        public double DistanceTo(GeoPolygon poly) => Distance.DistanceTo(this, poly);

        /// <summary>
        /// Calculates the shortest boundary distance from this circle to a polyline.
        /// </summary>
        public double DistanceTo(GeoPolyline polyline) => Distance.DistanceTo(polyline, this);

        /// <summary>
        /// Gets the closest point on the circumference of this circle to a target point, including for
        /// points inside the circle.
        /// </summary>
        public GeoPoint GetClosestPointOnBoundary(GeoPoint point) => Projection.ProjectToCircle(this, point);

        /// <summary>
        /// Checks whether the circle contains a point using default tolerance.
        /// </summary>
        public bool Contains(GeoPoint point) => Containment.Contains(this, point, Tolerance.Global);

        /// <summary>
        /// Checks whether the circle contains a point within tolerance.
        /// </summary>
        public bool Contains(GeoPoint point, Tolerance tolerance) => Containment.Contains(this, point, tolerance);

        /// <summary>
        /// Classifies the location of a point relative to this circle (Inside, OutSide, or OnSide) using default tolerance.
        /// </summary>
        public PointLocation Locate(GeoPoint point) => Containment.Locate(this, point, Tolerance.Global);

        /// <summary>
        /// Classifies the location of a point relative to this circle (Inside, OutSide, or OnSide) within tolerance.
        /// </summary>
        public PointLocation Locate(GeoPoint point, Tolerance tolerance) => Containment.Locate(this, point, tolerance);

        /// <summary>
        /// Checks whether the circle entirely contains another circle using default tolerance.
        /// </summary>
        public bool Contains(GeoCircle other) => Containment.Contains(this, other, Tolerance.Global);

        /// <summary>
        /// Checks whether the circle entirely contains another circle within tolerance.
        /// </summary>
        public bool Contains(GeoCircle other, Tolerance tolerance) => Containment.Contains(this, other, tolerance);

        /// <summary>
        /// Checks whether the circle entirely contains a line segment using default tolerance.
        /// </summary>
        public bool Contains(GeoLine line) => Containment.Contains(this, line, Tolerance.Global);

        /// <summary>
        /// Checks whether the circle entirely contains a line segment within tolerance.
        /// </summary>
        public bool Contains(GeoLine line, Tolerance tolerance) => Containment.Contains(this, line, tolerance);

        /// <summary>
        /// Checks whether a point lies on the circle circumference using default tolerance.
        /// </summary>
        public bool IsPointOn(GeoPoint point) => Containment.IsPointOn(this, point, Tolerance.Global);

        /// <summary>
        /// Checks whether a point lies on the circle circumference within tolerance.
        /// </summary>
        public bool IsPointOn(GeoPoint point, Tolerance tolerance) => Containment.IsPointOn(this, point, tolerance);

        /// <summary>
        /// Checks whether this circle collides with another circle using default tolerance.
        /// </summary>
        public bool CollidesWith(GeoCircle other) => Collision.CollidesWith(this, other, Tolerance.Global);

        /// <summary>
        /// Checks whether this circle collides with another circle within tolerance.
        /// </summary>
        public bool CollidesWith(GeoCircle other, Tolerance tolerance) => Collision.CollidesWith(this, other, tolerance);

        /// <summary>
        /// Checks whether this circle collides with a line segment using default tolerance.
        /// </summary>
        public bool CollidesWith(GeoLine line) => Collision.CollidesWith(this, line, Tolerance.Global);

        /// <summary>
        /// Checks whether this circle collides with a line segment within tolerance.
        /// </summary>
        public bool CollidesWith(GeoLine line, Tolerance tolerance) => Collision.CollidesWith(this, line, tolerance);

        /// <summary>
        /// Checks whether this circle collides with a rotated rectangle using default tolerance.
        /// </summary>
        public bool CollidesWith(GeoRectangle rect) => Collision.CollidesWith(this, rect, Tolerance.Global);

        /// <summary>
        /// Checks whether this circle collides with a rotated rectangle within tolerance.
        /// </summary>
        public bool CollidesWith(GeoRectangle rect, Tolerance tolerance) => Collision.CollidesWith(this, rect, tolerance);

        /// <summary>
        /// Checks whether this circle collides with a polygon using default tolerance.
        /// </summary>
        public bool CollidesWith(GeoPolygon poly) => Collision.CollidesWith(this, poly, Tolerance.Global);

        /// <summary>
        /// Checks whether this circle collides with a polygon within tolerance.
        /// </summary>
        public bool CollidesWith(GeoPolygon poly, Tolerance tolerance) => Collision.CollidesWith(this, poly, tolerance);

        /// <summary>
        /// Checks whether this circle collides with a polyline using default tolerance.
        /// </summary>
        public bool CollidesWith(GeoPolyline polyline) => Collision.CollidesWith(this, polyline, Tolerance.Global);

        /// <summary>
        /// Checks whether this circle collides with a polyline within tolerance.
        /// </summary>
        public bool CollidesWith(GeoPolyline polyline, Tolerance tolerance) => Collision.CollidesWith(this, polyline, tolerance);

        /// <summary>
        /// Gets all intersection points with another circle using default tolerance.
        /// </summary>
        public GeoPoint[] GetIntersections(GeoCircle other) => Intersection.GetIntersections(this, other, Tolerance.Global);

        /// <summary>
        /// Gets all intersection points with another circle within tolerance.
        /// </summary>
        public GeoPoint[] GetIntersections(GeoCircle other, Tolerance tolerance) => Intersection.GetIntersections(this, other, tolerance);

        /// <summary>
        /// Gets all intersection points with a line segment using default tolerance.
        /// </summary>
        public GeoPoint[] GetIntersections(GeoLine line) => Intersection.GetIntersections(this, line, Tolerance.Global);

        /// <summary>
        /// Gets all intersection points with a line segment within tolerance.
        /// </summary>
        public GeoPoint[] GetIntersections(GeoLine line, Tolerance tolerance) => Intersection.GetIntersections(this, line, tolerance);

        /// <summary>
        /// Gets all intersection points with a rectangle using default tolerance.
        /// </summary>
        public GeoPoint[] GetIntersections(GeoRectangle rect) => Intersection.GetIntersections(rect, this, Tolerance.Global);

        /// <summary>
        /// Gets all intersection points with a rectangle within tolerance.
        /// </summary>
        public GeoPoint[] GetIntersections(GeoRectangle rect, Tolerance tolerance) => Intersection.GetIntersections(rect, this, tolerance);

        /// <summary>
        /// Gets all intersection points with a polygon using default tolerance.
        /// </summary>
        public GeoPoint[] GetIntersections(GeoPolygon poly) => Intersection.GetIntersections(poly, this, Tolerance.Global);

        /// <summary>
        /// Gets all intersection points with a polygon within tolerance.
        /// </summary>
        public GeoPoint[] GetIntersections(GeoPolygon poly, Tolerance tolerance) => Intersection.GetIntersections(poly, this, tolerance);

        /// <summary>
        /// Gets all intersection points with a polyline using default tolerance.
        /// </summary>
        public GeoPoint[] GetIntersections(GeoPolyline polyline) => Intersection.GetIntersections(polyline, this, Tolerance.Global);

        /// <summary>
        /// Gets all intersection points with a polyline within tolerance.
        /// </summary>
        public GeoPoint[] GetIntersections(GeoPolyline polyline, Tolerance tolerance) => Intersection.GetIntersections(polyline, this, tolerance);

        /// <summary>
        /// Translates a circle by a vector.
        /// </summary>
        public static GeoCircle operator +(GeoCircle circle, GeoVector vector) => circle.Translate(vector);

        /// <summary>
        /// Translates a circle backwards by a vector.
        /// </summary>
        public static GeoCircle operator -(GeoCircle circle, GeoVector vector) => circle.Translate(-vector);

        /// <summary>
        /// Indicates whether the current circle is equal to another circle.
        /// </summary>
        public bool Equals(GeoCircle other) => Center.Equals(other.Center) && Radius.Equals(other.Radius);

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        public override bool Equals(object obj) => obj is GeoCircle other && Equals(other);

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                return (Center.GetHashCode() * 397) ^ Radius.GetHashCode();
            }
        }

        /// <summary>
        /// Compares two GeoCircle instances for equality.
        /// </summary>
        public static bool operator ==(GeoCircle left, GeoCircle right) => left.Equals(right);

        /// <summary>
        /// Compares two GeoCircle instances for inequality.
        /// </summary>
        public static bool operator !=(GeoCircle left, GeoCircle right) => !left.Equals(right);

        /// <summary>
        /// Returns the string representation of the circle.
        /// </summary>
        public override string ToString() => $"GeoCircle[Center:{Center}, Radius:{Radius:0.000}]";

    }
}
