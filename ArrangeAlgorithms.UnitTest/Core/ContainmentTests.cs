using ArrangeAlgorithms.Geometry;
using ArrangeAlgorithms.Core;
using ArrangeAlgorithms.Enums;
using Xunit;

namespace ArrangeAlgorithms.UnitTest.Core
{
    public class ContainmentTests
    {
        #region IsPointOn Tests

        [Theory]
        [InlineData(5, 0, true)]                       // Point on interior
        [InlineData(0, 0, true)]                       // Point at StartPoint
        [InlineData(10, 0, true)]                      // Point at EndPoint
        [InlineData(-1, 0, false)]                     // Collinear before StartPoint
        [InlineData(11, 0, false)]                     // Collinear after EndPoint
        [InlineData(5, 0.1, false)]                    // Parallel offset
        public void IsPointOn_Line_ComprehensiveCases(double px, double py, bool expected)
        {
            var line = new GeoLine(new GeoPoint(0, 0), new GeoPoint(10, 0));
            Assert.Equal(expected, Containment.IsPointOn(line, new GeoPoint(px, py)));
        }

        [Theory]
        [InlineData(10, 0, true)]                      // East cardinal point on boundary
        [InlineData(0, 10, true)]                      // North cardinal point on boundary
        [InlineData(-10, 0, true)]                     // West cardinal point on boundary
        [InlineData(0, -10, true)]                     // South cardinal point on boundary
        [InlineData(0, 0, false)]                      // Center (not on boundary)
        [InlineData(5, 5, false)]                      // Inside (not on boundary)
        [InlineData(15, 0, false)]                     // Outside
        public void IsPointOn_Circle_ComprehensiveCases(double px, double py, bool expected)
        {
            var circle = new GeoCircle(new GeoPoint(0, 0), 10);
            Assert.Equal(expected, Containment.IsPointOn(circle, new GeoPoint(px, py)));
        }

        [Theory]
        [InlineData(5, 0, true)]                       // Bottom edge
        [InlineData(10, 5, true)]                      // Right edge
        [InlineData(5, 10, true)]                      // Top edge
        [InlineData(0, 5, true)]                       // Left edge
        [InlineData(0, 0, true)]                       // Vertex
        [InlineData(5, 5, false)]                      // Interior center
        [InlineData(15, 5, false)]                     // Exterior
        public void IsPointOn_Polygon_ComprehensiveCases(double px, double py, bool expected)
        {
            var poly = new GeoPolygon(new[]
            {
                new GeoPoint(0, 0),
                new GeoPoint(10, 0),
                new GeoPoint(10, 10),
                new GeoPoint(0, 10)
            });
            Assert.Equal(expected, Containment.IsPointOn(poly, new GeoPoint(px, py)));
        }

        [Theory]
        [InlineData(5, 0, true)]                       // On segment 1
        [InlineData(10, 5, true)]                      // On segment 2
        [InlineData(15, 10, true)]                     // On segment 3
        [InlineData(10, 0, true)]                      // On bend vertex 1
        [InlineData(10, 10, true)]                     // On bend vertex 2
        [InlineData(5, 5, false)]                      // Off polyline
        public void IsPointOn_Polyline_ComprehensiveCases(double px, double py, bool expected)
        {
            var pl = new GeoPolyline(new[]
            {
                new GeoPoint(0, 0),
                new GeoPoint(10, 0),
                new GeoPoint(10, 10),
                new GeoPoint(20, 10)
            });
            Assert.Equal(expected, Containment.IsPointOn(pl, new GeoPoint(px, py)));
        }

        #endregion

        #region Locate Tests

