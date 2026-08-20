using System;
using ArrangeAlgorithms.Geometry;
using ArrangeAlgorithms.Core;
using Xunit;

namespace ArrangeAlgorithms.UnitTest.Operations
{
    public class DistanceTests
    {
        #region Point - Point Tests

        [Theory]
        [InlineData(0, 0, 0, 0, 0.0)]                  // Coincident points
        [InlineData(0, 0, 10, 0, 10.0)]                // Horizontal distance
        [InlineData(0, 0, 0, -15, 15.0)]               // Vertical negative distance
        [InlineData(0, 0, 3, 4, 5.0)]                  // Diagonal 3-4-5 triangle
        [InlineData(-3, -4, 3, 4, 10.0)]               // Across origin symmetry
        public void PointToPoint_BasicAndEdgeCases(double x1, double y1, double x2, double y2, double expected)
        {
            var p1 = new GeoPoint(x1, y1);
            var p2 = new GeoPoint(x2, y2);

            Assert.Equal(expected, Distance.DistanceTo(p1, p2), 4);
            Assert.Equal(expected * expected, Distance.GetDistanceSquaredTo(p1, p2), 4);
        }

        #endregion

        #region Point - Line Tests

        [Theory]
        [InlineData(5, 5, 5.0)]                        // Orthogonal projection lies within segment
        [InlineData(5, 0, 0.0)]                        // Point directly on segment
        [InlineData(-5, 0, 5.0)]                       // Clamped to StartPoint
        [InlineData(15, 0, 5.0)]                       // Clamped to EndPoint
        [InlineData(-3, 4, 5.0)]                       // Diagonal off StartPoint (3-4-5)
        public void PointToLine_OrthogonalAndClampedEndpoints(double px, double py, double expected)
        {
            var line = new GeoLine(new GeoPoint(0, 0), new GeoPoint(10, 0));
            var pt = new GeoPoint(px, py);

            Assert.Equal(expected, Distance.DistanceTo(line, pt), 4);
            Assert.Equal(expected, pt.DistanceTo(line), 4);
            Assert.Equal(expected, line.DistanceTo(pt), 4);
        }

        [Fact]
        public void PointToLine_DegenerateLine_ReturnsPointDistance()
        {
            var pointLine = new GeoLine(new GeoPoint(5, 5), new GeoPoint(5, 5));
            var target = new GeoPoint(5, 10);

            Assert.Equal(5.0, Distance.DistanceTo(pointLine, target), 4);
        }

        #endregion

        #region Line - Line Tests

        [Fact]
        public void LineToLine_IntersectingAndParallel()
        {
            var l1 = new GeoLine(new GeoPoint(0, 5), new GeoPoint(10, 5));

            // Crossing line -> dist = 0
            var lCross = new GeoLine(new GeoPoint(5, 0), new GeoPoint(5, 10));
            Assert.Equal(0.0, Distance.DistanceTo(l1, lCross), 4);

            // Parallel horizontal offset by 5 -> dist = 5
            var lParallel = new GeoLine(new GeoPoint(0, 10), new GeoPoint(10, 10));
            Assert.Equal(5.0, Distance.DistanceTo(l1, lParallel), 4);

            // Touching at endpoint (T-junction) -> dist = 0
            var lTouch = new GeoLine(new GeoPoint(5, 5), new GeoPoint(5, 15));
            Assert.Equal(0.0, Distance.DistanceTo(l1, lTouch), 4);
        }

        [Fact]
        public void LineToLine_CollinearAndSkew()
        {
            var l1 = new GeoLine(new GeoPoint(0, 0), new GeoPoint(10, 0));

            // Collinear disjoint -> gap = 5
            var lCollinear = new GeoLine(new GeoPoint(15, 0), new GeoPoint(25, 0));
            Assert.Equal(5.0, Distance.DistanceTo(l1, lCollinear), 4);

            // Skew disjoint -> closest points are (10, 0) and (10, 10)
            var lSkew = new GeoLine(new GeoPoint(10, 10), new GeoPoint(10, 20));
            Assert.Equal(10.0, Distance.DistanceTo(l1, lSkew), 4);

            // Symmetry: dist(A, B) == dist(B, A)
            Assert.Equal(Distance.DistanceTo(l1, lSkew), Distance.DistanceTo(lSkew, l1));
        }

