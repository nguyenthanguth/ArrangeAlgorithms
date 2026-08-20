using System.Collections.Generic;
using System.Linq;
using ArrangeAlgorithms.Enums;
using ArrangeAlgorithms.Geometry;
using ArrangeAlgorithms.Core;
using Xunit;

namespace ArrangeAlgorithms.UnitTest
{
    /// <summary>
    /// Runs the code shown in README.md and checks the numbers it quotes.
    /// <para>
    /// The README used to document an <c>IntersectsWith</c> family that never existed anywhere in the
    /// library, and it went unnoticed because nothing compiled it. Prose drifts silently; a test does not.
    /// Every snippet in the README is reproduced here, so renaming or removing an API breaks the build
    /// rather than leaving a reader with instructions that cannot work.
    /// </para>
    /// </summary>
    public class ReadmeExamplesTests
    {
        [Fact]
        public void QuickStart_RunsAndFillsInEveryLabel()
        {
            var leader = new GeoLine(0.0, 0.0, 2000.0, 0.0);

            var arranges = new List<Arrange>
            {
                new Arrange
                {
                    GeoRectangle = new GeoRectangle(new GeoPoint(1000.0, 0.0), 2000.0, 1000.0),
                    GeoLine      = leader,
                    MarkOffsetFromLine = 50.0,
                    BlockPolygons = new List<GeoPolygon>(),
                    BlockLines    = new List<GeoLine>()
                }
            };

            List<GeoVector> moves = Arrange.Run(arranges);

            Assert.Equal(arranges.Count, moves.Count);

            for (int i = 0; i < arranges.Count; i++)
            {
                GeoVector move = arranges[i].TranslationVector;
                GeoPoint newPosition = arranges[i].GeoRectangle.Center + move;
                bool isPlaced = arranges[i].Placed;

                // The README promises the returned vector and the property are the same value.
                Assert.True(move.IsEqualTo(moves[i]));
                Assert.True(newPosition.IsEqualTo(arranges[i].GeoRectangle.Center + moves[i]));
                Assert.True(isPlaced);
            }
        }

        [Fact]
        public void QuickStart_OptionsAndPerLabelOffset()
        {
            var leader = new GeoLine(0.0, 0.0, 2000.0, 0.0);

            var options = new ArrangeOptions
            {
                Algorithm           = ArrangeAlgorithmType.BoundedBacktracking,
                RowGap              = 20.0,
                PerpendicularLevels = 3
            };

            var smallTextLabel = new Arrange
            {
                GeoRectangle = new GeoRectangle(new GeoPoint(1000.0, 0.0), 2000.0, 1000.0),
                GeoLine      = leader,
                MarkOffsetFromLine = 50.0
            };

            var largeTextLabel = new Arrange
            {
                GeoRectangle = new GeoRectangle(new GeoPoint(1000.0, 0.0), 4000.0, 2000.0),
                GeoLine      = leader,
                MarkOffsetFromLine = 200.0
            };

            List<GeoVector> moves = Arrange.Run(new List<Arrange> { smallTextLabel, largeTextLabel }, options);

            Assert.Equal(2, moves.Count);

            // The larger label carries the larger offset, so it has to end up further from the guide.
            double smallGap = smallTextLabel.GeoRectangle.Translate(moves[0]).DistanceTo(leader);
            double largeGap = largeTextLabel.GeoRectangle.Translate(moves[1]).DistanceTo(leader);
            Assert.True(largeGap > smallGap);
        }

        [Fact]
        public void GeometricTypes_CurveVersusRegion()
        {
            var traced = new GeoPolyline(
                new GeoPoint(0, 0), new GeoPoint(10, 0),
                new GeoPoint(10, 10), new GeoPoint(0, 10), new GeoPoint(0, 0));

            // Exactly the four results quoted in the README table.
            Assert.Equal(PointLocation.OutSide, traced.Locate(new GeoPoint(5, 5)));
            Assert.Equal(5.0, traced.DistanceTo(new GeoPoint(5, 5)), 9);
            Assert.Equal(PointLocation.Inside, traced.ToPolygon().Locate(new GeoPoint(5, 5)));
            Assert.Equal(0.0, traced.ToPolygon().DistanceTo(new GeoPoint(5, 5)), 9);
        }

        [Fact]
        public void GeometricTypes_OnlyRegionsOfferContains()
        {
            // The README says Contains belongs to regions and Locate to everything. Curves reach Locate
            // and IsPointOn only, which is why the polyline lines below have no Contains counterpart.
            var poly = new GeoPolygon(
                new GeoPoint(0, 0), new GeoPoint(10, 0), new GeoPoint(10, 10), new GeoPoint(0, 10));
            var rect = new GeoRectangle(new GeoPoint(5, 5), 10, 10);
            var circle = new GeoCircle(new GeoPoint(5, 5), 5);
            var polyline = new GeoPolyline(new GeoPoint(0, 0), new GeoPoint(10, 0), new GeoPoint(10, 10));
            var line = new GeoLine(new GeoPoint(0, 0), new GeoPoint(10, 0));

            var centre = new GeoPoint(5, 5);
            Assert.True(poly.Contains(centre));
            Assert.True(rect.Contains(centre));
            Assert.True(circle.Contains(centre));

            var onPath = new GeoPoint(5, 0);
            Assert.Equal(PointLocation.OnSide, polyline.Locate(onPath));
            Assert.Equal(PointLocation.OnSide, line.Locate(onPath));
            Assert.True(polyline.IsPointOn(onPath));
            Assert.True(line.IsPointOn(onPath));
        }

