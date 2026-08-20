using System;
using ArrangeAlgorithms.Geometry;
using ArrangeAlgorithms.Core;
using Xunit;

namespace ArrangeAlgorithms.UnitTest.Core
{
    public class ProjectionTests
    {
        #region Project to Line Tests

        [Fact]
        public void ProjectToLine_InteriorAndClamped()
        {
            var line = new GeoLine(new GeoPoint(0, 0), new GeoPoint(10, 0));

            // Interior orthogonal projection
            var p1 = Projection.ProjectToLine(line, new GeoPoint(5, 7));
            Assert.True(p1.IsEqualTo(new GeoPoint(5, 0)));

            // Clamped to StartPoint
            var p2 = Projection.ProjectToLine(line, new GeoPoint(-5, 7));
            Assert.True(p2.IsEqualTo(new GeoPoint(0, 0)));

            // Clamped to EndPoint
            var p3 = Projection.ProjectToLine(line, new GeoPoint(15, 7));
            Assert.True(p3.IsEqualTo(new GeoPoint(10, 0)));
        }

        [Fact]
        public void ProjectToLine_NeverLeavesTheSegment()
        {
            var line = new GeoLine(new GeoPoint(0, 0), new GeoPoint(10, 0));

            // Far beyond either endpoint the result still sits on the segment itself, which is what
            // distinguishes this from projecting onto the infinite line carrying it.
            foreach (var far in new[] { new GeoPoint(-1000, 7), new GeoPoint(1000, 7) })
            {
                var projected = Projection.ProjectToLine(line, far);
                Assert.True(Containment.IsPointOn(line, projected));
            }

            // The facades agree.
            Assert.True(line.GetClosestPointOnBoundary(new GeoPoint(-1000, 7)).IsEqualTo(new GeoPoint(0, 0)));
            Assert.True(new GeoPoint(1000, 7).GetClosestPointOnBoundary(line).IsEqualTo(new GeoPoint(10, 0)));
        }

        [Fact]
        public void ProjectToLine_DegenerateLine()
        {
            var pointLine = new GeoLine(new GeoPoint(5, 5), new GeoPoint(5, 5));
            var proj = Projection.ProjectToLine(pointLine, new GeoPoint(10, 10));

            Assert.True(proj.IsEqualTo(new GeoPoint(5, 5)));
        }

        #endregion

        #region Project to Circle Tests

        [Fact]
        public void ProjectToCircle_OutsideInsideCenter()
        {
            var circle = new GeoCircle(new GeoPoint(0, 0), 10);

            // Point outside -> projects radially onto circumference
            var pOut = Projection.ProjectToCircle(circle, new GeoPoint(20, 0));
            Assert.True(pOut.IsEqualTo(new GeoPoint(10, 0)));

            // Point inside -> projects radially outwards onto circumference
            var pIn = Projection.ProjectToCircle(circle, new GeoPoint(0, 5));
            Assert.True(pIn.IsEqualTo(new GeoPoint(0, 10)));

            // Point already on circumference
            var pOn = Projection.ProjectToCircle(circle, new GeoPoint(10, 0));
            Assert.True(pOn.IsEqualTo(new GeoPoint(10, 0)));

            // Point at center -> returns default radial point on circumference
            var pCenter = Projection.ProjectToCircle(circle, new GeoPoint(0, 0));
            Assert.Equal(10.0, Distance.DistanceTo(pCenter, circle.Center), 4);
        }

        #endregion

        #region Project to Rectangle Tests

        [Fact]
        public void ProjectToRectangle_InsideAndOutsideBothLandOnAnEdge()
        {
            var rect = new GeoRectangle(new GeoPoint(0, 0), 20, 10, 0);   // X: [-10, 10], Y: [-5, 5]

            // Point inside is carried out to the nearest edge, not returned unchanged. At the centre the
            // horizontal edges are the nearer pair, five units away against ten.
            var pCentre = Projection.ProjectToRectangle(rect, new GeoPoint(0, 0));
            Assert.True(pCentre.IsEqualTo(new GeoPoint(0, 5)));

            // Inside but closer to the right edge.
            var pNearRight = Projection.ProjectToRectangle(rect, new GeoPoint(9, 0));
            Assert.True(pNearRight.IsEqualTo(new GeoPoint(10, 0)));

            // Inside but closer to the bottom edge.
            var pNearBottom = Projection.ProjectToRectangle(rect, new GeoPoint(0, -4));
            Assert.True(pNearBottom.IsEqualTo(new GeoPoint(0, -5)));

            // Point above top edge -> projects to top edge
            var pTop = Projection.ProjectToRectangle(rect, new GeoPoint(3, 10));
            Assert.True(pTop.IsEqualTo(new GeoPoint(3, 5)));

            // Point to the right of right edge -> projects to right edge
            var pRight = Projection.ProjectToRectangle(rect, new GeoPoint(15, 2));
            Assert.True(pRight.IsEqualTo(new GeoPoint(10, 2)));
        }

