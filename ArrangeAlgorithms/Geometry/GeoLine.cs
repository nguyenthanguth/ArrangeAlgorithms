using System;
using System.Collections.Generic;
using ArrangeAlgorithms.Core;
using ArrangeAlgorithms.Enums;

namespace ArrangeAlgorithms.Geometry
{
    /// <summary>
    /// Represents a 2D line segment between start and end points.
    /// </summary>
    public readonly struct GeoLine : IEquatable<GeoLine>
    {
        /// <summary>
        /// Gets the start point of the line segment.
        /// </summary>
        public GeoPoint StartPoint { get; }

        /// <summary>
        /// Gets the end point of the line segment.
        /// </summary>
        public GeoPoint EndPoint { get; }

        /// <summary>
        /// Initializes a new GeoLine instance from start and end points.
        /// </summary>
        /// <param name="startPoint">Start point.</param>
        /// <param name="endPoint">End point.</param>
        public GeoLine(GeoPoint startPoint, GeoPoint endPoint)
        {
            StartPoint = startPoint;
            EndPoint = endPoint;
        }

        /// <summary>
        /// Initializes a new GeoLine instance from the coordinates of the endpoints.
        /// </summary>
        public GeoLine(double startX, double startY, double endX, double endY)
            : this(new GeoPoint(startX, startY), new GeoPoint(endX, endY))
        {
        }

        /// <summary>
        /// Creates a copy of this line segment.
        /// </summary>
        /// <remarks>
        /// Line segment is a readonly struct, so plain assignment already produces an independent copy and
        /// this method is not needed to avoid sharing. It exists so that every geometry type offers the
        /// same way to ask for a copy.
        /// </remarks>
        /// <returns>A new line segment with the same endpoints.</returns>
        public GeoLine Clone() => new GeoLine(StartPoint, EndPoint);

        /// <summary>
        /// Gets the GeoVector pointing from start point to end point.
        /// </summary>
        public GeoVector Direction => StartPoint.GetVectorTo(EndPoint);

        /// <summary>
        /// Gets the length of the line segment.
        /// </summary>
        public double Length => StartPoint.DistanceTo(EndPoint);

        /// <summary>
        /// Gets the squared length of the line segment.
        /// </summary>
        public double LengthSquared => StartPoint.GetDistanceSquaredTo(EndPoint);

        /// <summary>
        /// Gets the midpoint of the line segment.
        /// </summary>
        public GeoPoint MidPoint => StartPoint.GetMiddlePoint(EndPoint);

        /// <summary>
        /// Gets the point on the line segment at parameter t (t=0 is the start point, t=1 is the end point).
        /// </summary>
        public GeoPoint GetPointAtParameter(double parameter) => Parametrization.GetPointAtParameter(this, parameter);

        /// <summary>
        /// Gets the normalized parameter of the point on this line segment closest to the supplied point.
        /// The point need not lie on the segment, so the result may fall outside [0, 1].
        /// </summary>
        public double GetParameterAtPoint(GeoPoint point) => Parametrization.GetParameterAtPoint(this, point);

        /// <summary>
        /// Gets the point at an arc length measured from the start point of this line segment.
        /// </summary>
        public GeoPoint GetPointAtDistance(double distance) => Parametrization.GetPointAtDistance(this, distance);

        /// <summary>
        /// Gets the arc length from the start point of this line segment to the point on it closest to the
        /// supplied point.
        /// </summary>
        public double GetDistanceAtPoint(GeoPoint point) => Parametrization.GetDistanceAtPoint(this, point);

        /// <summary>
        /// Gets the arc length from the start point of this line segment to a normalized parameter.
        /// </summary>
        public double GetDistanceAtParameter(double parameter) => Parametrization.GetDistanceAtParameter(this, parameter);

        /// <summary>
        /// Gets the normalized parameter at an arc length measured from the start point of this line segment.
        /// </summary>
        public double GetParameterAtDistance(double distance) => Parametrization.GetParameterAtDistance(this, distance);

        /// <summary>
        /// Gets the closest point on this line segment to a target point, clamped to the endpoints.
        /// </summary>
        public GeoPoint GetClosestPointOnBoundary(GeoPoint point) => Projection.ProjectToLine(this, point);

        /// <summary>
        /// Finds the shortest line segment connecting a point on this line segment to a point on another line segment using default tolerance.
        /// </summary>
        /// <param name="other">The other line segment.</param>
        /// <returns>A <see cref="GeoLine"/> connecting this line segment to the other line segment.</returns>
        /// <remarks>
        /// Both ends of the returned segment sit on a boundary, so a shape lying entirely inside another
        /// still reports the gap out to its outline rather than zero. <see cref="ArrangeAlgorithms.Core.Distance"/>
        /// takes the opposite view and treats a closed shape as a filled region, returning zero for that
        /// same pair.
        /// </remarks>
        public GeoLine GetClosestOnBoundary(GeoLine other) => Projection.GetClosestSegment(this, other, Tolerance.Global);

        /// <summary>
        /// Finds the shortest line segment connecting a point on this line segment to a point on another line segment within tolerance.
        /// </summary>
        /// <param name="other">The other line segment.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>A <see cref="GeoLine"/> connecting this line segment to the other line segment.</returns>
        /// <remarks>
        /// Both ends of the returned segment sit on a boundary, so a shape lying entirely inside another
        /// still reports the gap out to its outline rather than zero. <see cref="ArrangeAlgorithms.Core.Distance"/>
        /// takes the opposite view and treats a closed shape as a filled region, returning zero for that
        /// same pair.
        /// </remarks>
        public GeoLine GetClosestOnBoundary(GeoLine other, Tolerance tolerance) => Projection.GetClosestSegment(this, other, tolerance);

        /// <summary>
        /// Finds the shortest line segment connecting a point on this line segment to a point on the circumference of a circle using default tolerance.
        /// </summary>
        /// <param name="circle">The circle.</param>
        /// <returns>A <see cref="GeoLine"/> connecting this line segment to the circumference of the circle.</returns>
        /// <remarks>
        /// Both ends of the returned segment sit on a boundary, so a shape lying entirely inside another
        /// still reports the gap out to its outline rather than zero. <see cref="ArrangeAlgorithms.Core.Distance"/>
        /// takes the opposite view and treats a closed shape as a filled region, returning zero for that
        /// same pair.
        /// </remarks>
        public GeoLine GetClosestOnBoundary(GeoCircle circle) => Projection.GetClosestSegment(this, circle, Tolerance.Global);

        /// <summary>
        /// Finds the shortest line segment connecting a point on this line segment to a point on the circumference of a circle within tolerance.
        /// </summary>
        /// <param name="circle">The circle.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>A <see cref="GeoLine"/> connecting this line segment to the circumference of the circle.</returns>
        /// <remarks>
        /// Both ends of the returned segment sit on a boundary, so a shape lying entirely inside another
        /// still reports the gap out to its outline rather than zero. <see cref="ArrangeAlgorithms.Core.Distance"/>
        /// takes the opposite view and treats a closed shape as a filled region, returning zero for that
        /// same pair.
        /// </remarks>
        public GeoLine GetClosestOnBoundary(GeoCircle circle, Tolerance tolerance) => Projection.GetClosestSegment(this, circle, tolerance);

        /// <summary>
        /// Finds the shortest line segment connecting a point on this line segment to a point on the boundary of a rectangle using default tolerance.
        /// </summary>
        /// <param name="rect">The rectangle.</param>
        /// <returns>A <see cref="GeoLine"/> connecting this line segment to the boundary of the rectangle.</returns>
        /// <remarks>
        /// Both ends of the returned segment sit on a boundary, so a shape lying entirely inside another
        /// still reports the gap out to its outline rather than zero. <see cref="ArrangeAlgorithms.Core.Distance"/>
        /// takes the opposite view and treats a closed shape as a filled region, returning zero for that
        /// same pair.
        /// </remarks>
        public GeoLine GetClosestOnBoundary(GeoRectangle rect) => Projection.GetClosestSegment(this, rect, Tolerance.Global);

        /// <summary>
        /// Finds the shortest line segment connecting a point on this line segment to a point on the boundary of a rectangle within tolerance.
        /// </summary>
        /// <param name="rect">The rectangle.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>A <see cref="GeoLine"/> connecting this line segment to the boundary of the rectangle.</returns>
        /// <remarks>
        /// Both ends of the returned segment sit on a boundary, so a shape lying entirely inside another
        /// still reports the gap out to its outline rather than zero. <see cref="ArrangeAlgorithms.Core.Distance"/>
        /// takes the opposite view and treats a closed shape as a filled region, returning zero for that
        /// same pair.
        /// </remarks>
        public GeoLine GetClosestOnBoundary(GeoRectangle rect, Tolerance tolerance) => Projection.GetClosestSegment(this, rect, tolerance);

        /// <summary>
        /// Finds the shortest line segment connecting a point on this line segment to a point on a polyline using default tolerance.
        /// </summary>
        /// <param name="polyline">The polyline.</param>
        /// <returns>A <see cref="GeoLine"/> connecting this line segment to the polyline.</returns>
        /// <remarks>
        /// Both ends of the returned segment sit on a boundary, so a shape lying entirely inside another
        /// still reports the gap out to its outline rather than zero. <see cref="ArrangeAlgorithms.Core.Distance"/>
        /// takes the opposite view and treats a closed shape as a filled region, returning zero for that
        /// same pair.
        /// </remarks>
        public GeoLine GetClosestOnBoundary(GeoPolyline polyline) => Projection.GetClosestSegment(this, polyline, Tolerance.Global);

        /// <summary>
        /// Finds the shortest line segment connecting a point on this line segment to a point on a polyline within tolerance.
        /// </summary>
        /// <param name="polyline">The polyline.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>A <see cref="GeoLine"/> connecting this line segment to the polyline.</returns>
        /// <remarks>
        /// Both ends of the returned segment sit on a boundary, so a shape lying entirely inside another
        /// still reports the gap out to its outline rather than zero. <see cref="ArrangeAlgorithms.Core.Distance"/>
        /// takes the opposite view and treats a closed shape as a filled region, returning zero for that
        /// same pair.
        /// </remarks>
        public GeoLine GetClosestOnBoundary(GeoPolyline polyline, Tolerance tolerance) => Projection.GetClosestSegment(this, polyline, tolerance);

        /// <summary>
        /// Finds the shortest line segment connecting a point on this line segment to a point on the boundary of a polygon using default tolerance.
        /// </summary>
        /// <param name="poly">The polygon.</param>
        /// <returns>A <see cref="GeoLine"/> connecting this line segment to the boundary of the polygon.</returns>
        /// <remarks>
        /// Both ends of the returned segment sit on a boundary, so a shape lying entirely inside another
        /// still reports the gap out to its outline rather than zero. <see cref="ArrangeAlgorithms.Core.Distance"/>
        /// takes the opposite view and treats a closed shape as a filled region, returning zero for that
        /// same pair.
        /// </remarks>
        public GeoLine GetClosestOnBoundary(GeoPolygon poly) => Projection.GetClosestSegment(this, poly, Tolerance.Global);

        /// <summary>
        /// Finds the shortest line segment connecting a point on this line segment to a point on the boundary of a polygon within tolerance.
        /// </summary>
        /// <param name="poly">The polygon.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>A <see cref="GeoLine"/> connecting this line segment to the boundary of the polygon.</returns>
        /// <remarks>
        /// Both ends of the returned segment sit on a boundary, so a shape lying entirely inside another
        /// still reports the gap out to its outline rather than zero. <see cref="ArrangeAlgorithms.Core.Distance"/>
        /// takes the opposite view and treats a closed shape as a filled region, returning zero for that
        /// same pair.
        /// </remarks>
        public GeoLine GetClosestOnBoundary(GeoPolygon poly, Tolerance tolerance) => Projection.GetClosestSegment(this, poly, tolerance);

        /// <summary>
        /// Calculates the distance from a point to the closest point on this line segment.
        /// </summary>
        public double DistanceTo(GeoPoint point) => Distance.DistanceTo(this, point);

        /// <summary>
        /// Calculates the shortest distance from this line segment to another line segment using default tolerance.
        /// </summary>
        public double DistanceTo(GeoLine other) => Distance.DistanceTo(this, other, Tolerance.Global);

        /// <summary>
        /// Calculates the shortest distance from this line segment to another line segment within tolerance.
        /// </summary>
        public double DistanceTo(GeoLine other, Tolerance tolerance) => Distance.DistanceTo(this, other, tolerance);

        /// <summary>
        /// Calculates the shortest distance from this line segment to a rectangle.
        /// </summary>
        public double DistanceTo(GeoRectangle rect) => Distance.DistanceTo(rect, this);

        /// <summary>
        /// Calculates the shortest distance from this line segment to a polygon.
        /// </summary>
        public double DistanceTo(GeoPolygon poly) => Distance.DistanceTo(poly, this);

        /// <summary>
        /// Calculates the shortest distance from this line segment to a circle.
        /// </summary>
        public double DistanceTo(GeoCircle circle) => Distance.DistanceTo(circle, this);

        /// <summary>
        /// Calculates the shortest distance from this line segment to a polyline.
        /// </summary>
        public double DistanceTo(GeoPolyline polyline) => Distance.DistanceTo(polyline, this);

        /// <summary>
        /// Checks whether a point lies on the line segment using default tolerance.
        /// </summary>
        public bool IsPointOn(GeoPoint GeoPoint) => IsPointOn(GeoPoint, Tolerance.Global);

        /// <summary>
        /// Checks whether a point lies on the line segment within tolerance.
        /// </summary>
        public bool IsPointOn(GeoPoint GeoPoint, Tolerance tolerance) => Containment.IsPointOn(this, GeoPoint, tolerance);

        /// <summary>
        /// Classifies the location of a point relative to this line segment (OnSide or OutSide) using default tolerance.
        /// </summary>
        public PointLocation Locate(GeoPoint point) => Containment.Locate(this, point, Tolerance.Global);

        /// <summary>
        /// Classifies the location of a point relative to this line segment (OnSide or OutSide) within tolerance.
        /// </summary>
        public PointLocation Locate(GeoPoint point, Tolerance tolerance) => Containment.Locate(this, point, tolerance);

        /// <summary>
        /// Checks whether this line segment collides / intersects with another line segment using default tolerance.
        /// </summary>
        public bool CollidesWith(GeoLine other) => Collision.CollidesWith(this, other, Tolerance.Global);

        /// <summary>
        /// Checks whether this line segment collides / intersects with another line segment within tolerance.
        /// </summary>
        public bool CollidesWith(GeoLine other, Tolerance tolerance) => Collision.CollidesWith(this, other, tolerance);

        /// <summary>
        /// Checks whether this line segment collides / intersects with a rectangle using default tolerance.
        /// </summary>
        public bool CollidesWith(GeoRectangle rect) => Collision.CollidesWith(rect, this, Tolerance.Global);

        /// <summary>
        /// Checks whether this line segment collides / intersects with a rectangle within tolerance.
        /// </summary>
        public bool CollidesWith(GeoRectangle rect, Tolerance tolerance) => Collision.CollidesWith(rect, this, tolerance);

        /// <summary>
        /// Checks whether this line segment collides / intersects with a polygon using default tolerance.
        /// </summary>
        public bool CollidesWith(GeoPolygon poly) => Collision.CollidesWith(poly, this, Tolerance.Global);

        /// <summary>
        /// Checks whether this line segment collides / intersects with a polygon within tolerance.
        /// </summary>
        public bool CollidesWith(GeoPolygon poly, Tolerance tolerance) => Collision.CollidesWith(poly, this, tolerance);

        /// <summary>
        /// Checks whether this line segment collides / intersects with a circle using default tolerance.
        /// </summary>
        public bool CollidesWith(GeoCircle circle) => Collision.CollidesWith(circle, this, Tolerance.Global);

        /// <summary>
        /// Checks whether this line segment collides / intersects with a circle within tolerance.
        /// </summary>
        public bool CollidesWith(GeoCircle circle, Tolerance tolerance) => Collision.CollidesWith(circle, this, tolerance);

        /// <summary>
        /// Checks whether this line segment collides / intersects with a polyline using default tolerance.
        /// </summary>
        public bool CollidesWith(GeoPolyline polyline) => Collision.CollidesWith(polyline, this, Tolerance.Global);

        /// <summary>
        /// Checks whether this line segment collides / intersects with a polyline within tolerance.
        /// </summary>
        public bool CollidesWith(GeoPolyline polyline, Tolerance tolerance) => Collision.CollidesWith(polyline, this, tolerance);

        /// <summary>
        /// Tries to calculate the intersection with another line segment using default tolerance.
        /// </summary>
        public bool TryIntersectWith(GeoLine other, out GeoPoint intersection) => TryIntersectWith(other, out intersection, Tolerance.Global);

        /// <summary>
        /// Tries to calculate the intersection with another line segment within tolerance.
        /// </summary>
        public bool TryIntersectWith(GeoLine other, out GeoPoint intersection, Tolerance tolerance) => Intersection.TryIntersectWith(this, other, out intersection, tolerance);

        /// <summary>
        /// Gets the intersection point with another line segment using default tolerance.
        /// Returns null if lines do not intersect.
        /// </summary>
        public GeoPoint? GetIntersection(GeoLine other) => Intersection.GetIntersection(this, other, Tolerance.Global);

        /// <summary>
        /// Gets the intersection point with another line segment within tolerance.
        /// Returns null if lines do not intersect.
        /// </summary>
        public GeoPoint? GetIntersection(GeoLine other, Tolerance tolerance) => Intersection.GetIntersection(this, other, tolerance);

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
        /// Gets all intersection points with a circle using default tolerance.
        /// </summary>
        public GeoPoint[] GetIntersections(GeoCircle circle) => Intersection.GetIntersections(circle, this, Tolerance.Global);

        /// <summary>
        /// Gets all intersection points with a circle within tolerance.
        /// </summary>
        public GeoPoint[] GetIntersections(GeoCircle circle, Tolerance tolerance) => Intersection.GetIntersections(circle, this, tolerance);

        /// <summary>
        /// Gets all intersection points with a polyline using default tolerance.
        /// </summary>
        public GeoPoint[] GetIntersections(GeoPolyline polyline) => Intersection.GetIntersections(polyline, this, Tolerance.Global);

        /// <summary>
        /// Gets all intersection points with a polyline within tolerance.
        /// </summary>
        public GeoPoint[] GetIntersections(GeoPolyline polyline, Tolerance tolerance) => Intersection.GetIntersections(polyline, this, tolerance);

        /// <summary>
        /// Splits this segment at a point lying on it, using the default tolerance.
        /// </summary>
        /// <param name="point">The point to split at, which must lie on this segment.</param>
        /// <param name="first">The piece holding the start point.</param>
        /// <param name="second">The piece holding the end point.</param>
        /// <returns>true if the segment was split; otherwise, false.</returns>
        public bool TrySplitBy(GeoPoint point, out GeoLine first, out GeoLine second)
            => Splition.TrySplitBy(this, point, out first, out second, Tolerance.Global);

        /// <summary>
        /// Splits this segment at a point lying on it, within tolerance.
        /// </summary>
        /// <param name="point">The point to split at, which must lie on this segment.</param>
        /// <param name="first">The piece holding the start point.</param>
        /// <param name="second">The piece holding the end point.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>true if the segment was split; otherwise, false.</returns>
        public bool TrySplitBy(GeoPoint point, out GeoLine first, out GeoLine second, Tolerance tolerance)
            => Splition.TrySplitBy(this, point, out first, out second, tolerance);

        /// <summary>
        /// Splits this segment where a cutting line segment crosses it, using the default tolerance.
        /// </summary>
        /// <param name="cutter">The cutting line segment.</param>
        /// <param name="first">The piece holding the start point.</param>
        /// <param name="second">The piece holding the end point.</param>
        /// <returns>true if the segment was split; otherwise, false.</returns>
        public bool TrySplitBy(GeoLine cutter, out GeoLine first, out GeoLine second)
            => Splition.TrySplitBy(this, cutter, out first, out second, Tolerance.Global);

        /// <summary>
        /// Splits this segment where a cutting line segment crosses it, within tolerance.
        /// </summary>
        /// <param name="cutter">The cutting line segment.</param>
        /// <param name="first">The piece holding the start point.</param>
        /// <param name="second">The piece holding the end point.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>true if the segment was split; otherwise, false.</returns>
        public bool TrySplitBy(GeoLine cutter, out GeoLine first, out GeoLine second, Tolerance tolerance)
            => Splition.TrySplitBy(this, cutter, out first, out second, tolerance);

        /// <summary>
        /// Splits this segment against a polygon, sorting the parts by which side of its boundary they
        /// fall on, using the default tolerance.
        /// </summary>
        /// <param name="cutter">The polygon to split against.</param>
        /// <param name="inside">The parts lying inside the polygon, in order along this segment.</param>
        /// <param name="outside">The parts lying outside the polygon, in order along this segment.</param>
        /// <returns>true if the polygon boundary crosses this segment; otherwise, false.</returns>
        public bool TrySplitBy(GeoPolygon cutter, out GeoLine[] inside, out GeoLine[] outside)
            => Splition.TrySplitBy(this, cutter, out inside, out outside, Tolerance.Global);

        /// <summary>
        /// Splits this segment against a polygon, sorting the parts by which side of its boundary they
        /// fall on, within tolerance.
        /// </summary>
        /// <param name="cutter">The polygon to split against.</param>
        /// <param name="inside">The parts lying inside the polygon, in order along this segment.</param>
        /// <param name="outside">The parts lying outside the polygon, in order along this segment.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>true if the polygon boundary crosses this segment; otherwise, false.</returns>
        public bool TrySplitBy(GeoPolygon cutter, out GeoLine[] inside, out GeoLine[] outside, Tolerance tolerance)
            => Splition.TrySplitBy(this, cutter, out inside, out outside, tolerance);

        /// <summary>
        /// Splits this segment everywhere a list of cutting line segments crosses it, using the default tolerance.
        /// </summary>
        /// <param name="cutters">The cutting line segments.</param>
        /// <param name="pieces">The resulting pieces in order along this segment.</param>
        /// <returns>true if this segment was split by at least one cutter; otherwise, false.</returns>
        public bool TrySplitBy(GeoLine[] cutters, out GeoLine[] pieces)
            => Splition.TrySplitBy(this, cutters, out pieces, Tolerance.Global);

        /// <summary>
        /// Splits this segment everywhere a list of cutting line segments crosses it, within tolerance.
        /// </summary>
        /// <param name="cutters">The cutting line segments.</param>
        /// <param name="pieces">The resulting pieces in order along this segment.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>true if this segment was split by at least one cutter; otherwise, false.</returns>
        public bool TrySplitBy(GeoLine[] cutters, out GeoLine[] pieces, Tolerance tolerance)
            => Splition.TrySplitBy(this, cutters, out pieces, tolerance);

        /// <summary>
        /// Splits this segment everywhere a cutting polyline crosses it, using the default tolerance.
        /// </summary>
        /// <param name="cutter">The cutting polyline.</param>
        /// <param name="pieces">The resulting pieces in order along this segment.</param>
        /// <returns>true if this segment was split by the polyline; otherwise, false.</returns>
        public bool TrySplitBy(GeoPolyline cutter, out GeoLine[] pieces)
            => Splition.TrySplitBy(this, cutter, out pieces, Tolerance.Global);

        /// <summary>
        /// Splits this segment everywhere a cutting polyline crosses it, within tolerance.
        /// </summary>
        /// <param name="cutter">The cutting polyline.</param>
        /// <param name="pieces">The resulting pieces in order along this segment.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>true if this segment was split by the polyline; otherwise, false.</returns>
        public bool TrySplitBy(GeoPolyline cutter, out GeoLine[] pieces, Tolerance tolerance)
            => Splition.TrySplitBy(this, cutter, out pieces, tolerance);

        /// <summary>
        /// Splits this segment everywhere a list of points lies on it, using the default tolerance.
        /// </summary>
        /// <param name="points">The points to split at.</param>
        /// <param name="pieces">The resulting pieces in order along this segment.</param>
        /// <returns>true if this segment was split; otherwise, false.</returns>
        public bool TrySplitBy(GeoPoint[] points, out GeoLine[] pieces)
            => Splition.TrySplitBy(this, points, out pieces, Tolerance.Global);

        /// <summary>
        /// Splits this segment everywhere a list of points lies on it, within tolerance.
        /// </summary>
        /// <param name="points">The points to split at.</param>
        /// <param name="pieces">The resulting pieces in order along this segment.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>true if this segment was split; otherwise, false.</returns>
        public bool TrySplitBy(GeoPoint[] points, out GeoLine[] pieces, Tolerance tolerance)
            => Splition.TrySplitBy(this, points, out pieces, tolerance);

        /// <summary>
        /// Splits this segment everywhere a list of polygon boundaries crosses it, separating the parts that fall
        /// inside the polygons from those that fall outside, using the default tolerance.
        /// </summary>
        /// <param name="cutters">The polygons to split against.</param>
        /// <param name="inside">The parts lying inside the polygons, in order along this segment.</param>
        /// <param name="outside">The parts lying outside the polygons, in order along this segment.</param>
        /// <returns>true if at least one polygon boundary crosses this segment; otherwise, false.</returns>
        public bool TrySplitBy(GeoPolygon[] cutters, out GeoLine[] inside, out GeoLine[] outside)
            => Splition.TrySplitBy(this, cutters, out inside, out outside, Tolerance.Global);

        /// <summary>
        /// Splits this segment everywhere a list of polygon boundaries crosses it, separating the parts that fall
        /// inside the polygons from those that fall outside, within tolerance.
        /// </summary>
        /// <param name="cutters">The polygons to split against.</param>
        /// <param name="inside">The parts lying inside the polygons, in order along this segment.</param>
        /// <param name="outside">The parts lying outside the polygons, in order along this segment.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>true if at least one polygon boundary crosses this segment; otherwise, false.</returns>
        public bool TrySplitBy(GeoPolygon[] cutters, out GeoLine[] inside, out GeoLine[] outside, Tolerance tolerance)
            => Splition.TrySplitBy(this, cutters, out inside, out outside, tolerance);

        /// <summary>
        /// Splits this segment everywhere a list of polylines crosses it, using the default tolerance.
        /// </summary>
        /// <param name="cutters">The cutting polylines.</param>
        /// <param name="pieces">The resulting pieces in order along this segment.</param>
        /// <returns>true if at least one polyline crosses this segment; otherwise, false.</returns>
        public bool TrySplitBy(GeoPolyline[] cutters, out GeoLine[] pieces)
            => Splition.TrySplitBy(this, cutters, out pieces, Tolerance.Global);

        /// <summary>
        /// Splits this segment everywhere a list of polylines crosses it, within tolerance.
        /// </summary>
        /// <param name="cutters">The cutting polylines.</param>
        /// <param name="pieces">The resulting pieces in order along this segment.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>true if at least one polyline crosses this segment; otherwise, false.</returns>
        public bool TrySplitBy(GeoPolyline[] cutters, out GeoLine[] pieces, Tolerance tolerance)
            => Splition.TrySplitBy(this, cutters, out pieces, tolerance);

        /// <summary>
        /// Splits this segment at an arc length measured from its start point, using the default tolerance.
        /// </summary>
        /// <param name="distance">Arc length from the start point.</param>
        /// <param name="first">The piece holding the start point.</param>
        /// <param name="second">The piece holding the end point.</param>
        /// <returns>true if the segment was split; otherwise, false.</returns>
        public bool TrySplitAtDistance(double distance, out GeoLine first, out GeoLine second)
            => Splition.TrySplitAtDistance(this, distance, out first, out second, Tolerance.Global);

        /// <summary>
        /// Splits this segment at an arc length measured from its start point, within tolerance.
        /// </summary>
        /// <param name="distance">Arc length from the start point.</param>
        /// <param name="first">The piece holding the start point.</param>
        /// <param name="second">The piece holding the end point.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>true if the segment was split; otherwise, false.</returns>
        public bool TrySplitAtDistance(double distance, out GeoLine first, out GeoLine second, Tolerance tolerance)
            => Splition.TrySplitAtDistance(this, distance, out first, out second, tolerance);

        /// <summary>
        /// Splits this segment at several arc lengths measured from its start point, using the default
        /// tolerance.
        /// </summary>
        /// <param name="distances">Arc lengths from the start point, in any order.</param>
        /// <returns>The pieces in order along this segment.</returns>
        public GeoLine[] SplitAtDistances(IEnumerable<double> distances)
            => Splition.SplitAtDistances(this, distances, Tolerance.Global);

        /// <summary>
        /// Splits this segment at several arc lengths measured from its start point, within tolerance.
        /// </summary>
        /// <param name="distances">Arc lengths from the start point, in any order.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>The pieces in order along this segment.</returns>
        public GeoLine[] SplitAtDistances(IEnumerable<double> distances, Tolerance tolerance)
            => Splition.SplitAtDistances(this, distances, tolerance);

        /// <summary>
        /// Checks whether this line segment is parallel to another line segment using default tolerance.
        /// </summary>
        public bool IsParallelTo(GeoLine other) => Parallel.IsParallel(this, other, Tolerance.Global);

        /// <summary>
        /// Checks whether this line segment is parallel to another line segment within angular tolerance.
        /// </summary>
        public bool IsParallelTo(GeoLine other, Tolerance tolerance) => Parallel.IsParallel(this, other, tolerance);

        /// <summary>
        /// Checks whether this line segment is parallel to a vector using default tolerance.
        /// </summary>
        public bool IsParallelTo(GeoVector vector) => Parallel.IsParallel(this, vector, Tolerance.Global);

        /// <summary>
        /// Checks whether this line segment is parallel to a vector within angular tolerance.
        /// </summary>
        public bool IsParallelTo(GeoVector vector, Tolerance tolerance) => Parallel.IsParallel(this, vector, tolerance);

        /// <summary>
        /// Checks whether this line segment is parallel to any edge of a rotated rectangle using default tolerance.
        /// </summary>
        public bool IsParallelTo(GeoRectangle rect) => Parallel.IsParallel(rect, this, Tolerance.Global);

        /// <summary>
        /// Checks whether this line segment is parallel to any edge of a rotated rectangle within angular tolerance.
        /// </summary>
        public bool IsParallelTo(GeoRectangle rect, Tolerance tolerance) => Parallel.IsParallel(rect, this, tolerance);

        /// <summary>
        /// Checks whether this line segment is perpendicular to another line segment using default tolerance.
        /// </summary>
        public bool IsPerpendicularTo(GeoLine other) => Parallel.IsPerpendicular(this, other, Tolerance.Global);

        /// <summary>
        /// Checks whether this line segment is perpendicular to another line segment within angular tolerance.
        /// </summary>
        public bool IsPerpendicularTo(GeoLine other, Tolerance tolerance) => Parallel.IsPerpendicular(this, other, tolerance);

        /// <summary>
        /// Checks whether this line segment is perpendicular to a vector using default tolerance.
        /// </summary>
        public bool IsPerpendicularTo(GeoVector vector) => Parallel.IsPerpendicular(this, vector, Tolerance.Global);

        /// <summary>
        /// Checks whether this line segment is perpendicular to a vector within angular tolerance.
        /// </summary>
        public bool IsPerpendicularTo(GeoVector vector, Tolerance tolerance) => Parallel.IsPerpendicular(this, vector, tolerance);

        /// <summary>
        /// Indicates whether the current line segment is equal to another line segment.
        /// </summary>
        /// <param name="other">A line segment to compare with this line segment.</param>
        /// <returns>true if the current line segment is equal to the other parameter; otherwise, false.</returns>
        public bool Equals(GeoLine other) => StartPoint.Equals(other.StartPoint) && EndPoint.Equals(other.EndPoint);

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>true if obj and this instance are the same type and represent the same value; otherwise, false.</returns>
        public override bool Equals(object obj) => obj is GeoLine other && Equals(other);

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (StartPoint.GetHashCode() * 397) ^ EndPoint.GetHashCode();
            }
        }

        /// <summary>
        /// Compares two GeoLine instances for equality.
        /// </summary>
        /// <param name="left">The first line segment.</param>
        /// <param name="right">The second line segment.</param>
        /// <returns>true if they are equal; otherwise, false.</returns>
        public static bool operator ==(GeoLine left, GeoLine right) => left.Equals(right);

        /// <summary>
        /// Compares two GeoLine instances for inequality.
        /// </summary>
        /// <param name="left">The first line segment.</param>
        /// <param name="right">The second line segment.</param>
        /// <returns>true if they are not equal; otherwise, false.</returns>
        public static bool operator !=(GeoLine left, GeoLine right) => !left.Equals(right);

        /// <summary>
        /// Returns the string representation of the line segment.
        /// </summary>
        /// <returns>A string representation of the start and end points of the line.</returns>
        public override string ToString() => $"GeoLine[{StartPoint} -> {EndPoint}]";
    }
}