        [Fact]
        public void CollisionAndIntersection_EveryPairWorksFromBothDirections()
        {
            var line = new GeoLine(new GeoPoint(0, 5), new GeoPoint(10, 5));
            var otherLine = new GeoLine(new GeoPoint(5, 0), new GeoPoint(5, 10));
            var rect = new GeoRectangle(new GeoPoint(5, 5), 8, 8);
            var otherRect = new GeoRectangle(new GeoPoint(6, 6), 8, 8);
            var poly = new GeoPolygon(
                new GeoPoint(0, 0), new GeoPoint(10, 0), new GeoPoint(10, 10), new GeoPoint(0, 10));
            var otherPoly = new GeoPolygon(
                new GeoPoint(5, 5), new GeoPoint(15, 5), new GeoPoint(15, 15), new GeoPoint(5, 15));
            var circle = new GeoCircle(new GeoPoint(5, 5), 4);
            var polyline = new GeoPolyline(new GeoPoint(0, 5), new GeoPoint(10, 5), new GeoPoint(10, 10));

            // The README claims every pair is reachable from both sides and agrees either way.
            Assert.Equal(rect.CollidesWith(line), line.CollidesWith(rect));
            Assert.Equal(rect.CollidesWith(poly), poly.CollidesWith(rect));
            Assert.Equal(circle.CollidesWith(polyline), polyline.CollidesWith(circle));
            Assert.Equal(circle.CollidesWith(line), line.CollidesWith(circle));
            Assert.Equal(poly.CollidesWith(line), line.CollidesWith(poly));
            Assert.Equal(polyline.CollidesWith(poly), poly.CollidesWith(polyline));
            Assert.Equal(polyline.CollidesWith(rect), rect.CollidesWith(polyline));

            Assert.True(rect.CollidesWith(otherRect));
            Assert.True(poly.CollidesWith(otherPoly));
            Assert.True(line.CollidesWith(otherLine));

            GeoPoint[] points = poly.GetIntersections(line);
            Assert.Equal(2, points.Length);
        }

        [Fact]
        public void Splitting_MatchesWhatTheReadmeShows()
        {
            var line = new GeoLine(new GeoPoint(0, 0), new GeoPoint(10, 0));
            var polyline = new GeoPolyline(new GeoPoint(0, 0), new GeoPoint(10, 0), new GeoPoint(10, 10));
            var point = new GeoPoint(4, 0);
            var cutter = new GeoLine(new GeoPoint(6, -5), new GeoPoint(6, 5));

            Assert.True(Splition.TrySplitBy(line, point, out GeoLine first, out GeoLine second));
            Assert.True(Splition.TrySplitAtDistance(polyline, 12.5, out GeoPolyline head, out GeoPolyline tail));

            GeoLine[] pieces = Splition.SplitAtDistances(line, new[] { 2.0, 5.0, 8.0 });
            Assert.True(Splition.TrySplitBy(polyline, cutter, out GeoPolyline[] parts));

            // The first piece holds the start point and the last holds the end point.
            Assert.True(first.StartPoint.IsEqualTo(line.StartPoint));
            Assert.True(second.EndPoint.IsEqualTo(line.EndPoint));
            Assert.True(head[0].IsEqualTo(polyline[0]));
            Assert.True(tail[tail.VertexCount - 1].IsEqualTo(polyline[polyline.VertexCount - 1]));

            Assert.Equal(4, pieces.Length);
            Assert.Equal(2, parts.Length);
        }

        [Fact]
        public void Splitting_SkipsAndMergesExactlyAsDocumented()
        {
            var tolerance = new Tolerance(1E-4, 1E-4);
            var polyline = new GeoPolyline(new GeoPoint(0, 0), new GeoPoint(10, 0), new GeoPoint(10, 10));

            // "Positions outside the subject, or landing on one of its endpoints, are not splits."
            GeoPolyline[] skipped = Splition.SplitAtDistances(
                polyline, new[] { -5.0, 0.0, 20.0, 50.0 }, tolerance);
            Assert.Single(skipped);

            // "Positions closer together than the tolerance merge into one."
            GeoPolyline[] merged = Splition.SplitAtDistances(
                polyline, new[] { 5.0, 5.0 + tolerance.EqualPoint * 0.5 }, tolerance);
            Assert.Equal(2, merged.Length);

            // "A position within a tolerance of an existing vertex snaps onto it, so no piece and no
            // edge is ever shorter than the tolerance."
            GeoPolyline[] snapped = Splition.SplitAtDistances(
                polyline, new[] { 10.0 + tolerance.EqualPoint * 0.5 }, tolerance);
            Assert.Equal(2, snapped.Length);

            foreach (GeoPolyline piece in snapped)
            {
                Assert.True(piece.Length > tolerance.EqualPoint);
                for (int e = 0; e < piece.EdgeCount; e++)
                {
                    Assert.True(piece.GetEdgeAt(e).Length > tolerance.EqualPoint);
                }
            }
        }