        [Fact]
        public void ProjectToRectangle_CornerProjections()
        {
            var rect = new GeoRectangle(new GeoPoint(0, 0), 20, 10, 0);

            // Point beyond top-right corner -> projects to corner (10, 5)
            var pTopRight = Projection.ProjectToRectangle(rect, new GeoPoint(15, 10));
            Assert.True(pTopRight.IsEqualTo(new GeoPoint(10, 5)));

            // Point beyond bottom-left corner -> projects to corner (-10, -5)
            var pBottomLeft = Projection.ProjectToRectangle(rect, new GeoPoint(-15, -10));
            Assert.True(pBottomLeft.IsEqualTo(new GeoPoint(-10, -5)));
        }

        [Fact]
        public void ProjectToRotatedRectangle_OBB()
        {
            // Rectangle rotated 90 degrees: width 20 along Y axis, height 10 along X axis
            var rect90 = new GeoRectangle(new GeoPoint(0, 0), 20, 10, Math.PI / 2.0);

            // Point on X axis (outside height bound = 5)
            var p = Projection.ProjectToRectangle(rect90, new GeoPoint(10, 0));
            Assert.True(p.IsEqualTo(new GeoPoint(5, 0)));
        }

        #endregion

        #region Project to Polygon & Polyline Tests

        [Fact]
        public void ProjectToPolygon_EdgeAndVertices()
        {
            var poly = new GeoPolygon(new[]
            {
                new GeoPoint(0, 0),
                new GeoPoint(10, 0),
                new GeoPoint(10, 10),
                new GeoPoint(0, 10)
            });

            // Point outside bottom edge -> projects to edge
            var pEdge = Projection.ProjectToPolygon(poly, new GeoPoint(5, -5));
            Assert.True(pEdge.IsEqualTo(new GeoPoint(5, 0)));

            // Point diagonally off corner (10, 10) -> projects to vertex
            var pCorner = Projection.ProjectToPolygon(poly, new GeoPoint(15, 15));
            Assert.True(pCorner.IsEqualTo(new GeoPoint(10, 10)));
        }

        [Fact]
        public void ProjectToPolyline_MultiSegmentsAndBends()
        {
            var pl = new GeoPolyline(new[]
            {
                new GeoPoint(0, 0),
                new GeoPoint(10, 0),
                new GeoPoint(10, 10)
            });

            // Orthogonal to first segment
            var p1 = Projection.ProjectToPolyline(pl, new GeoPoint(5, 5));
            Assert.True(p1.IsEqualTo(new GeoPoint(5, 0)));

            // Orthogonal to second segment
            var p2 = Projection.ProjectToPolyline(pl, new GeoPoint(15, 5));
            Assert.True(p2.IsEqualTo(new GeoPoint(10, 5)));

            // Closest to bend vertex
            var p3 = Projection.ProjectToPolyline(pl, new GeoPoint(12, -2));
            Assert.True(p3.IsEqualTo(new GeoPoint(10, 0)));
        }

        #endregion

        #region Vector Projections & Consistency

        [Fact]
        public void ProjectVector_OntoVector()
        {
            var v = new GeoVector(3, 4);
            var onto = new GeoVector(1, 0);

            var proj = Projection.Project(v, onto);
            Assert.True(proj.IsEqualTo(new GeoVector(3, 0)));
        }

        [Fact]
        public void ProjectVector_OntoUnitAxes()
        {
            var v = new GeoVector(3, 4);

            var projX = Projection.Project(v, GeoVector.XAxis);
            Assert.True(projX.IsEqualTo(new GeoVector(3, 0)));

            var projY = Projection.Project(v, GeoVector.YAxis);
            Assert.True(projY.IsEqualTo(new GeoVector(0, 4)));
        }

        [Fact]
        public void Projection_ConsistencyWithDistance()
        {
            var line = new GeoLine(new GeoPoint(0, 0), new GeoPoint(10, 0));
            var pt = new GeoPoint(5, 8);

            var closest = Projection.ProjectToLine(line, pt);
            double distFromProj = Distance.DistanceTo(pt, closest);
            double directDist = Distance.DistanceTo(line, pt);

            Assert.Equal(directDist, distFromProj, 4);
        }