        [Theory]
        [InlineData(0, 0, PointLocation.Inside)]       // Circle center
        [InlineData(6, 0, PointLocation.Inside)]       // Inside interior
        [InlineData(10, 0, PointLocation.OnSide)]      // Circumference
        [InlineData(10.000001, 0, PointLocation.OnSide)]// Within default tolerance
        [InlineData(11, 0, PointLocation.OutSide)]     // Outside
        public void Locate_Circle_InsideOnAndOutside(double px, double py, PointLocation expected)
        {
            var circle = new GeoCircle(new GeoPoint(0, 0), 10);
            Assert.Equal(expected, Containment.Locate(circle, new GeoPoint(px, py)));
        }

        [Theory]
        [InlineData(0, 0, PointLocation.Inside)]       // Center
        [InlineData(10, 0, PointLocation.OnSide)]      // Right edge
        [InlineData(-10, 0, PointLocation.OnSide)]     // Left edge
        [InlineData(0, 5, PointLocation.OnSide)]       // Top edge
        [InlineData(0, -5, PointLocation.OnSide)]      // Bottom edge
        [InlineData(10, 5, PointLocation.OnSide)]      // Top-right corner
        [InlineData(-10, -5, PointLocation.OnSide)]    // Bottom-left corner
        [InlineData(15, 0, PointLocation.OutSide)]     // Outside
        public void Locate_Rectangle_InsideEdgesCornersOutside(double px, double py, PointLocation expected)
        {
            var rect = new GeoRectangle(new GeoPoint(0, 0), 20, 10, 0);
            Assert.Equal(expected, Containment.Locate(rect, new GeoPoint(px, py)));
        }

        [Fact]
        public void Locate_Polygon_ConvexAndConcave()
        {
            // Concave polygon with a V-shaped indentation on top edge
            var concave = new GeoPolygon(new[]
            {
                new GeoPoint(0, 0),
                new GeoPoint(20, 0),
                new GeoPoint(20, 20),
                new GeoPoint(10, 10), // V-notch vertex inside
                new GeoPoint(0, 20)
            });

            // Strictly inside solid region
            Assert.Equal(PointLocation.Inside, Containment.Locate(concave, new GeoPoint(5, 5)));
            Assert.Equal(PointLocation.Inside, Containment.Locate(concave, new GeoPoint(15, 5)));

            // On notch vertex
            Assert.Equal(PointLocation.OnSide, Containment.Locate(concave, new GeoPoint(10, 10)));

            // In the empty notch bay (outside polygon but within bounding box)
            Assert.Equal(PointLocation.OutSide, Containment.Locate(concave, new GeoPoint(10, 15)));

            // Completely outside
            Assert.Equal(PointLocation.OutSide, Containment.Locate(concave, new GeoPoint(-5, 0)));
        }

        [Theory]
        [InlineData(5, 0, PointLocation.OnSide)]       // On segment 1
        [InlineData(10, 0, PointLocation.OnSide)]      // On vertex
        [InlineData(10, 5, PointLocation.OnSide)]      // On segment 2
        [InlineData(5, 5, PointLocation.OutSide)]      // Outside
        public void Locate_Polyline_SegmentsVerticesOutside(double px, double py, PointLocation expected)
        {
            var pl = new GeoPolyline(new[]
            {
                new GeoPoint(0, 0),
                new GeoPoint(10, 0),
                new GeoPoint(10, 10)
            });
            Assert.Equal(expected, Containment.Locate(pl, new GeoPoint(px, py)));
        }

        [Theory]
        [InlineData(5, 0, PointLocation.OnSide)]       // On line midpoint
        [InlineData(0, 0, PointLocation.OnSide)]       // On line startpoint
        [InlineData(10, 0, PointLocation.OnSide)]      // On line endpoint
        [InlineData(5, 5, PointLocation.OutSide)]      // Off line
        public void Locate_Line_MidpointEndpointsOutside(double px, double py, PointLocation expected)
        {
            var line = new GeoLine(new GeoPoint(0, 0), new GeoPoint(10, 0));
            Assert.Equal(expected, Containment.Locate(line, new GeoPoint(px, py)));
        }

        #endregion

