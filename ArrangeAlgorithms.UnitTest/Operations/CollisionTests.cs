using System;
using ArrangeAlgorithms.Geometry;
using ArrangeAlgorithms.Operations;
using Xunit;

namespace ArrangeAlgorithms.UnitTest.Operations
{
    public class CollisionTests
    {
        #region Line - Line

        [Fact]
        public void LineLine_CollidesWith_ComprehensiveCases()
        {
            var l1 = new GeoLine(new GeoPoint(0, 5), new GeoPoint(10, 5));

            // Crossing lines
            var lCross = new GeoLine(new GeoPoint(5, 0), new GeoPoint(5, 10));
            Assert.True(Collision.CollidesWith(l1, lCross));

            // Touching at endpoint (T-junction)
            var lTouch = new GeoLine(new GeoPoint(5, 5), new GeoPoint(5, 15));
            Assert.True(Collision.CollidesWith(l1, lTouch));

            // Collinear overlapping
            var lOverlap = new GeoLine(new GeoPoint(5, 5), new GeoPoint(15, 5));
            Assert.True(Collision.CollidesWith(l1, lOverlap));

            // Parallel disjoint
            var lParallel = new GeoLine(new GeoPoint(0, 10), new GeoPoint(10, 10));
            Assert.False(Collision.CollidesWith(l1, lParallel));

            // Skew disjoint
            var lSkew = new GeoLine(new GeoPoint(0, 0), new GeoPoint(3, 3));
            Assert.False(Collision.CollidesWith(l1, lSkew));
        }

        #endregion

        #region Circle - Shapes

        [Fact]
        public void CircleCircle_CollidesWith_ComprehensiveCases()
        {
            var c1 = new GeoCircle(new GeoPoint(0, 0), 10);

            // Overlapping
            var cOverlap = new GeoCircle(new GeoPoint(10, 0), 5);
            Assert.True(Collision.CollidesWith(c1, cOverlap));

            // Tangent externally
            var cTangentExt = new GeoCircle(new GeoPoint(15, 0), 5);
            Assert.True(Collision.CollidesWith(c1, cTangentExt));

            // Tangent internally
            var cTangentInt = new GeoCircle(new GeoPoint(5, 0), 5);
            Assert.True(Collision.CollidesWith(c1, cTangentInt));

            // Concentric inside
            var cConcentric = new GeoCircle(new GeoPoint(0, 0), 3);
            Assert.True(Collision.CollidesWith(c1, cConcentric));

            // Disjoint
            var cDisjoint = new GeoCircle(new GeoPoint(20, 0), 4);
            Assert.False(Collision.CollidesWith(c1, cDisjoint));
        }

        [Fact]
        public void CircleLine_CollidesWith_ComprehensiveCases()
        {
            var circle = new GeoCircle(new GeoPoint(0, 0), 10);

            // Secant (crosses through)
            var lSecant = new GeoLine(new GeoPoint(-15, 0), new GeoPoint(15, 0));
            Assert.True(Collision.CollidesWith(circle, lSecant));

            // Tangent
            var lTangent = new GeoLine(new GeoPoint(-10, 10), new GeoPoint(10, 10));
            Assert.True(Collision.CollidesWith(circle, lTangent));

            // Line strictly inside circle
            var lInside = new GeoLine(new GeoPoint(-3, 0), new GeoPoint(3, 0));
            Assert.True(Collision.CollidesWith(circle, lInside));

            // Disjoint line
            var lDisjoint = new GeoLine(new GeoPoint(-10, 15), new GeoPoint(10, 15));
            Assert.False(Collision.CollidesWith(circle, lDisjoint));
        }