        #endregion
        
        #region Explicit Tolerance Overloads

        [Fact]
        public void GetParameterOf_UsesTheSuppliedToleranceForDegeneracy()
        {
            var shortLine = new GeoLine(new GeoPoint(0, 0), new GeoPoint(0.5, 0));
            var midpoint = new GeoPoint(0.25, 0);

            // Under the default tolerance the segment is perfectly usable.
            Assert.Equal(0.5, Parametrization.GetParameterAtPoint(shortLine, midpoint), 9);

            // Under a coarse tolerance the whole segment counts as degenerate and the parameter is 0.
            Assert.Equal(0.0, Parametrization.GetParameterAtPoint(shortLine, midpoint, new Tolerance(1.0, 1.0)), 9);
        }

        [Fact]
        public void ProjectToCircle_UsesTheSuppliedToleranceAtTheCenter()
        {
            var circle = new GeoCircle(new GeoPoint(0, 0), 10);
            var nearCenter = new GeoPoint(0.5, 0);

            // Normally the offset from the center defines the direction.
            Assert.True(Projection.ProjectToCircle(circle, nearCenter).IsEqualTo(new GeoPoint(10, 0)));

            // With a coarse tolerance the point counts as coincident with the center and falls back to angle 0.
            Assert.True(Projection.ProjectToCircle(circle, new GeoPoint(0, 0.5), new Tolerance(1.0, 1.0))
                .IsEqualTo(new GeoPoint(10, 0)));
        }

        [Fact]
        public void ProjectVector_UsesTheSuppliedToleranceForAZeroAxis()
        {
            var vector = new GeoVector(3, 4);
            var tinyAxis = new GeoVector(0.5, 0);

            Assert.True(Projection.Project(vector, tinyAxis).IsEqualTo(new GeoVector(3, 0)));
            Assert.True(Projection.Project(vector, tinyAxis, new Tolerance(1.0, 1.0)).IsEqualTo(GeoVector.Zero));
        }

        [Fact]
        public void ProjectionAndTheGeometryFacades_AgreeOnEveryShape()
        {
            var line = new GeoLine(new GeoPoint(0, 0), new GeoPoint(10, 0));
            var circle = new GeoCircle(new GeoPoint(0, 0), 10);
            var rect = new GeoRectangle(new GeoPoint(0, 0), 20, 10, 0.3);
            var poly = new GeoPolygon(
                new GeoPoint(0, 0), new GeoPoint(10, 0),
                new GeoPoint(10, 10), new GeoPoint(0, 10));
            var polyline = new GeoPolyline(new GeoPoint(0, 0), new GeoPoint(10, 0), new GeoPoint(10, 10), new GeoPoint(0, 10));

            // Both classes report a point on the boundary, so they agree everywhere. Probes mix interior,
            // exterior, and on-boundary positions.
            var probes = new[]
            {
                new GeoPoint(5, 5), new GeoPoint(-20, 3), new GeoPoint(0, 0),
                new GeoPoint(100, -100), new GeoPoint(10, 0), new GeoPoint(1, 1)
            };

            foreach (var p in probes)
            {
                // Both directions of the facade route to the same Projection call.
                Assert.True(Projection.ProjectToLine(line, p).IsEqualTo(line.GetClosestPointOnBoundary(p)));
                Assert.True(Projection.ProjectToCircle(circle, p).IsEqualTo(circle.GetClosestPointOnBoundary(p)));
                Assert.True(Projection.ProjectToRectangle(rect, p).IsEqualTo(rect.GetClosestPointOnBoundary(p)));
                Assert.True(Projection.ProjectToPolygon(poly, p).IsEqualTo(poly.GetClosestPointOnBoundary(p)));
                Assert.True(Projection.ProjectToPolyline(polyline, p).IsEqualTo(polyline.GetClosestPointOnBoundary(p)));

                Assert.True(p.GetClosestPointOnBoundary(line).IsEqualTo(line.GetClosestPointOnBoundary(p)));
                Assert.True(p.GetClosestPointOnBoundary(circle).IsEqualTo(circle.GetClosestPointOnBoundary(p)));
                Assert.True(p.GetClosestPointOnBoundary(rect).IsEqualTo(rect.GetClosestPointOnBoundary(p)));
                Assert.True(p.GetClosestPointOnBoundary(poly).IsEqualTo(poly.GetClosestPointOnBoundary(p)));
                Assert.True(p.GetClosestPointOnBoundary(polyline).IsEqualTo(polyline.GetClosestPointOnBoundary(p)));
            }
        }

        #endregion
    }
}