        #endregion

        #region Circle - Shapes Tests

        [Theory]
        [InlineData(0, 0, 0.0)]                        // At center (inside) -> dist = 0
        [InlineData(5, 0, 0.0)]                        // Inside circle -> dist = 0
        [InlineData(10, 0, 0.0)]                       // On circumference -> dist = 0
        [InlineData(15, 0, 5.0)]                       // Outside circle -> dist = 15 - 10 = 5
        public void CircleToPoint_InsideOnAndOutside(double px, double py, double expected)
        {
            var circle = new GeoCircle(new GeoPoint(0, 0), 10);
            var pt = new GeoPoint(px, py);

            Assert.Equal(expected, Distance.DistanceTo(circle, pt), 4);
            Assert.Equal(expected, pt.DistanceTo(circle), 4);
            Assert.Equal(expected, circle.DistanceTo(pt), 4);
        }

        [Fact]
        public void CircleToLine_SecantTangentAndDisjoint()
        {
            var circle = new GeoCircle(new GeoPoint(0, 0), 10);

            // Secant (crosses through) -> dist = 0
            var lSecant = new GeoLine(new GeoPoint(-15, 0), new GeoPoint(15, 0));
            Assert.Equal(0.0, Distance.DistanceTo(circle, lSecant), 4);
            Assert.Equal(0.0, lSecant.DistanceTo(circle), 4);

            // Tangent (touches boundary) -> dist = 0
            var lTangent = new GeoLine(new GeoPoint(-10, 10), new GeoPoint(10, 10));
            Assert.Equal(0.0, Distance.DistanceTo(circle, lTangent), 4);
            Assert.Equal(0.0, lTangent.DistanceTo(circle), 4);

            // Disjoint parallel line -> dist = 15 - 10 = 5
            var lDisjoint = new GeoLine(new GeoPoint(-10, 15), new GeoPoint(10, 15));
            Assert.Equal(5.0, Distance.DistanceTo(circle, lDisjoint), 4);
            Assert.Equal(5.0, lDisjoint.DistanceTo(circle), 4);

            // Line segment strictly inside circle -> dist = 0
            var lInside = new GeoLine(new GeoPoint(-3, 0), new GeoPoint(3, 0));
            Assert.Equal(0.0, Distance.DistanceTo(circle, lInside), 4);
            Assert.Equal(0.0, lInside.DistanceTo(circle), 4);
        }

        [Fact]
        public void CircleToCircle_ConcentricTangentAndDisjoint()
        {
            var c1 = new GeoCircle(new GeoPoint(0, 0), 10);

            // Concentric smaller circle inside -> dist = 0
            var cConcentric = new GeoCircle(new GeoPoint(0, 0), 5);
            Assert.Equal(0.0, Distance.DistanceTo(c1, cConcentric), 4);

            // Tangent externally at (10, 0) -> dist = 0
            var cTangent = new GeoCircle(new GeoPoint(15, 0), 5);
            Assert.Equal(0.0, Distance.DistanceTo(c1, cTangent), 4);

            // Disjoint circles -> dist = 25 - (10 + 5) = 10
            var cDisjoint = new GeoCircle(new GeoPoint(25, 0), 5);
            Assert.Equal(10.0, Distance.DistanceTo(c1, cDisjoint), 4);
        }

        #endregion

        #region Rectangle - Shapes Tests

        [Theory]
        [InlineData(0, 0, 0.0)]                        // Inside center -> dist = 0
        [InlineData(10, 0, 0.0)]                       // On right edge -> dist = 0
        [InlineData(10, 5, 0.0)]                       // On corner -> dist = 0
        [InlineData(15, 0, 5.0)]                       // Outside right edge -> dist = 5
        [InlineData(0, 10, 5.0)]                       // Outside top edge -> dist = 5
        [InlineData(13, 9, 5.0)]                       // Diagonal from corner (10, 5): dx=3, dy=4 -> dist = 5
        public void RectangleToPoint_InsideEdgesAndCorners(double px, double py, double expected)
        {
            var rect = new GeoRectangle(new GeoPoint(0, 0), 20, 10, 0);
            var pt = new GeoPoint(px, py);

            Assert.Equal(expected, Distance.DistanceTo(rect, pt), 4);
            Assert.Equal(expected, pt.DistanceTo(rect), 4);
            Assert.Equal(expected, rect.DistanceTo(pt), 4);
        }