        [Fact]
        public void CircleRectangle_CollidesWith_ComprehensiveCases()
        {
            var rect = new GeoRectangle(new GeoPoint(0, 0), 20, 20, 0);

            // Center inside rectangle
            var cInside = new GeoCircle(new GeoPoint(0, 0), 5);
            Assert.True(Collision.CollidesWith(cInside, rect));

            // Circle overlapping edge
            var cOverlap = new GeoCircle(new GeoPoint(12, 0), 5);
            Assert.True(Collision.CollidesWith(cOverlap, rect));

            // Circle touching corner (10, 10): center at (13, 14), dist to (10, 10) = 5
            var cCorner = new GeoCircle(new GeoPoint(13, 14), 5);
            Assert.True(Collision.CollidesWith(cCorner, rect));

            // Disjoint circle
            var cDisjoint = new GeoCircle(new GeoPoint(25, 25), 5);
            Assert.False(Collision.CollidesWith(cDisjoint, rect));
        }

        [Fact]
        public void CirclePolygon_CollidesWith_ComprehensiveCases()
        {
            var poly = new GeoPolygon(new[]
            {
                new GeoPoint(0, 0),
                new GeoPoint(20, 0),
                new GeoPoint(20, 20),
                new GeoPoint(0, 20)
            });

            // Center inside polygon
            var cInside = new GeoCircle(new GeoPoint(10, 10), 3);
            Assert.True(Collision.CollidesWith(cInside, poly));

            // Intersecting edge
            var cIntersect = new GeoCircle(new GeoPoint(20, 10), 3);
            Assert.True(Collision.CollidesWith(cIntersect, poly));

            // Disjoint
            var cDisjoint = new GeoCircle(new GeoPoint(30, 30), 3);
            Assert.False(Collision.CollidesWith(cDisjoint, poly));
        }

        [Fact]
        public void CirclePolyline_CollidesWith_ComprehensiveCases()
        {
            var pl = new GeoPolyline(new[]
            {
                new GeoPoint(0, 0),
                new GeoPoint(10, 0),
                new GeoPoint(10, 10)
            });

            // Circle crossing first segment
            var cCross = new GeoCircle(new GeoPoint(5, 0), 3);
            Assert.True(Collision.CollidesWith(cCross, pl));

            // Circle touching bend vertex (10, 0)
            var cTouchVertex = new GeoCircle(new GeoPoint(13, 4), 5);
            Assert.True(Collision.CollidesWith(cTouchVertex, pl));

            // Disjoint circle
            var cDisjoint = new GeoCircle(new GeoPoint(0, 20), 3);
            Assert.False(Collision.CollidesWith(cDisjoint, pl));
        }

        #endregion

        #region Rectangle - Shapes (SAT)

        [Fact]
        public void RectangleRectangle_CollidesWith_SATRotatedCases()
        {
            var r1 = new GeoRectangle(new GeoPoint(0, 0), 10, 10, 0);

            // Aligned overlap
            var rAligned = new GeoRectangle(new GeoPoint(5, 5), 10, 10, 0);
            Assert.True(Collision.CollidesWith(r1, rAligned));

            // Rotated 45 degrees intersecting
            var rRotated = new GeoRectangle(new GeoPoint(7, 0), 5, 5, Math.PI / 4.0);
            Assert.True(Collision.CollidesWith(r1, rRotated));

            // Edge-touching
            var rEdgeTouch = new GeoRectangle(new GeoPoint(10, 0), 10, 10, 0);
            Assert.True(Collision.CollidesWith(r1, rEdgeTouch));

            // Disjoint
            var rDisjoint = new GeoRectangle(new GeoPoint(20, 20), 10, 10, 0);
            Assert.False(Collision.CollidesWith(r1, rDisjoint));
        }

        [Fact]
        public void RectangleLine_CollidesWith_ComprehensiveCases()
        {
            var rect = new GeoRectangle(new GeoPoint(0, 0), 20, 10, 0);

            // Line crossing through
            var lCross = new GeoLine(new GeoPoint(-15, 0), new GeoPoint(15, 0));
            Assert.True(Collision.CollidesWith(rect, lCross));

            // Line contained completely inside
            var lInside = new GeoLine(new GeoPoint(-5, 0), new GeoPoint(5, 0));
            Assert.True(Collision.CollidesWith(rect, lInside));

            // Disjoint parallel line
            var lDisjoint = new GeoLine(new GeoPoint(-10, 10), new GeoPoint(10, 10));
            Assert.False(Collision.CollidesWith(rect, lDisjoint));
        }