        #region Shape Contains Sub-Shapes Tests

        [Fact]
        public void Contains_Rectangle_LineAndPolyline()
        {
            var rect = new GeoRectangle(new GeoPoint(0, 0), 20, 20, 0);

            // Fully inside line
            var lInside = new GeoLine(new GeoPoint(-5, -5), new GeoPoint(5, 5));
            Assert.True(Containment.Contains(rect, lInside));

            // Line touching boundary endpoints
            var lTouching = new GeoLine(new GeoPoint(-10, 0), new GeoPoint(10, 0));
            Assert.True(Containment.Contains(rect, lTouching));

            // Line partially protruding outside
            var lProtruding = new GeoLine(new GeoPoint(0, 0), new GeoPoint(15, 0));
            Assert.False(Containment.Contains(rect, lProtruding));

            // Polyline inside
            var plInside = new GeoPolyline(new[]
            {
                new GeoPoint(-5, -5),
                new GeoPoint(0, -5),
                new GeoPoint(0, 5)
            });
            Assert.True(Containment.Contains(rect, plInside));
        }

        [Fact]
        public void Contains_Circle_LineAndCircle()
        {
            var cOuter = new GeoCircle(new GeoPoint(0, 0), 10);

            // Inner circle fully contained
            var cInner = new GeoCircle(new GeoPoint(2, 2), 3);
            Assert.True(Containment.Contains(cOuter, cInner));

            // Inner circle touching boundary internally
            var cTangentInner = new GeoCircle(new GeoPoint(5, 0), 5);
            Assert.True(Containment.Contains(cOuter, cTangentInner));

            // Overlapping circle protruding outside
            var cOverlap = new GeoCircle(new GeoPoint(8, 0), 5);
            Assert.False(Containment.Contains(cOuter, cOverlap));

            // Line segment inside circle
            var lInside = new GeoLine(new GeoPoint(-5, 0), new GeoPoint(5, 0));
            Assert.True(Containment.Contains(cOuter, lInside));
        }

        [Fact]
        public void Contains_Polygon_LinePolylineAndNestedShapes()
        {
            var poly = new GeoPolygon(new[]
            {
                new GeoPoint(0, 0),
                new GeoPoint(20, 0),
                new GeoPoint(20, 20),
                new GeoPoint(0, 20)
            });

            // Line segment inside
            var lInside = new GeoLine(new GeoPoint(5, 5), new GeoPoint(15, 15));
            Assert.True(Containment.Contains(poly, lInside));

            // Line segment cutting through boundary
            var lCross = new GeoLine(new GeoPoint(5, 5), new GeoPoint(25, 5));
            Assert.False(Containment.Contains(poly, lCross));

            // Polyline zigzag inside
            var plZigzag = new GeoPolyline(new[]
            {
                new GeoPoint(2, 2),
                new GeoPoint(18, 2),
                new GeoPoint(2, 18),
                new GeoPoint(18, 18)
            });
            Assert.True(Containment.Contains(poly, plZigzag));
        }

        #endregion
        #region Tolerance Propagation Regression

        [Fact]
        public void ContainsLine_HonoursTheSuppliedTolerance()
        {
            var loose = new Tolerance(5.0, 5.0);

            var circle = new GeoCircle(new GeoPoint(0, 0), 10);
            var pastTheRim = new GeoLine(new GeoPoint(0, 0), new GeoPoint(13, 0));
            Assert.False(Containment.Contains(circle, pastTheRim));
            Assert.True(Containment.Contains(circle, pastTheRim, loose));

            var rect = new GeoRectangle(new GeoPoint(0, 0), 10, 10);
            var pastTheEdge = new GeoLine(new GeoPoint(0, 0), new GeoPoint(6, 0));
            Assert.False(Containment.Contains(rect, pastTheEdge));
            Assert.True(Containment.Contains(rect, pastTheEdge, loose));
        }

