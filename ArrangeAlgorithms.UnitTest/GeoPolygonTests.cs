using ArrangeAlgorithms.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ArrangeAlgorithms.UnitTest
{
    public class PolygonTests
    {
        [Fact]
        public void Polygon_WindingAndArea_WorkCorrectly()
        {
            // Counter-clockwise triangle (CCW)
            var p1 = new GeoPoint(0.0, 0.0);
            var p2 = new GeoPoint(4.0, 0.0);
            var p3 = new GeoPoint(0.0, 3.0);
            var poly = new GeoPolygon(p1, p2, p3);

            Assert.Equal(3, poly.VertexCount);
            Assert.Equal(3, poly.EdgeCount);
            Assert.Equal(6.0, poly.GetArea(), 12);
            Assert.Equal(6.0, poly.GetSignedArea(), 12);
            Assert.False(poly.IsClockwise());

            // Clockwise triangle (CW)
            var polyCW = new GeoPolygon(p1, p3, p2);
            Assert.Equal(6.0, polyCW.GetArea(), 12);
            Assert.Equal(-6.0, polyCW.GetSignedArea(), 12);
            Assert.True(polyCW.IsClockwise());
        }

        [Fact]
        public void Polygon_Centroid_WorksCorrectly()
        {
            var p1 = new GeoPoint(0.0, 0.0);
            var p2 = new GeoPoint(6.0, 0.0);
            var p3 = new GeoPoint(0.0, 6.0);
            var poly = new GeoPolygon(p1, p2, p3);

            // The centroid of this right triangle is at (2, 2)
            Assert.True(poly.GetCentroid().IsEqualTo(new GeoPoint(2.0, 2.0)));
        }

        [Fact]
        public void Polygon_ContainsPoint_WorksCorrectly()
        {
            var poly = new GeoPolygon(
                new GeoPoint(0.0, 0.0),
                new GeoPoint(4.0, 0.0),
                new GeoPoint(4.0, 4.0),
                new GeoPoint(0.0, 4.0)
            ); // 4x4 square

            Assert.True(poly.Contains(new GeoPoint(2.0, 2.0)));   // Point inside
            Assert.True(poly.Contains(new GeoPoint(0.0, 2.0)));   // Point on boundary
            Assert.False(poly.Contains(new GeoPoint(5.0, 2.0)));  // Point outside
        }

        [Fact]
        public void Polygon_IntersectsWithRectangle_WorksCorrectly()
        {
            var poly = new GeoPolygon(
                new GeoPoint(0.0, 0.0),
                new GeoPoint(4.0, 0.0),
                new GeoPoint(4.0, 4.0),
                new GeoPoint(0.0, 4.0)
            ); // 4x4 square

            // 1. Mutually intersecting
            var rect1 = new GeoRectangle(new GeoPoint(4.0, 4.0), 2.0, 2.0, Math.PI / 4.0); // Center at square vertex, rotated 45 degrees
            Assert.True(poly.IntersectsWith(rect1));

            // 2. Containing the rectangle entirely
            var rect2 = new GeoRectangle(new GeoPoint(2.0, 2.0), 1.0, 1.0, 0.1);
            Assert.True(poly.IntersectsWith(rect2));

            // 3. Completely separated
            var rect3 = new GeoRectangle(new GeoPoint(8.0, 8.0), 2.0, 2.0, 0.5);
            Assert.False(poly.IntersectsWith(rect3));
        }

        [Fact]
        public void Polygon_IntersectsWithLine_WorksCorrectly()
        {
            var poly = new GeoPolygon(
                new GeoPoint(0.0, 0.0),
                new GeoPoint(4.0, 0.0),
                new GeoPoint(4.0, 4.0),
                new GeoPoint(0.0, 4.0)
            ); // 4x4 square

            // 1. Line segment passing through the square
            Assert.True(poly.IntersectsWith(new GeoLine(-2.0, 2.0, 6.0, 2.0)));

            // 2. Line segment completely inside (does not intersect any edges)
            Assert.True(poly.IntersectsWith(new GeoLine(1.0, 1.0, 3.0, 3.0)));

            // 3. Line segment touching boundary
            Assert.True(poly.IntersectsWith(new GeoLine(-2.0, 4.0, 2.0, 4.0)));

            // 4. Completely separated
            Assert.False(poly.IntersectsWith(new GeoLine(6.0, 6.0, 8.0, 8.0)));
        }

        [Fact]
        public void Polygon_IntersectsWithPolygon_WorksCorrectly()
        {
            var poly = new GeoPolygon(
                new GeoPoint(0.0, 0.0),
                new GeoPoint(4.0, 0.0),
                new GeoPoint(4.0, 4.0),
                new GeoPoint(0.0, 4.0)
            ); // 4x4 square

            // 1. Partially overlapping
            var overlapping = new GeoPolygon(
                new GeoPoint(2.0, 2.0),
                new GeoPoint(6.0, 2.0),
                new GeoPoint(6.0, 6.0),
                new GeoPoint(2.0, 6.0)
            );
            Assert.True(poly.IntersectsWith(overlapping));

            // 2. Nested entirely inside: no edges intersect, must be recognized via containment check
            var inner = new GeoPolygon(
                new GeoPoint(1.0, 1.0),
                new GeoPoint(3.0, 1.0),
                new GeoPoint(3.0, 3.0),
                new GeoPoint(1.0, 3.0)
            );
            Assert.True(poly.IntersectsWith(inner));

            // 3. Containment relation must be symmetric in both calling directions
            Assert.True(inner.IntersectsWith(poly));

            // 4. Completely separated
            var far = new GeoPolygon(
                new GeoPoint(10.0, 10.0),
                new GeoPoint(12.0, 10.0),
                new GeoPoint(12.0, 12.0)
            );
            Assert.False(poly.IntersectsWith(far));
        }

        [Fact]
        public void Polygon_Constructor_RejectsDegeneratePolygon()
        {
            // A polygon with only 2 vertices is degenerate. The constructor blocks it immediately instead of creating
            // an object that cannot answer Contains meaningfully, so the correct test is to assert an exception.
            Assert.Throws<ArgumentException>(
                () => new GeoPolygon(new GeoPoint(0.0, 0.0), new GeoPoint(1.0, 1.0)));
        }

        [Fact]
        public void Polygon_CentroidConcavePolygon_WorksCorrectly()
        {
            // L-shaped polygon (concave)
            var poly = new GeoPolygon(
                new GeoPoint(0.0, 0.0),
                new GeoPoint(4.0, 0.0),
                new GeoPoint(4.0, 2.0),
                new GeoPoint(2.0, 2.0),
                new GeoPoint(2.0, 4.0),
                new GeoPoint(0.0, 4.0)
            );

            // Centroid and area of arbitrary polygon using Shoelace / Green Theorem algorithm
            // Area = 12
            Assert.Equal(12.0, poly.GetArea(), 12);

            // Composed of two rectangles: [0,4]x[0,2] area 8 center (2,1) and [0,2]x[2,4] area 4 center (1,3).
            // Centroid = (8*2 + 4*1)/12 = 5/3 along X, (8*1 + 4*3)/12 = 5/3 along Y.
            Assert.True(poly.GetCentroid().IsEqualTo(new GeoPoint(5.0 / 3.0, 5.0 / 3.0)));
        }

        [Fact]
        public void Polygon_ContainsPointConcavePolygon_WorksCorrectly()
        {
            // L-shaped polygon (concave)
            var poly = new GeoPolygon(
                new GeoPoint(0.0, 0.0),
                new GeoPoint(4.0, 0.0),
                new GeoPoint(4.0, 2.0),
                new GeoPoint(2.0, 2.0),
                new GeoPoint(2.0, 4.0),
                new GeoPoint(0.0, 4.0)
            );

            // Point (3, 3) lies outside the polygon, although it is within the polygon's [0, 4] x [0, 4] bounding box
            Assert.False(poly.Contains(new GeoPoint(3.0, 3.0)));

            // Point (1, 3) lies inside the vertical branch of the L-shape
            Assert.True(poly.Contains(new GeoPoint(1.0, 3.0)));

            // Point (3, 1) lies inside the horizontal branch of the L-shape
            Assert.True(poly.Contains(new GeoPoint(3.0, 1.0)));
        }

        [Fact]
        public void Polygon_ZeroAreaCollinear_ReturnsCorrectArea()
        {
            // 3 collinear points -> flat polygon area = 0
            var poly = new GeoPolygon(
                new GeoPoint(0.0, 0.0),
                new GeoPoint(5.0, 5.0),
                new GeoPoint(10.0, 10.0)
            );

            Assert.Equal(0.0, poly.GetArea(), 12);
            Assert.Equal(0.0, poly.GetSignedArea(), 12);
        }

        [Fact]
        public void Polygon_Constructor_DropsRepeatedClosingVertex()
        {
            // Many drawing formats repeat the start vertex at the end to close the loop. The constructor must discard it,
            // otherwise the polygon will have a degenerate edge of length 0.
            var poly = new GeoPolygon(
                new GeoPoint(0.0, 0.0),
                new GeoPoint(4.0, 0.0),
                new GeoPoint(4.0, 4.0),
                new GeoPoint(0.0, 0.0));

            Assert.Equal(3, poly.VertexCount);
            Assert.Equal(3, poly.EdgeCount);
            Assert.Equal(8.0, poly.GetArea(), 12);
        }

        [Fact]
        public void Polygon_Constructor_RejectsNullAndTooFewVertices()
        {
            Assert.Throws<ArgumentNullException>(() => new GeoPolygon((IEnumerable<GeoPoint>)null));
            Assert.Throws<ArgumentException>(() => new GeoPolygon(new GeoPoint(0.0, 0.0)));
            Assert.Throws<ArgumentException>(
                () => new GeoPolygon(new GeoPoint(1.0, 1.0), new GeoPoint(1.0, 1.0), new GeoPoint(1.0, 1.0)));
        }

        [Fact]
        public void Polygon_Indexer_AndGetEdgeAt_RejectOutOfRange()
        {
            var poly = new GeoPolygon(
                new GeoPoint(0.0, 0.0),
                new GeoPoint(4.0, 0.0),
                new GeoPoint(4.0, 4.0));

            Assert.Throws<ArgumentOutOfRangeException>(() => poly[-1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => poly[3]);
            Assert.Throws<ArgumentOutOfRangeException>(() => poly.GetEdgeAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => poly.GetEdgeAt(3));
        }

        [Fact]
        public void Polygon_GetEdges_FormsClosedLoop()
        {
            var poly = new GeoPolygon(
                new GeoPoint(0.0, 0.0),
                new GeoPoint(4.0, 0.0),
                new GeoPoint(4.0, 4.0));

            var edges = poly.GetEdges().ToList();

            Assert.Equal(3, edges.Count);
            for (int i = 0; i < edges.Count; i++)
            {
                // The end point of this edge must match the start point of the next edge, with the last edge wrapping back to the first.
                Assert.Equal(edges[(i + 1) % edges.Count].StartPoint, edges[i].EndPoint);
            }
        }

        [Fact]
        public void Polygon_Equality_AndHashCode_WorkCorrectly()
        {
            var a = new GeoPolygon(new GeoPoint(0.0, 0.0), new GeoPoint(4.0, 0.0), new GeoPoint(4.0, 4.0));
            var b = new GeoPolygon(new GeoPoint(0.0, 0.0), new GeoPoint(4.0, 0.0), new GeoPoint(4.0, 4.0));
            var different = new GeoPolygon(new GeoPoint(0.0, 0.0), new GeoPoint(5.0, 0.0), new GeoPoint(5.0, 5.0));

            Assert.True(a.Equals(b));
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
            Assert.False(a.Equals(different));
            Assert.False(a.Equals((GeoPolygon)null));

            object notAPolygon = "not a GeoPolygon";
            Assert.False(a.Equals(notAPolygon));
        }

        [Fact]
        public void Polygon_IntersectsWithLine_TouchingBoundaryCounts()
        {
            var poly = new GeoPolygon(
                new GeoPoint(0.0, 0.0),
                new GeoPoint(4.0, 0.0),
                new GeoPoint(4.0, 4.0),
                new GeoPoint(0.0, 4.0));

            // A line segment touching exactly one vertex still counts as an intersection.
            Assert.True(poly.IntersectsWith(new GeoLine(4.0, 4.0, 8.0, 8.0)));

            // A line segment running just outside the boundary does not.
            Assert.False(poly.IntersectsWith(new GeoLine(4.5, 0.0, 4.5, 4.0)));
        }

        [Fact]
        public void Polygon_IntersectsWithPolygon_ConcaveNotchIsNotOccupied()
        {
            // L-shaped polygon, the missing corner is at the top-right.
            var lShape = new GeoPolygon(
                new GeoPoint(0.0, 0.0),
                new GeoPoint(4.0, 0.0),
                new GeoPoint(4.0, 2.0),
                new GeoPoint(2.0, 2.0),
                new GeoPoint(2.0, 4.0),
                new GeoPoint(0.0, 4.0));

            // Small polygon fits entirely inside the missing corner: inside bounding box but does NOT intersect the L-shape.
            var inNotch = new GeoPolygon(
                new GeoPoint(2.5, 2.5),
                new GeoPoint(3.5, 2.5),
                new GeoPoint(3.5, 3.5),
                new GeoPoint(2.5, 3.5));

            Assert.False(lShape.IntersectsWith(inNotch));
            Assert.False(inNotch.IntersectsWith(lShape));
        }

        [Fact]
        public void Polygon_ClockwiseAndCounterClockwise_BehaveIdenticallyForContainment()
        {
            var ccw = new GeoPolygon(
                new GeoPoint(0.0, 0.0),
                new GeoPoint(4.0, 0.0),
                new GeoPoint(4.0, 4.0),
                new GeoPoint(0.0, 4.0));

            var cw = new GeoPolygon(
                new GeoPoint(0.0, 0.0),
                new GeoPoint(0.0, 4.0),
                new GeoPoint(4.0, 4.0),
                new GeoPoint(4.0, 0.0));

            Assert.False(ccw.IsClockwise());
            Assert.True(cw.IsClockwise());

            // Vertices traversal direction must not affect containment checks or absolute area.
            Assert.Equal(ccw.GetArea(), cw.GetArea(), 12);
            Assert.Equal(ccw.Contains(new GeoPoint(2.0, 2.0)), cw.Contains(new GeoPoint(2.0, 2.0)));
            Assert.Equal(ccw.Contains(new GeoPoint(9.0, 9.0)), cw.Contains(new GeoPoint(9.0, 9.0)));
        }
    }
}