        [Fact]
        public void RectanglePolygon_CollidesWith_ComprehensiveCases()
        {
            var rect = new GeoRectangle(new GeoPoint(0, 0), 20, 10, 0);

            // Polygon overlapping rectangle
            var pOverlap = new GeoPolygon(new[]
            {
                new GeoPoint(5, 0),
                new GeoPoint(15, 0),
                new GeoPoint(15, 10),
                new GeoPoint(5, 10)
            });
            Assert.True(Collision.CollidesWith(rect, pOverlap));

            // Disjoint polygon
            var pDisjoint = new GeoPolygon(new[]
            {
                new GeoPoint(30, 30),
                new GeoPoint(40, 30),
                new GeoPoint(40, 40),
                new GeoPoint(30, 40)
            });
            Assert.False(Collision.CollidesWith(rect, pDisjoint));
        }

        [Fact]
        public void RectanglePolyline_CollidesWith_ComprehensiveCases()
        {
            var rect = new GeoRectangle(new GeoPoint(0, 0), 20, 10, 0);

            // Polyline cutting through rectangle
            var plCross = new GeoPolyline(new[]
            {
                new GeoPoint(-15, 0),
                new GeoPoint(0, 0),
                new GeoPoint(0, 15)
            });
            Assert.True(Collision.CollidesWith(plCross, rect));
            Assert.True(rect.CollidesWith(plCross));
            Assert.True(plCross.CollidesWith(rect));

            // Disjoint polyline
            var plDisjoint = new GeoPolyline(new[]
            {
                new GeoPoint(30, 30),
                new GeoPoint(40, 30)
            });
            Assert.False(Collision.CollidesWith(plDisjoint, rect));
            Assert.False(rect.CollidesWith(plDisjoint));
            Assert.False(plDisjoint.CollidesWith(rect));
        }

        #endregion

        #region Polygon & Polyline

        [Fact]
        public void PolygonPolygon_CollidesWith_ComprehensiveCases()
        {
            var p1 = new GeoPolygon(new[]
            {
                new GeoPoint(0, 0),
                new GeoPoint(10, 0),
                new GeoPoint(10, 10),
                new GeoPoint(0, 10)
            });

            // Overlapping
            var pOverlap = new GeoPolygon(new[]
            {
                new GeoPoint(5, 5),
                new GeoPoint(15, 5),
                new GeoPoint(15, 15),
                new GeoPoint(5, 15)
            });
            Assert.True(Collision.CollidesWith(p1, pOverlap));

            // Touching along edge
            var pTouch = new GeoPolygon(new[]
            {
                new GeoPoint(10, 0),
                new GeoPoint(20, 0),
                new GeoPoint(20, 10),
                new GeoPoint(10, 10)
            });
            Assert.True(Collision.CollidesWith(p1, pTouch));

            // Disjoint
            var pDisjoint = new GeoPolygon(new[]
            {
                new GeoPoint(20, 20),
                new GeoPoint(30, 20),
                new GeoPoint(30, 30),
                new GeoPoint(20, 30)
            });
            Assert.False(Collision.CollidesWith(p1, pDisjoint));
        }

        [Fact]
        public void PolylinePolyline_CollidesWith_ComprehensiveCases()
        {
            var pl1 = new GeoPolyline(new[]
            {
                new GeoPoint(0, 0),
                new GeoPoint(10, 0),
                new GeoPoint(10, 10)
            });

            // Crossing segment 1
            var plCross = new GeoPolyline(new[]
            {
                new GeoPoint(5, -5),
                new GeoPoint(5, 5)
            });
            Assert.True(Collision.CollidesWith(pl1, plCross));

            // Touching endpoint
            var plTouch = new GeoPolyline(new[]
            {
                new GeoPoint(10, 10),
                new GeoPoint(20, 10)
            });
            Assert.True(Collision.CollidesWith(pl1, plTouch));

            // Disjoint
            var plDisjoint = new GeoPolyline(new[]
            {
                new GeoPoint(0, 20),
                new GeoPoint(10, 20)
            });
            Assert.False(Collision.CollidesWith(pl1, plDisjoint));
        }

