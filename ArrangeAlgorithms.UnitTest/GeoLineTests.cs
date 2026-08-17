using ArrangeAlgorithms.Geometry;
using System;
using Xunit;

namespace ArrangeAlgorithms.UnitTest
{
    public class LineTests
    {
        [Fact]
        public void Line_CreationAndProperties_WorkCorrectly()
        {
            var p1 = new GeoPoint(1.0, 1.0);
            var p2 = new GeoPoint(4.0, 5.0);
            var GeoLine = new GeoLine(p1, p2);

            Assert.Equal(p1, GeoLine.StartPoint);
            Assert.Equal(p2, GeoLine.EndPoint);
            Assert.Equal(5.0, GeoLine.Length, 12);
            Assert.Equal(25.0, GeoLine.LengthSquared, 12);
            Assert.Equal(new GeoPoint(2.5, 3.0), GeoLine.MidPoint);
            Assert.Equal(new GeoVector(3.0, 4.0), GeoLine.Direction);
        }

        [Fact]
        public void Line_ProjectionAndClosestPoint_WorkCorrectly()
        {
            var GeoLine = new GeoLine(0.0, 0.0, 10.0, 0.0); // Horizontal on the X-axis

            // Point on the line segment
            Assert.Equal(0.5, GeoLine.GetParameterOf(new GeoPoint(5.0, 0.0)), 12);
            Assert.Equal(new GeoPoint(5.0, 0.0), GeoLine.GetClosestPointTo(new GeoPoint(5.0, 5.0)));
            Assert.Equal(5.0, GeoLine.DistanceTo(new GeoPoint(5.0, 5.0)), 12);

            // Point outside the endpoints
            Assert.Equal(-0.2, GeoLine.GetParameterOf(new GeoPoint(-2.0, 0.0)), 12);
            Assert.Equal(GeoLine.StartPoint, GeoLine.GetClosestPointTo(new GeoPoint(-2.0, 5.0)));

            Assert.True(GeoLine.IsPointOn(new GeoPoint(3.0, 0.0)));
            Assert.False(GeoLine.IsPointOn(new GeoPoint(3.0, 0.1)));
        }

        [Fact]
        public void Line_Intersection_WorksCorrectly()
        {
            var l1 = new GeoLine(0.0, 0.0, 10.0, 10.0);
            var l2 = new GeoLine(0.0, 10.0, 10.0, 0.0);

            // Intersect at the center (5, 5)
            Assert.True(l1.TryIntersectWith(l2, out var hit));
            Assert.True(hit.IsEqualTo(new GeoPoint(5.0, 5.0)));

            // Song song
            var l3 = new GeoLine(0.0, 2.0, 10.0, 12.0);
            Assert.False(l1.TryIntersectWith(l3, out _));

            // Do not intersect directly (only intersect on extended line)
            var l4 = new GeoLine(20.0, 10.0, 30.0, 0.0);
            Assert.False(l1.TryIntersectWith(l4, out _));
        }

        [Fact]
        public void Line_Direction_ReturnsZeroForDegenerateLine()
        {
            var GeoLine = new GeoLine(1.0, 1.0, 1.0, 1.0); // Start point equals end point
            Assert.Equal(GeoVector.Zero, GeoLine.Direction);
            Assert.Equal(0.0, GeoLine.Length);
            Assert.False(GeoLine.Direction.TryGetNormal(out _));
        }

        [Fact]
        public void Line_CollinearAndOverlappingIntersection_WorkCorrectly()
        {
            // Two line segments lying on the same line
            var l1 = new GeoLine(0.0, 0.0, 5.0, 0.0);

            // Case 1: Lying on the extended line but not touching
            var l2 = new GeoLine(10.0, 0.0, 15.0, 0.0);
            Assert.False(l1.TryIntersectWith(l2, out _));

            // Case 2: Collinear Overlap
            var l3 = new GeoLine(3.0, 0.0, 8.0, 0.0);
            // Basic geometric algorithms in AutoCAD/libraries typically return false for collinear overlap
            // because there are infinite intersection points; we test the library's actual behavior:
            Assert.False(l1.TryIntersectWith(l3, out _));
        }

        [Fact]
        public void Line_PointProjectionEdgeCases_WorkCorrectly()
        {
            var GeoLine = new GeoLine(0.0, 0.0, 10.0, 0.0);

            // Projection falls exactly on the start point (parameter t = 0)
            Assert.Equal(0.0, GeoLine.GetParameterOf(new GeoPoint(0.0, 5.0)), 12);
            Assert.Equal(GeoLine.StartPoint, GeoLine.GetClosestPointTo(new GeoPoint(0.0, 5.0)));

            // Projection falls exactly on the end point (parameter t = 1)
            Assert.Equal(1.0, GeoLine.GetParameterOf(new GeoPoint(10.0, -5.0)), 12);
            Assert.Equal(GeoLine.EndPoint, GeoLine.GetClosestPointTo(new GeoPoint(10.0, -5.0)));
        }

        [Fact]
        public void Line_TJunctionIntersection_WorksCorrectly()
        {
            var l1 = new GeoLine(0.0, 0.0, 10.0, 0.0);

            // A perpendicular segment touching exactly at the endpoint
            var l2 = new GeoLine(5.0, 0.0, 5.0, 5.0);
            Assert.True(l1.TryIntersectWith(l2, out var hit));
            Assert.True(hit.IsEqualTo(new GeoPoint(5.0, 0.0)));
        }

