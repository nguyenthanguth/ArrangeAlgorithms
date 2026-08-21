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

        #region Closest Segment Tests

        [Theory]
        [InlineData(0.0001)]
        [InlineData(0.001)]
        [InlineData(0.005)]
        [InlineData(0.01)]
        [InlineData(0.017)]
        [InlineData(0.05)]
        public void GetClosestSegment_CrossingSegments_ReportZeroAtEveryAngle(double slope)
        {
            // The two segments meet at x = 50. An angular parallel test would file every slope under
            // EqualAngleRad as parallel and miss the crossing, reporting the gap between the endpoints
            // instead - roughly length * slope / 2, so an error that grows without bound as the segments
            // get longer.
            var horizontal = new GeoLine(new GeoPoint(0.0, 0.0), new GeoPoint(100.0, 0.0));
            var crossing = new GeoLine(new GeoPoint(0.0, -50.0 * slope), new GeoPoint(100.0, 50.0 * slope));

            Assert.Equal(0.0, Projection.GetClosestSegment(horizontal, crossing).Length, 9);
            Assert.Equal(0.0, Projection.GetClosestSegment(crossing, horizontal).Length, 9);
            Assert.Equal(0.0, Distance.DistanceTo(horizontal, crossing), 9);
            Assert.Equal(0.0, Distance.DistanceTo(crossing, horizontal), 9);
        }

        [Fact]
        public void GetClosestSegment_LineCrossingSliverPolygon_AgreesWithCollisionAndDistance()
        {
            // The bottom edge climbs 5 units over 1000, so it sits 0.2865 degrees off the line and is
            // genuinely crossed at (-100, 0).
            var poly = new GeoPolygon(
                new GeoPoint(-500.0, -2.0),
                new GeoPoint(500.0, 3.0),
                new GeoPoint(500.0, 40.0),
                new GeoPoint(-500.0, 40.0));

            var line = new GeoLine(-400.0, 0.0, 400.0, 0.0);

            Assert.True(Containment.IsPointOn(poly.GetEdgeAt(0), new GeoPoint(-100.0, 0.0)));
            Assert.True(Containment.IsPointOn(line, new GeoPoint(-100.0, 0.0)));

            Assert.True(Collision.CollidesWith(poly, line));
            Assert.Equal(0.0, Distance.DistanceTo(poly, line), 9);
            Assert.Equal(0.0, Projection.GetClosestSegment(line, poly).Length, 9);
            Assert.Equal(0.0, line.GetClosestOnBoundary(poly).Length, 9);
            Assert.Equal(0.0, poly.GetClosestOnBoundary(line).Length, 9);
        }

        [Fact]
        public void GetClosestSegment_ParallelOverlap_AnchorsAtMiddleOfTheOverlap()
        {
            // Fully overlapping: every pair between the two is 4 apart, so the middle is taken.
            var a = new GeoLine(0.0, 0.0, 10.0, 0.0);
            var b = new GeoLine(0.0, 4.0, 10.0, 4.0);
            var full = Projection.GetClosestSegment(a, b);
            Assert.Equal(4.0, full.Length, 9);
            Assert.True(full.StartPoint.IsEqualTo(new GeoPoint(5.0, 0.0)));
            Assert.True(full.EndPoint.IsEqualTo(new GeoPoint(5.0, 4.0)));

            // Partially overlapping: only the shared stretch counts, so the middle sits at x = 5.
            var shortOne = new GeoLine(3.0, 4.0, 7.0, 4.0);
            var partial = Projection.GetClosestSegment(a, shortOne);
            Assert.Equal(4.0, partial.Length, 9);
            Assert.True(partial.StartPoint.IsEqualTo(new GeoPoint(5.0, 0.0)));
            Assert.True(partial.EndPoint.IsEqualTo(new GeoPoint(5.0, 4.0)));

            // No overlap at all: the answer is the corner-to-corner pair, not a midpoint.
            var offset = new GeoLine(20.0, 4.0, 30.0, 4.0);
            var disjoint = Projection.GetClosestSegment(a, offset);
            Assert.True(disjoint.StartPoint.IsEqualTo(new GeoPoint(10.0, 0.0)));
            Assert.True(disjoint.EndPoint.IsEqualTo(new GeoPoint(20.0, 4.0)));
        }

        [Fact]
        public void GetClosestSegment_TieAcrossEdges_PrefersTheFaceOverTheCorner()
        {
            // The line runs parallel to the top edge. The right edge of the rectangle is exactly as far
            // away, but it only touches at its corner, so the top edge is the one to anchor to.
            var rect = new GeoRectangle(new GeoPoint(5.0, 0.0), 4.0, 2.0, 0.0);
            var line = new GeoLine(0.0, 5.0, 10.0, 5.0);

            var seg = line.GetClosestOnBoundary(rect);
            Assert.Equal(4.0, seg.Length, 9);
            Assert.True(seg.StartPoint.IsEqualTo(new GeoPoint(5.0, 5.0)));
            Assert.True(seg.EndPoint.IsEqualTo(new GeoPoint(5.0, 1.0)));

            // Same tie between two rectangles: the facing edges win over the corners of the side edges.
            var r1 = new GeoRectangle(new GeoPoint(0.0, 0.0), 4.0, 2.0, 0.0);
            var r2 = new GeoRectangle(new GeoPoint(10.0, 0.0), 4.0, 2.0, 0.0);

            var forward = r1.GetClosestOnBoundary(r2);
            Assert.Equal(6.0, forward.Length, 9);
            Assert.True(forward.StartPoint.IsEqualTo(new GeoPoint(2.0, 0.0)));
            Assert.True(forward.EndPoint.IsEqualTo(new GeoPoint(8.0, 0.0)));

            // Swapping the roles mirrors the very same segment.
            var backward = r2.GetClosestOnBoundary(r1);
            Assert.True(backward.StartPoint.IsEqualTo(forward.EndPoint));
            Assert.True(backward.EndPoint.IsEqualTo(forward.StartPoint));
        }

        [Fact]
        public void GetClosestSegment_NearParallelButNotTouching_KeepsTheExactMinimum()
        {
            // Guards the tie-break from over-reaching: these two never quite line up, so the true minimum
            // is at an endpoint and the middle of the overlap would be measurably worse.
            var a = new GeoLine(0.0, 0.0, 100.0, 0.0);
            var b = new GeoLine(0.0, 1.0, 100.0, 1.5);

            var seg = Projection.GetClosestSegment(a, b);
            Assert.Equal(1.0, seg.Length, 9);
            Assert.Equal(Distance.DistanceTo(a, b), seg.Length, 9);
            Assert.True(seg.EndPoint.IsEqualTo(new GeoPoint(0.0, 1.0)));
        }

        [Fact]
        public void GetClosestSegment_DegenerateInputs_AreMeasuredNotRejected()
        {
            var pointLike = new GeoLine(new GeoPoint(5.0, 5.0), new GeoPoint(5.0, 5.0));
            var line = new GeoLine(0.0, 0.0, 10.0, 0.0);

            Assert.Equal(5.0, Projection.GetClosestSegment(pointLike, line).Length, 9);
            Assert.Equal(5.0, Projection.GetClosestSegment(line, pointLike).Length, 9);

            var otherPointLike = new GeoLine(new GeoPoint(1.0, 1.0), new GeoPoint(1.0, 1.0));
            Assert.Equal(Math.Sqrt(32.0), Projection.GetClosestSegment(pointLike, otherPointLike).Length, 9);

            var circle = new GeoCircle(new GeoPoint(0.0, 0.0), 3.0);
            Assert.Equal(Math.Sqrt(50.0) - 3.0, Projection.GetClosestSegment(pointLike, circle).Length, 9);

            var pointCircle = new GeoCircle(new GeoPoint(0.0, 0.0), 0.0);
            Assert.Equal(4.0, Projection.GetClosestSegment(pointCircle, new GeoLine(4.0, 0.0, 4.0, 10.0)).Length, 9);

            var flatRect = new GeoRectangle(new GeoPoint(0.0, 0.0), 0.0, 0.0, 0.0);
            Assert.Equal(4.0, Projection.GetClosestSegment(flatRect, new GeoLine(4.0, -5.0, 4.0, 5.0)).Length, 9);
        }

        [Fact]
        public void GetClosestSegment_LooseTolerance_DoesNotCutTheEdgeScanShort()
        {
            // The first polyline edge sits 2.5 away and the last one 2.0 away. A scan that stopped as soon
            // as it was within EqualPoint would settle for the first and never reach the nearer edge.
            var rect = new GeoRectangle(new GeoPoint(0.0, 0.0), 2.0, 2.0, 0.0);
            var polyline = new GeoPolyline(
                new GeoPoint(3.5, -5.0),
                new GeoPoint(3.5, 5.0),
                new GeoPoint(3.0, 5.0),
                new GeoPoint(3.0, -5.0));

            var tight = new Tolerance(1E-9, 1E-9, 1E-9);
            var loose = new Tolerance(3.0, 3.0, Math.PI / 180.0);

            Assert.Equal(2.0, Projection.GetClosestSegment(rect, polyline, tight).Length, 9);
            Assert.Equal(2.0, Projection.GetClosestSegment(rect, polyline, loose).Length, 9);
        }

        [Fact]
        public void GetClosestSegment_MatchesBruteForce_OverEveryShapePair()
        {
            var rnd = new Random(20260821);
            Func<double, double, double> range = (lo, hi) => lo + rnd.NextDouble() * (hi - lo);

            for (int kindA = 0; kindA < 5; kindA++)
            {
                for (int kindB = 0; kindB < 5; kindB++)
                {
                    for (int iteration = 0; iteration < 40; iteration++)
                    {
                        object a = RandomShape(kindA, rnd, range);
                        object b = RandomShape(kindB, rnd, range);

                        GeoLine seg = ClosestSegmentOf(a, b);
                        double sampled = SampledMinimumDistance(a, b);

                        // Sampling can only ever overstate the true minimum, so the analytic answer must
                        // not come out above it.
                        Assert.True(seg.Length <= sampled + 1E-6,
                            "kinds " + kindA + "/" + kindB + ": analytic " + seg.Length + " > sampled " + sampled);

                        // The start point belongs to the first shape and the end point to the second.
                        Assert.True(seg.StartPoint.DistanceTo(ClosestOnBoundaryOf(a, seg.StartPoint)) < 1E-7);
                        Assert.True(seg.EndPoint.DistanceTo(ClosestOnBoundaryOf(b, seg.EndPoint)) < 1E-7);
                    }
                }
            }
        }

        [Fact]
        public void GetClosestSegment_LineToLine_AgreesWithDistanceTo()
        {
            var rnd = new Random(4242);
            Func<double> coord = () => rnd.NextDouble() * 100.0 - 50.0;

            for (int i = 0; i < 5000; i++)
            {
                var a = new GeoLine(new GeoPoint(coord(), coord()), new GeoPoint(coord(), coord()));
                var b = new GeoLine(new GeoPoint(coord(), coord()), new GeoPoint(coord(), coord()));

                Assert.Equal(Distance.DistanceTo(a, b), Projection.GetClosestSegment(a, b).Length, 9);
            }
        }

        [Fact]
        public void GetClosestSegment_SegmentsMeetingWithinTolerance_CountAsTouching()
        {
            // Neither pair strictly crosses, but each misses by less than EqualPoint, so both count as
            // meeting - the same endpoint slack Intersection.TryIntersectWith applies. The slack is a
            // distance, so it has to be carried into each segment's own parameter space: one case runs
            // past the end of the first segment, the other past the start of the second.
            double gap = Tolerance.Global.EqualPoint * 0.5;

            var stopsShort = new GeoLine(0.0, 0.0, 5.0, 0.0);
            var justBeyond = new GeoLine(5.0 + gap, -5.0, 5.0 + gap, 5.0);

            var forward = Projection.GetClosestSegment(stopsShort, justBeyond);
            var backward = Projection.GetClosestSegment(justBeyond, stopsShort);
            Assert.Equal(0.0, forward.Length, 9);
            Assert.Equal(0.0, backward.Length, 9);

            // The two never literally share a point, so the one that comes back is taken from the first
            // argument and clamped to it. That is what keeps the reversed overloads exact mirrors.
            Assert.Equal(5.0, forward.StartPoint.X, 9);
            Assert.Equal(0.0, forward.StartPoint.Y, 9);
            Assert.Equal(5.0 + gap, backward.StartPoint.X, 9);

            var spanning = new GeoLine(0.0, 0.0, 10.0, 0.0);
            var startsAbove = new GeoLine(5.0, gap, 5.0, 5.0);
            Assert.Equal(0.0, Projection.GetClosestSegment(spanning, startsAbove).Length, 9);
            Assert.Equal(0.0, Projection.GetClosestSegment(startsAbove, spanning).Length, 9);

            // Widen the miss well past the tolerance and it is measured rather than absorbed.
            var clearlyAbove = new GeoLine(5.0, 1.0, 5.0, 5.0);
            Assert.Equal(1.0, Projection.GetClosestSegment(spanning, clearlyAbove).Length, 9);
        }

        [Fact]
        public void GetClosestSegment_EndpointSlack_IsScaleInvariant()
        {
            // The slack is a distance, so on a 100000 unit segment it may only reach 1E-4 past the end,
            // not a proportion of the length. Applied unscaled it would swallow a gap of a whole unit and
            // call these two touching.
            var veryLong = new GeoLine(0.0, 0.0, 100000.0, 0.0);
            var pastTheEnd = new GeoLine(100001.0, -5.0, 100001.0, 5.0);

            Assert.Equal(1.0, Projection.GetClosestSegment(veryLong, pastTheEnd).Length, 9);
            Assert.Equal(1.0, Projection.GetClosestSegment(pastTheEnd, veryLong).Length, 9);
            Assert.Equal(1.0, Distance.DistanceTo(veryLong, pastTheEnd), 9);

            // The same shape of configuration a thousand times smaller: the gap is still the same
            // fraction of the length and is still measured rather than absorbed.
            var shorter = new GeoLine(0.0, 0.0, 100.0, 0.0);
            var pastItsEnd = new GeoLine(100.001, -5.0, 100.001, 5.0);

            Assert.Equal(0.001, Projection.GetClosestSegment(shorter, pastItsEnd).Length, 9);
            Assert.Equal(0.001, Distance.DistanceTo(shorter, pastItsEnd), 9);

            // What does get absorbed is the same absolute gap at either scale, because that is what the
            // tolerance actually measures.
            double within = Tolerance.Global.EqualPoint * 0.5;
            Assert.Equal(0.0, Projection.GetClosestSegment(veryLong, new GeoLine(100000.0 + within, -5.0, 100000.0 + within, 5.0)).Length, 9);
            Assert.Equal(0.0, Projection.GetClosestSegment(shorter, new GeoLine(100.0 + within, -5.0, 100.0 + within, 5.0)).Length, 9);
        }

        #region Closest Segment Helpers

        private static object RandomShape(int kind, Random rnd, Func<double, double, double> range)
        {
            const double Spread = 20.0;

            if (kind == 0)
            {
                return new GeoLine(
                    new GeoPoint(range(-Spread, Spread), range(-Spread, Spread)),
                    new GeoPoint(range(-Spread, Spread), range(-Spread, Spread)));
            }

            if (kind == 1)
            {
                return new GeoCircle(new GeoPoint(range(-Spread, Spread), range(-Spread, Spread)), range(1.0, Spread * 0.6));
            }

            if (kind == 2)
            {
                return new GeoRectangle(
                    new GeoPoint(range(-Spread, Spread), range(-Spread, Spread)),
                    range(1.0, Spread), range(1.0, Spread), range(0.0, Math.PI * 2.0));
            }

            if (kind == 3)
            {
                int corners = rnd.Next(3, 7);
                double cx = range(-Spread, Spread);
                double cy = range(-Spread, Spread);
                double radius = range(2.0, Spread * 0.7);

                var vertices = new GeoPoint[corners];
                for (int i = 0; i < corners; i++)
                {
                    double angle = 2.0 * Math.PI * i / corners;
                    double r = radius * range(0.6, 1.0);
                    vertices[i] = new GeoPoint(cx + r * Math.Cos(angle), cy + r * Math.Sin(angle));
                }

                return new GeoPolygon(vertices);
            }

            int count = rnd.Next(2, 6);
            var chain = new GeoPoint[count];
            double x = range(-Spread, Spread);
            double y = range(-Spread, Spread);
            for (int i = 0; i < count; i++)
            {
                x += range(-Spread * 0.5, Spread * 0.5);
                y += range(-Spread * 0.5, Spread * 0.5);
                chain[i] = new GeoPoint(x, y);
            }

            return new GeoPolyline(chain);
        }

        private static GeoPoint BoundaryPointAt(object shape, double parameter)
        {
            if (shape is GeoLine line) return line.GetPointAtParameter(parameter);
            if (shape is GeoCircle circle)
            {
                return new GeoPoint(
                    circle.Center.X + circle.Radius * Math.Cos(2.0 * Math.PI * parameter),
                    circle.Center.Y + circle.Radius * Math.Sin(2.0 * Math.PI * parameter));
            }
            if (shape is GeoRectangle rect) return Parametrization.GetPointAtParameter(rect, parameter);
            if (shape is GeoPolygon poly) return Parametrization.GetPointAtParameter(poly, parameter);
            return Parametrization.GetPointAtParameter((GeoPolyline)shape, parameter);
        }

        private static GeoPoint ClosestOnBoundaryOf(object shape, GeoPoint point)
        {
            if (shape is GeoLine line) return Projection.ProjectToLine(line, point);
            if (shape is GeoCircle circle) return Projection.ProjectToCircle(circle, point);
            if (shape is GeoRectangle rect) return Projection.ProjectToRectangle(rect, point);
            if (shape is GeoPolygon poly) return Projection.ProjectToPolygon(poly, point);
            return Projection.ProjectToPolyline((GeoPolyline)shape, point);
        }

        private static double SampledMinimumDistance(object a, object b)
        {
            const int Samples = 300;
            double best = double.MaxValue;

            for (int i = 0; i < Samples; i++)
            {
                double t = (double)i / Samples;

                GeoPoint onA = BoundaryPointAt(a, t);
                best = Math.Min(best, onA.DistanceTo(ClosestOnBoundaryOf(b, onA)));

                GeoPoint onB = BoundaryPointAt(b, t);
                best = Math.Min(best, onB.DistanceTo(ClosestOnBoundaryOf(a, onB)));
            }

            return best;
        }

        private static GeoLine ClosestSegmentOf(object a, object b)
        {
            if (a is GeoLine la)
            {
                if (b is GeoLine lb) return Projection.GetClosestSegment(la, lb);
                if (b is GeoCircle cb) return Projection.GetClosestSegment(la, cb);
                if (b is GeoRectangle rb) return Projection.GetClosestSegment(la, rb);
                if (b is GeoPolygon gb) return Projection.GetClosestSegment(la, gb);
                return Projection.GetClosestSegment(la, (GeoPolyline)b);
            }

            if (a is GeoCircle ca)
            {
                if (b is GeoLine lb) return Projection.GetClosestSegment(ca, lb);
                if (b is GeoCircle cb) return Projection.GetClosestSegment(ca, cb);
                if (b is GeoRectangle rb) return Projection.GetClosestSegment(ca, rb);
                if (b is GeoPolygon gb) return Projection.GetClosestSegment(ca, gb);
                return Projection.GetClosestSegment(ca, (GeoPolyline)b);
            }

            if (a is GeoRectangle ra)
            {
                if (b is GeoLine lb) return Projection.GetClosestSegment(ra, lb);
                if (b is GeoCircle cb) return Projection.GetClosestSegment(ra, cb);
                if (b is GeoRectangle rb) return Projection.GetClosestSegment(ra, rb);
                if (b is GeoPolygon gb) return Projection.GetClosestSegment(ra, gb);
                return Projection.GetClosestSegment(ra, (GeoPolyline)b);
            }

            if (a is GeoPolygon ga)
            {
                if (b is GeoLine lb) return Projection.GetClosestSegment(ga, lb);
                if (b is GeoCircle cb) return Projection.GetClosestSegment(ga, cb);
                if (b is GeoRectangle rb) return Projection.GetClosestSegment(ga, rb);
                if (b is GeoPolygon gb) return Projection.GetClosestSegment(ga, gb);
                return Projection.GetClosestSegment(ga, (GeoPolyline)b);
            }

            var pa = (GeoPolyline)a;
            if (b is GeoLine l2) return Projection.GetClosestSegment(pa, l2);
            if (b is GeoCircle c2) return Projection.GetClosestSegment(pa, c2);
            if (b is GeoRectangle r2) return Projection.GetClosestSegment(pa, r2);
            if (b is GeoPolygon g2) return Projection.GetClosestSegment(pa, g2);
            return Projection.GetClosestSegment(pa, (GeoPolyline)b);
        }

        #endregion

        #endregion
    }
}