        [Fact]
        public void Tolerance_GlobalAppliesToOverloadsThatDoNotPassOne()
        {
            Tolerance saved = Tolerance.Global;
            try
            {
                var line = new GeoLine(new GeoPoint(0, 0), new GeoPoint(10, 0));
                var nearby = new GeoPoint(5, 0.5);

                Tolerance.Global = new Tolerance(1E-4, 1E-4);
                Assert.False(line.IsPointOn(nearby));

                Tolerance.Global = new Tolerance(1.0, 1.0);
                Assert.True(line.IsPointOn(nearby));

                // An explicit tolerance always wins over the global one.
                Assert.False(line.IsPointOn(nearby, new Tolerance(1E-4, 1E-4)));
            }
            finally
            {
                Tolerance.Global = saved;
            }
        }

        [Fact]
        public void Splitting_AgainstAPolygonSortsBySide()
        {
            var polygon = new GeoPolygon(
                new GeoPoint(0, 0), new GeoPoint(10, 0), new GeoPoint(10, 10), new GeoPoint(0, 10));
            var line = new GeoLine(new GeoPoint(-5, 5), new GeoPoint(15, 5));
            var polyline = new GeoPolyline(new GeoPoint(-5, 5), new GeoPoint(5, 5), new GeoPoint(5, 15));

            Assert.True(Splition.TrySplitBy(line, polygon, out GeoLine[] inside, out GeoLine[] outside));
            Assert.Single(inside);
            Assert.Equal(2, outside.Length);

            Assert.True(Splition.TrySplitBy(polyline, polygon, out GeoPolyline[] pathInside, out GeoPolyline[] pathOutside));
            Assert.Equal(polyline.Length, pathInside.Sum(p => p.Length) + pathOutside.Sum(p => p.Length), 9);

            // "false means the boundary was never crossed, not that the call failed."
            var clear = new GeoLine(new GeoPoint(50, 50), new GeoPoint(60, 60));
            Assert.False(Splition.TrySplitBy(clear, polygon, out GeoLine[] none, out GeoLine[] all));
            Assert.Empty(none);
            Assert.Single(all);

            // "A part running along the boundary counts as inside."
            var alongEdge = new GeoLine(new GeoPoint(2, 0), new GeoPoint(8, 0));
            Splition.TrySplitBy(alongEdge, polygon, out GeoLine[] onEdge, out GeoLine[] off);
            Assert.Single(onEdge);
            Assert.Empty(off);

            // "A path that merely touches the boundary and turns back is not a crossing."
            var graze = new GeoLine(new GeoPoint(5, 15), new GeoPoint(15, 5));
            Assert.False(Splition.TrySplitBy(graze, polygon, out GeoLine[] grazeIn, out GeoLine[] grazeOut));
            Assert.Empty(grazeIn);
            Assert.Single(grazeOut);
        }


        [Fact]
        public void Splitting_IsReachableFromTheShapeBeingCut()
        {
            var line = new GeoLine(new GeoPoint(0, 0), new GeoPoint(10, 0));
            var point = new GeoPoint(4, 0);
            var polygon = new GeoPolygon(
                new GeoPoint(2, -5), new GeoPoint(8, -5), new GeoPoint(8, 5), new GeoPoint(2, 5));
            var polyline = new GeoPolyline(new GeoPoint(0, 0), new GeoPoint(10, 0), new GeoPoint(10, 10));
            var cutter = new GeoLine(new GeoPoint(6, -5), new GeoPoint(6, 5));

            Assert.True(line.TrySplitBy(point, out GeoLine first, out GeoLine second));
            Assert.True(line.TrySplitAtDistance(4.0, out first, out second));
            Assert.True(line.TrySplitBy(polygon, out GeoLine[] inside, out GeoLine[] outside));
            GeoLine[] pieces = line.SplitAtDistances(new[] { 2.0, 5.0, 8.0 });

            Assert.True(polyline.TrySplitBy(cutter, out GeoPolyline[] parts));
            GeoPolyline head = parts[0];
            GeoPolyline tail = parts[1];

            // Each instance call is the static one with the receiver moved to the front.
            Splition.TrySplitBy(line, point, out GeoLine sFirst, out GeoLine sSecond);
            Assert.Equal(sFirst, first);
            Assert.Equal(sSecond, second);

            Assert.Single(inside);
            Assert.Equal(2, outside.Length);
            Assert.Equal(4, pieces.Length);
            Assert.Equal(2, parts.Length);
            Assert.True(head[0].IsEqualTo(polyline[0]));
            Assert.True(tail[tail.VertexCount - 1].IsEqualTo(polyline[polyline.VertexCount - 1]));
        }

    }
}