        [Fact]
        public void RectangleToLine_CrossingContainedAndDisjoint()
        {
            var rect = new GeoRectangle(new GeoPoint(0, 0), 20, 10, 0);

            // Crossing line -> dist = 0
            var lCrossing = new GeoLine(new GeoPoint(-15, 0), new GeoPoint(15, 0));
            Assert.Equal(0.0, Distance.DistanceTo(rect, lCrossing), 4);

            // Line segment completely inside -> dist = 0
            var lInside = new GeoLine(new GeoPoint(-5, 0), new GeoPoint(5, 0));
            Assert.Equal(0.0, Distance.DistanceTo(rect, lInside), 4);

            // Parallel line offset by 5 -> dist = 5
            var lParallel = new GeoLine(new GeoPoint(-10, 10), new GeoPoint(10, 10));
            Assert.Equal(5.0, Distance.DistanceTo(rect, lParallel), 4);

            // Line diagonally off corner (10, 5): (13, 9)-(20, 9) -> dist = 5
            var lCorner = new GeoLine(new GeoPoint(13, 9), new GeoPoint(20, 9));
            Assert.Equal(5.0, Distance.DistanceTo(rect, lCorner), 4);
        }

        [Fact]
        public void RectangleToRectangle_OverlapTouchAndSeparated()
        {
            var r1 = new GeoRectangle(new GeoPoint(0, 0), 10, 10, 0);

            // Overlapping -> dist = 0
            var rOverlap = new GeoRectangle(new GeoPoint(5, 5), 10, 10, 0);
            Assert.Equal(0.0, Distance.DistanceTo(r1, rOverlap), 4);

            // Edge-touching at x = 10 -> dist = 0
            var rTouch = new GeoRectangle(new GeoPoint(10, 0), 10, 10, 0);
            Assert.Equal(0.0, Distance.DistanceTo(r1, rTouch), 4);

            // Separated by gap = 5 -> dist = 5
            var rSeparated = new GeoRectangle(new GeoPoint(15, 0), 10, 10, 0);
            Assert.Equal(5.0, Distance.DistanceTo(r1, rSeparated), 4);

            // Rotated 45 degrees separated -> dist > 0
            var rRotated = new GeoRectangle(new GeoPoint(20, 20), 5, 5, Math.PI / 4.0);
            Assert.True(Distance.DistanceTo(r1, rRotated) > 0.0);
        }

        #endregion

        #region Polygon & Polyline Tests

        [Fact]
        public void PolygonAndPolyline_ComplexDistanceCalculations()
        {
            var poly = new GeoPolygon(new[]
            {
                new GeoPoint(0, 0),
                new GeoPoint(20, 0),
                new GeoPoint(20, 20),
                new GeoPoint(10, 10), // Concave bay vertex
                new GeoPoint(0, 20)
            });

            // Point on boundary -> dist = 0
            Assert.Equal(0.0, Distance.DistanceTo(poly, new GeoPoint(10, 0)), 4);

            // Point inside concave bay -> closest edge distance
            var ptInBay = new GeoPoint(10, 15);
            Assert.True(Distance.DistanceTo(poly, ptInBay) > 0.0);

            // Polyline tests
            var pl = new GeoPolyline(new[]
            {
                new GeoPoint(0, 0),
                new GeoPoint(10, 0),
                new GeoPoint(10, 10),
                new GeoPoint(20, 10)
            });

            // Point on bend vertex (10, 0) -> dist = 0
            Assert.Equal(0.0, Distance.DistanceTo(pl, new GeoPoint(10, 0)), 4);

            // Point perpendicular to vertical segment -> dist = 5
            Assert.Equal(5.0, Distance.DistanceTo(pl, new GeoPoint(15, 5)), 4);

            // Line intersecting polyline -> dist = 0
            var lIntersect = new GeoLine(new GeoPoint(5, -5), new GeoPoint(5, 5));
            Assert.Equal(0.0, Distance.DistanceTo(pl, lIntersect), 4);
        }