        #endregion
        #region Degenerate Rectangle Regression

        [Fact]
        public void RectangleRectangle_ZeroExtent_DoesNotThrowAndStillSeparates()
        {
            var flat = new GeoRectangle(new GeoPoint(0, 0), 10, 0);      // zero height
            var thin = new GeoRectangle(new GeoPoint(0, 0), 0, 10);      // zero width
            var degenerate = new GeoRectangle(new GeoPoint(0, 0), 0, 0); // a single point
            var overlapping = new GeoRectangle(new GeoPoint(1, 0), 10, 10);
            var faraway = new GeoRectangle(new GeoPoint(500, 500), 10, 10);

            // Normalizing a zero-length edge axis used to throw before the boxes were ever tested.
            Assert.True(Collision.CollidesWith(flat, overlapping));
            Assert.True(Collision.CollidesWith(thin, overlapping));
            Assert.True(Collision.CollidesWith(degenerate, overlapping));

            Assert.False(Collision.CollidesWith(flat, faraway));
            Assert.False(Collision.CollidesWith(thin, faraway));
            Assert.False(Collision.CollidesWith(degenerate, faraway));
        }

        #endregion

        #region Closed Polyline Region Regression

        [Fact]
        public void PolylinePolyline_NestedClosedLoops_Collide()
        {
            var outer = new GeoPolyline(true, new GeoPoint(0, 0), new GeoPoint(100, 0), new GeoPoint(100, 100), new GeoPoint(0, 100));
            var inner = new GeoPolyline(true, new GeoPoint(40, 40), new GeoPoint(60, 40), new GeoPoint(60, 60), new GeoPoint(40, 60));

            // No edges cross, but one loop encloses the other. The polygon form of the same geometry has
            // always reported a collision, so the polyline form has to agree.
            Assert.True(Collision.CollidesWith(outer, inner));
            Assert.True(Collision.CollidesWith(outer.ToPolygon(), inner.ToPolygon()));
            Assert.Equal(0.0, Distance.DistanceTo(outer, inner), 9);
        }

        [Fact]
        public void PolylinePolyline_DisjointLoops_DoNotCollide()
        {
            var left = new GeoPolyline(true, new GeoPoint(0, 0), new GeoPoint(10, 0), new GeoPoint(10, 10), new GeoPoint(0, 10));
            var right = new GeoPolyline(true, new GeoPoint(50, 0), new GeoPoint(60, 0), new GeoPoint(60, 10), new GeoPoint(50, 10));

            Assert.False(Collision.CollidesWith(left, right));
        }

        [Fact]
        public void OpenPolyline_EnclosesNothing_SoContainedShapesDoNotCollide()
        {
            // An open polyline is a curve: a shape sitting on its concave side is not inside it.
            var open = new GeoPolyline(new GeoPoint(0, 0), new GeoPoint(100, 0), new GeoPoint(100, 100));
            var box = new GeoRectangle(new GeoPoint(50, 90), 4, 4);

            Assert.False(Collision.CollidesWith(open, box));
        }

        [Fact]
        public void ClosedPolyline_EnclosedShapes_Collide()
        {
            var loop = new GeoPolyline(true, new GeoPoint(0, 0), new GeoPoint(100, 0), new GeoPoint(100, 100), new GeoPoint(0, 100));

            Assert.True(Collision.CollidesWith(loop, new GeoRectangle(new GeoPoint(50, 50), 4, 4)));
            Assert.True(Collision.CollidesWith(new GeoCircle(new GeoPoint(50, 50), 2), loop));
            Assert.True(Collision.CollidesWith(loop, new GeoLine(new GeoPoint(40, 50), new GeoPoint(60, 50))));

            var insidePolygon = new GeoPolygon(new GeoPoint(40, 40), new GeoPoint(60, 40), new GeoPoint(60, 60));
            Assert.True(Collision.CollidesWith(loop, insidePolygon));
        }

        #endregion
    }
}