        [Fact]
        public void ContainsPoint_Rectangle_HonoursTheSuppliedTolerance()
        {
            var rect = new GeoRectangle(new GeoPoint(0, 0), 10, 10);
            var justOutside = new GeoPoint(6, 0);

            Assert.False(Containment.Contains(rect, justOutside));
            Assert.True(Containment.Contains(rect, justOutside, new Tolerance(2.0, 2.0)));
        }

        #endregion

        #region Polygon Contains Line Regression

        [Fact]
        public void ContainsLine_DiagonalBetweenVertices_IsContained()
        {
            var square = new GeoPolygon(
                new GeoPoint(0, 0), new GeoPoint(10, 0),
                new GeoPoint(10, 10), new GeoPoint(0, 10));

            // The diagonal touches the boundary at both ends but never leaves the square. Treating any
            // crossing as a failure used to reject it.
            Assert.True(Containment.Contains(square, new GeoLine(new GeoPoint(0, 0), new GeoPoint(10, 10))));

            // A segment lying along an edge is contained for the same reason.
            Assert.True(Containment.Contains(square, new GeoLine(new GeoPoint(0, 0), new GeoPoint(10, 0))));

            // A segment that genuinely leaves is not.
            Assert.False(Containment.Contains(square, new GeoLine(new GeoPoint(5, 5), new GeoPoint(20, 5))));
        }

        [Fact]
        public void ContainsLine_ConcavePolygon_RejectsSegmentsCrossingTheNotch()
        {
            // An L shape: both endpoints sit inside, but the straight path between them exits the notch.
            var lShape = new GeoPolygon(
                new GeoPoint(0, 0), new GeoPoint(10, 0), new GeoPoint(10, 4),
                new GeoPoint(4, 4), new GeoPoint(4, 10), new GeoPoint(0, 10));

            Assert.True(Containment.Contains(lShape, new GeoPoint(9, 2)));
            Assert.True(Containment.Contains(lShape, new GeoPoint(2, 9)));
            Assert.False(Containment.Contains(lShape, new GeoLine(new GeoPoint(9, 2), new GeoPoint(2, 9))));

            // A segment staying within one arm is contained.
            Assert.True(Containment.Contains(lShape, new GeoLine(new GeoPoint(1, 1), new GeoPoint(9, 1))));
        }

        #endregion

        #region Polyline Contains Point

        [Fact]
        public void Polyline_HoldsOnlyThePointsOnItsPath()
        {
            // A chain of vertices tracing a square is still a curve, not the square it draws. There is no
            // Contains overload for a polyline at all, because it would only ever restate IsPointOn.
            var traced = new GeoPolyline(
                new GeoPoint(0, 0), new GeoPoint(10, 0), new GeoPoint(10, 10), new GeoPoint(0, 10), new GeoPoint(0, 0));
            var open = new GeoPolyline(new GeoPoint(0, 0), new GeoPoint(10, 0), new GeoPoint(10, 10));

            var inside = new GeoPoint(5, 5);
            var onPath = new GeoPoint(5, 0);
            var outside = new GeoPoint(50, 50);

            Assert.False(Containment.IsPointOn(traced, inside));
            Assert.True(Containment.IsPointOn(traced, onPath));
            Assert.False(Containment.IsPointOn(traced, outside));

            Assert.False(Containment.IsPointOn(open, inside));
            Assert.True(Containment.IsPointOn(open, onPath));
            Assert.False(Containment.IsPointOn(open, outside));

            Assert.Equal(PointLocation.OutSide, Containment.Locate(traced, inside));
            Assert.Equal(PointLocation.OnSide, Containment.Locate(traced, onPath));

            // Converting to a polygon is what brings the enclosed area into play.
            Assert.True(Containment.Contains(traced.ToPolygon(), inside));
            Assert.Equal(PointLocation.Inside, Containment.Locate(traced.ToPolygon(), inside));
        }

        #endregion
    }
}