        #endregion

        #region Filled Region Semantics Regression

        [Fact]
        public void DistanceToInterior_IsZeroForEveryClosedShape()
        {
            var interior = new GeoPoint(5, 5);

            var poly = new GeoPolygon(
                new GeoPoint(0, 0), new GeoPoint(10, 0),
                new GeoPoint(10, 10), new GeoPoint(0, 10));
            var rect = new GeoRectangle(new GeoPoint(5, 5), 10, 10);
            var circle = new GeoCircle(new GeoPoint(5, 5), 10);

            // A polygon used to report the distance to its boundary instead, unlike every other closed shape.
            Assert.Equal(0.0, Distance.DistanceTo(poly, interior), 9);
            Assert.Equal(0.0, Distance.DistanceTo(rect, interior), 9);
            Assert.Equal(0.0, Distance.DistanceTo(circle, interior), 9);
        }

        [Fact]
        public void DistanceToPolyline_MeasuresThePathEvenWhenItSurroundsThePoint()
        {
            // A polyline is a curve, so a point in the middle of a shape it traces is at its distance
            // from the path, not at zero. Only the polygon form encloses.
            var traced = new GeoPolyline(
                new GeoPoint(0, 0), new GeoPoint(10, 0), new GeoPoint(10, 10), new GeoPoint(0, 10), new GeoPoint(0, 0));
            var interior = new GeoPoint(5, 5);

            Assert.Equal(5.0, Distance.DistanceTo(traced, interior), 9);
            Assert.Equal(0.0, Distance.DistanceTo(traced.ToPolygon(), interior), 9);
        }

        [Fact]
        public void DistanceToInteriorLine_IsZeroForPolygonAndRectangleAlike()
        {
            var inside = new GeoLine(new GeoPoint(4, 4), new GeoPoint(6, 6));

            var poly = new GeoPolygon(
                new GeoPoint(0, 0), new GeoPoint(10, 0),
                new GeoPoint(10, 10), new GeoPoint(0, 10));
            var rect = new GeoRectangle(new GeoPoint(5, 5), 10, 10);

            Assert.Equal(0.0, Distance.DistanceTo(poly, inside), 9);
            Assert.Equal(0.0, Distance.DistanceTo(rect, inside), 9);
        }

        [Fact]
        public void BoundaryDistance_RemainsReachableThroughProjection()
        {
            var poly = new GeoPolygon(
                new GeoPoint(0, 0), new GeoPoint(10, 0),
                new GeoPoint(10, 10), new GeoPoint(0, 10));
            var interior = new GeoPoint(5, 5);

            // The region distance is zero, but the distance to the boundary is still available.
            Assert.Equal(0.0, Distance.DistanceTo(poly, interior), 9);
            Assert.Equal(5.0, interior.DistanceTo(Projection.ProjectToPolygon(poly, interior)), 9);
            Assert.Equal(5.0, interior.DistanceTo(poly.GetClosestPointOnBoundary(interior)), 9);
        }

        [Fact]
        public void DistanceToExterior_IsUnchanged()
        {
            var poly = new GeoPolygon(
                new GeoPoint(0, 0), new GeoPoint(10, 0),
                new GeoPoint(10, 10), new GeoPoint(0, 10));

            Assert.Equal(5.0, Distance.DistanceTo(poly, new GeoPoint(15, 5)), 9);
            Assert.Equal(5.0, Distance.DistanceTo(poly, new GeoPoint(5, -5)), 9);
        }

        [Fact]
        public void OpenPolyline_IsACurve_SoItsHullIsNotInside()
        {
            var open = new GeoPolyline(new GeoPoint(0, 0), new GeoPoint(10, 0), new GeoPoint(10, 10));

            // (5, 5) sits in the concave corner but an open polyline encloses nothing.
            Assert.Equal(5.0, Distance.DistanceTo(open, new GeoPoint(5, 5)), 9);
        }

        #endregion
    }
}