        [Fact]
        public void Line_GetPointAtParameter_InterpolatesAndExtrapolates()
        {
            var line = new GeoLine(0.0, 0.0, 10.0, 20.0);

            Assert.True(line.GetPointAtParameter(0.0).IsEqualTo(line.StartPoint));
            Assert.True(line.GetPointAtParameter(1.0).IsEqualTo(line.EndPoint));
            Assert.True(line.GetPointAtParameter(0.5).IsEqualTo(line.MidPoint));

            // Parameter outside [0, 1] for points on the extended line.
            Assert.True(line.GetPointAtParameter(2.0).IsEqualTo(new GeoPoint(20.0, 40.0)));
            Assert.True(line.GetPointAtParameter(-0.5).IsEqualTo(new GeoPoint(-5.0, -10.0)));
        }

        [Fact]
        public void Line_GetParameterOf_ReturnsZeroForDegenerateLine()
        {
            // A degenerate segment has no direction so parameter is undefined; must return 0 instead of dividing by 0.
            var degenerate = new GeoLine(3.0, 3.0, 3.0, 3.0);

            Assert.Equal(0.0, degenerate.GetParameterOf(new GeoPoint(10.0, 10.0)));
            Assert.Equal(degenerate.StartPoint, degenerate.GetClosestPointTo(new GeoPoint(10.0, 10.0)));
        }

        [Fact]
        public void Line_Equality_AndHashCode_WorkCorrectly()
        {
            var a = new GeoLine(1.0, 2.0, 3.0, 4.0);
            var b = new GeoLine(1.0, 2.0, 3.0, 4.0);
            var reversed = new GeoLine(3.0, 4.0, 1.0, 2.0);

            Assert.True(a == b);
            Assert.False(a != b);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
            Assert.True(a.Equals((object)b));

            object notALine = "not a GeoLine";
            Assert.False(a.Equals(notALine));

            // Reversing direction creates a DIFFERENT line segment: direction is part of its identity.
            Assert.True(a != reversed);
        }

        [Fact]
        public void Line_IntersectsWith_MatchesTryIntersectWith()
        {
            var line = new GeoLine(0.0, 0.0, 10.0, 10.0);
            var crossing = new GeoLine(0.0, 10.0, 10.0, 0.0);
            var parallel = new GeoLine(0.0, 2.0, 10.0, 12.0);

            Assert.Equal(line.TryIntersectWith(crossing, out _), line.IntersectsWith(crossing));
            Assert.Equal(line.TryIntersectWith(parallel, out _), line.IntersectsWith(parallel));

            Assert.True(line.IntersectsWith(crossing));
            Assert.False(line.IntersectsWith(parallel));
        }

        [Fact]
        public void Line_IntersectsWith_NullPolygon_Throws()
        {
            var line = new GeoLine(0.0, 0.0, 10.0, 10.0);

            Assert.Throws<ArgumentNullException>(() => line.IntersectsWith((GeoPolygon)null));
        }

        [Fact]
        public void Line_DistanceTo_MeasuresFromNearestEndWhenProjectionFallsOutside()
        {
            var line = new GeoLine(0.0, 0.0, 10.0, 0.0);

            // Projection falls inside the segment: perpendicular distance.
            Assert.Equal(3.0, line.DistanceTo(new GeoPoint(4.0, 3.0)), 9);

            // Projection falls outside the endpoints: measure directly to that endpoint.
            Assert.Equal(5.0, line.DistanceTo(new GeoPoint(-3.0, 4.0)), 9);
            Assert.Equal(5.0, line.DistanceTo(new GeoPoint(13.0, 4.0)), 9);
        }

        [Fact]
        public void Line_IntersectsWithLine_WorksCorrectly()
        {
            var l1 = new GeoLine(0.0, 0.0, 10.0, 0.0);

            Assert.True(l1.IntersectsWith(new GeoLine(5.0, -5.0, 5.0, 5.0)));   // Intersects
            Assert.False(l1.IntersectsWith(new GeoLine(0.0, 5.0, 10.0, 5.0)));  // Parallel
            Assert.False(l1.IntersectsWith(new GeoLine(20.0, -5.0, 20.0, 5.0))); // Intersects extension, not segment
        }

        [Fact]
        public void Line_IntersectsWithRectangleAndPolygon_WorksCorrectly()
        {
            var rect = new GeoRectangle(new GeoPoint(0.0, 0.0), 4.0, 4.0);
            var poly = new GeoPolygon(
                new GeoPoint(-2.0, -2.0),
                new GeoPoint(2.0, -2.0),
                new GeoPoint(2.0, 2.0),
                new GeoPoint(-2.0, 2.0)
            );

            var crossing = new GeoLine(-5.0, 0.0, 5.0, 0.0);
            var outside = new GeoLine(10.0, 10.0, 12.0, 12.0);

            Assert.True(crossing.IntersectsWith(rect));
            Assert.False(outside.IntersectsWith(rect));

            Assert.True(crossing.IntersectsWith(poly));
            Assert.False(outside.IntersectsWith(poly));

            // Calling in reverse direction must give the same result
            Assert.True(rect.IntersectsWith(crossing));
            Assert.True(poly.IntersectsWith(crossing));
        }
    }
}

