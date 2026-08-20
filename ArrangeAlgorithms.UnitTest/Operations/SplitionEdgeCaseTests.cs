using System;
using System.Collections.Generic;
using System.Linq;
using ArrangeAlgorithms.Geometry;
using ArrangeAlgorithms.Core;
using Xunit;

namespace ArrangeAlgorithms.UnitTest.Operations
{
    /// <summary>
    /// The configurations the ordinary cases do not reach: cuts landing exactly on a vertex through each
    /// of the three routes that can produce one, subjects at the smallest shape a polyline can take,
    /// redundant and reversing vertices, and coordinates far from the origin.
    /// </summary>
    public class SplitionEdgeCaseTests
    {
        private static readonly Tolerance Tol = new Tolerance(1E-4, 1E-4);

        // An L: 10 along X, then 10 up. The corner sits at (10, 0), arc length 10.
        private static GeoPolyline Corner() => new GeoPolyline(
            new GeoPoint(0, 0), new GeoPoint(10, 0), new GeoPoint(10, 10));

        private static GeoPolygon Square() => new GeoPolygon(
            new GeoPoint(0, 0), new GeoPoint(10, 0), new GeoPoint(10, 10), new GeoPoint(0, 10));

        #region A cut landing exactly on a vertex, by each route that can produce one

        [Fact]
        public void Vertex_ByPoint_SplitsWithoutDuplicatingIt()
        {
            // Splitting by distance at a corner is covered elsewhere. This is the other route: the point
            // is handed in directly, and its arc length has to be resolved from geometry, where the two
            // edges meeting at the corner are both at distance zero from it.
            Assert.True(Splition.TrySplitBy(Corner(), new GeoPoint(10, 0), out GeoPolyline first, out GeoPolyline second, Tol));

            Assert.Equal(2, first.VertexCount);
            Assert.True(first[0].IsEqualTo(new GeoPoint(0, 0)));
            Assert.True(first[1].IsEqualTo(new GeoPoint(10, 0)));

            Assert.Equal(2, second.VertexCount);
            Assert.True(second[0].IsEqualTo(new GeoPoint(10, 0)));
            Assert.True(second[1].IsEqualTo(new GeoPoint(10, 10)));

            Assert.Equal(Corner().Length, first.Length + second.Length, 9);
        }

        [Fact]
        public void Vertex_ByCuttingLine_SplitsOnce()
        {
            // The cutter passes through the corner, so both edges meeting there report an intersection at
            // the same place. Two cuts at one position would leave an empty piece between them.
            var cutter = new GeoLine(new GeoPoint(5, -5), new GeoPoint(15, 5));

            Assert.True(Splition.TrySplitBy(Corner(), cutter, out GeoPolyline[] pieces, Tol));

            Assert.Equal(2, pieces.Length);
            Assert.True(pieces[0][pieces[0].VertexCount - 1].IsEqualTo(new GeoPoint(10, 0)));
            Assert.True(pieces[1][0].IsEqualTo(new GeoPoint(10, 0)));
            Assert.Equal(Corner().Length, pieces.Sum(p => p.Length), 9);
        }

        [Fact]
        public void Vertex_ByPolygonBoundary_ClassifiesBothSides()
        {
            // The square's own corner (10, 0) is also a vertex of the subject, so the crossing lands on a
            // vertex of both shapes at once.
            var path = new GeoPolyline(new GeoPoint(5, -5), new GeoPoint(10, 0), new GeoPoint(5, 5));

            Assert.True(Splition.TrySplitBy(path, Square(), out GeoPolyline[] inside, out GeoPolyline[] outside, Tol));

            Assert.Equal(path.Length, inside.Sum(p => p.Length) + outside.Sum(p => p.Length), 9);
            Assert.NotEmpty(inside);
            Assert.NotEmpty(outside);
        }

        [Fact]
        public void Vertex_OfThePolygonLyingOnTheSubject_IsHandled()
        {
            // The reverse arrangement: a corner of the polygon rests on the subject rather than the other
            // way round. The line runs along y = 0 straight through the square's lower corners.
            var line = new GeoLine(new GeoPoint(-5, 0), new GeoPoint(15, 0));

            Splition.TrySplitBy(line, Square(), out GeoLine[] inside, out GeoLine[] outside, Tol);

            Assert.Equal(line.Length, inside.Sum(p => p.Length) + outside.Sum(p => p.Length), 9);

            // The middle stretch lies along the bottom edge, which counts as inside.
            Assert.Contains(inside, p => p.MidPoint.IsEqualTo(new GeoPoint(5, 0), Tol));
        }

        [Fact]
        public void Vertex_APointJustShortOfOne_SnapsOntoIt()
        {
            // 0.2 from the corner under a tolerance of 0.5. Cutting where asked would leave a 0.2 long
            // edge behind, shorter than the tolerance that is supposed to rule such things out.
            var wide = new Tolerance(0.5, 0.5);

            Assert.True(Splition.TrySplitBy(Corner(), new GeoPoint(9.8, 0), out GeoPolyline first, out GeoPolyline second, wide));

            Assert.Equal(2, first.VertexCount);
            Assert.True(first[1].IsEqualTo(new GeoPoint(10, 0)));

            // Two vertices, not three: no sliver edge from the corner back to the requested position.
            Assert.Equal(2, second.VertexCount);
            Assert.True(second[0].IsEqualTo(new GeoPoint(10, 0)));

            for (int e = 0; e < second.EdgeCount; e++)
            {
                Assert.True(second.GetEdgeAt(e).Length > wide.EqualPoint);
            }
        }

        [Fact]
        public void Vertex_ACutterCrossingJustShortOfOne_SnapsOntoIt()
        {
            // Same near miss, reached through the cutter route rather than the point route.
            var wide = new Tolerance(0.5, 0.5);
            var cutter = new GeoLine(new GeoPoint(9.8, -5), new GeoPoint(9.8, 5));

            Assert.True(Splition.TrySplitBy(Corner(), cutter, out GeoPolyline[] pieces, wide));

            Assert.Equal(2, pieces.Length);
            Assert.True(pieces[0][pieces[0].VertexCount - 1].IsEqualTo(new GeoPoint(10, 0)));
            Assert.Equal(2, pieces[1].VertexCount);

            foreach (GeoPolyline piece in pieces)
            {
                for (int e = 0; e < piece.EdgeCount; e++)
                {
                    Assert.True(piece.GetEdgeAt(e).Length > wide.EqualPoint);
                }
            }
        }

        [Fact]
        public void Vertex_ACutterCrossingAtTheVeryFirstVertex_IsNotASplit()
        {
            // A crossing at an endpoint cuts nothing, whichever route produced it.
            var cutter = new GeoLine(new GeoPoint(0, -5), new GeoPoint(0, 5));

            Assert.False(Splition.TrySplitBy(Corner(), cutter, out GeoPolyline[] pieces, Tol));

            Assert.Null(pieces);
        }

        #endregion

        #region Subject shapes the ordinary cases do not reach

        [Fact]
        public void SmallestPolyline_TwoVertices_SupportsEveryOperation()
        {
            // Runs clear across the square at y = 5, entering at x = 0 and leaving at x = 10.
            var path = new GeoPolyline(new GeoPoint(-5, 5), new GeoPoint(15, 5));
            var cutter = new GeoLine(new GeoPoint(6, 0), new GeoPoint(6, 10));

            Assert.Equal(20.0, path.Length, 9);

            Assert.True(Splition.TrySplitAtDistance(path, 4.0, out GeoPolyline a1, out GeoPolyline a2, Tol));
            Assert.Equal(2, a1.VertexCount);
            Assert.Equal(2, a2.VertexCount);

            Assert.True(Splition.TrySplitBy(path, new GeoPoint(-1, 5), out GeoPolyline _, out GeoPolyline _, Tol));
            Assert.True(Splition.TrySplitBy(path, cutter, out GeoPolyline[] pathPieces, Tol));
            Assert.Equal(2, pathPieces.Length);
            Assert.Equal(3, Splition.SplitAtDistances(path, new[] { 3.0, 7.0 }, Tol).Length);

            Assert.True(Splition.TrySplitBy(path, Square(), out GeoPolyline[] inside, out GeoPolyline[] outside, Tol));
            Assert.Single(inside);
            Assert.Equal(10.0, inside[0].Length, 9);
            Assert.Equal(path.Length, inside.Sum(p => p.Length) + outside.Sum(p => p.Length), 9);
        }

        [Fact]
        public void PolylineLyingAlongAPolygonEdge_CountsAsInside()
        {
            // The whole path runs on top of the square's bottom edge, then turns up its right edge: never
            // strictly inside, never strictly outside, on the boundary throughout.
            var path = new GeoPolyline(new GeoPoint(0, 0), new GeoPoint(10, 0), new GeoPoint(10, 10));

            Assert.False(Splition.TrySplitBy(path, Square(), out GeoPolyline[] inside, out GeoPolyline[] outside, Tol));

            Assert.Single(inside);
            Assert.Equal(path.VertexCount, inside[0].VertexCount);
            Assert.Empty(outside);
            Assert.Equal(path.Length, inside.Sum(p => p.Length), 9);
        }

        [Fact]
        public void PathLeavingTheBoundaryPartWayAlong_SplitsWhereItLeaves()
        {
            // Half on the bottom edge, then dropping away below it.
            var path = new GeoPolyline(new GeoPoint(0, 0), new GeoPoint(5, 0), new GeoPoint(5, -5));

            Splition.TrySplitBy(path, Square(), out GeoPolyline[] inside, out GeoPolyline[] outside, Tol);

            GeoLine[] insideEdges = ToEdges(inside);
            GeoLine[] outsideEdges = ToEdges(outside);

            Assert.Equal(path.Length, insideEdges.Sum(p => p.Length) + outsideEdges.Sum(p => p.Length), 9);
            Assert.Contains(insideEdges, p => p.MidPoint.IsEqualTo(new GeoPoint(2.5, 0), Tol));
            Assert.Contains(outsideEdges, p => p.MidPoint.IsEqualTo(new GeoPoint(5, -2.5), Tol));
        }

        [Fact]
        public void CollinearVertices_AreKeptAndCutNormally()
        {
            // Three points on one straight line. The middle one carries no bend, but it is a real vertex
            // and the pieces have to keep it.
            var path = new GeoPolyline(new GeoPoint(0, 0), new GeoPoint(5, 0), new GeoPoint(10, 0));

            Assert.Equal(2, path.EdgeCount);
            Assert.Equal(10.0, path.Length, 9);

            Assert.True(Splition.TrySplitAtDistance(path, 7.0, out GeoPolyline first, out GeoPolyline second, Tol));

            Assert.Equal(3, first.VertexCount);
            Assert.True(first[1].IsEqualTo(new GeoPoint(5, 0)));
            Assert.True(first[2].IsEqualTo(new GeoPoint(7, 0)));
            Assert.Equal(2, second.VertexCount);
            Assert.Equal(10.0, first.Length + second.Length, 9);
        }

        [Fact]
        public void ReversingSpike_ResolvesTheAmbiguityByTakingTheEarlierEdge()
        {
            // The path runs out and comes straight back, so (5, 0) sits on it twice. Splitting by that
            // point cannot mean both, and the arc length is resolved on the first edge that reaches it.
            var spike = new GeoPolyline(new GeoPoint(0, 0), new GeoPoint(10, 0), new GeoPoint(0, 0));

            Assert.Equal(20.0, spike.Length, 9);
            Assert.True(Splition.TrySplitBy(spike, new GeoPoint(5, 0), out GeoPolyline first, out GeoPolyline second, Tol));

            // The outbound leg wins, so the cut is at arc length 5 rather than 15.
            Assert.Equal(5.0, first.Length, 9);
            Assert.Equal(15.0, second.Length, 9);

            // Splitting at the far arc length reaches the return leg, which the point form cannot.
            Assert.True(Splition.TrySplitAtDistance(spike, 15.0, out GeoPolyline farFirst, out GeoPolyline farSecond, Tol));
            Assert.Equal(15.0, farFirst.Length, 9);
            Assert.Equal(5.0, farSecond.Length, 9);
        }

        #endregion

        #region Polygon boundary meeting the subject at its ends

        [Fact]
        public void Polygon_SubjectStartingOnTheBoundary_DropsTheCutThere()
        {
            // The start point sits on the left edge of the square, so a crossing is reported at arc
            // length zero, where there is nothing to cut.
            var line = new GeoLine(new GeoPoint(0, 5), new GeoPoint(15, 5));

            Assert.True(Splition.TrySplitBy(line, Square(), out GeoLine[] inside, out GeoLine[] outside, Tol));

            Assert.Single(inside);
            Assert.Single(outside);
            Assert.Equal(10.0, inside[0].Length, 9);
            Assert.Equal(5.0, outside[0].Length, 9);
        }

        [Fact]
        public void Polygon_SubjectRunningThroughTwoCorners_SplitsAtBoth()
        {
            // The diagonal enters at (0, 0) and leaves at (10, 10), both corners of the square.
            var line = new GeoLine(new GeoPoint(-5, -5), new GeoPoint(15, 15));

            Assert.True(Splition.TrySplitBy(line, Square(), out GeoLine[] inside, out GeoLine[] outside, Tol));

            Assert.Single(inside);
            Assert.Equal(2, outside.Length);
            Assert.Equal(Math.Sqrt(200.0), inside[0].Length, 9);
            Assert.Equal(line.Length, inside.Sum(p => p.Length) + outside.Sum(p => p.Length), 9);
        }

        [Fact]
        public void Polygon_SplittingIsSymmetricUnderReversal()
        {
            var random = new Random(11);

            for (int c = 0; c < 200; c++)
            {
                var line = new GeoLine(
                    new GeoPoint(random.NextDouble() * 30 - 10, random.NextDouble() * 30 - 10),
                    new GeoPoint(random.NextDouble() * 30 - 10, random.NextDouble() * 30 - 10));
                var flipped = new GeoLine(line.EndPoint, line.StartPoint);

                bool a = Splition.TrySplitBy(line, Square(), out GeoLine[] inA, out GeoLine[] outA, Tol);
                bool b = Splition.TrySplitBy(flipped, Square(), out GeoLine[] inB, out GeoLine[] outB, Tol);

                Assert.Equal(a, b);
                Assert.Equal(inA.Length, inB.Length);
                Assert.Equal(outA.Length, outB.Length);
                Assert.Equal(inA.Sum(p => p.Length), inB.Sum(p => p.Length), 6);
                Assert.Equal(outA.Sum(p => p.Length), outB.Sum(p => p.Length), 6);
            }
        }

        #endregion

        #region Numbers far from the origin

        [Fact]
        public void FarFromTheOrigin_SplitsJustAsAccurately()
        {
            // Survey coordinates: the interesting digits sit far down the mantissa.
            const double ox = 1_500_000.0;
            const double oy = 4_200_000.0;

            var line = new GeoLine(new GeoPoint(ox, oy), new GeoPoint(ox + 100.0, oy));

            Assert.True(Splition.TrySplitAtDistance(line, 40.0, out GeoLine first, out GeoLine second, Tol));
            Assert.Equal(40.0, first.Length, 6);
            Assert.Equal(60.0, second.Length, 6);
            Assert.True(first.EndPoint.IsEqualTo(new GeoPoint(ox + 40.0, oy), Tol));

            var path = new GeoPolyline(
                new GeoPoint(ox, oy), new GeoPoint(ox + 100.0, oy), new GeoPoint(ox + 100.0, oy + 100.0));
            var cutter = new GeoLine(new GeoPoint(ox + 60.0, oy - 50.0), new GeoPoint(ox + 60.0, oy + 50.0));

            Assert.True(Splition.TrySplitBy(path, cutter, out GeoPolyline[] pieces, Tol));
            Assert.Equal(2, pieces.Length);
            Assert.Equal(path.Length, pieces.Sum(p => p.Length), 6);
        }

        [Fact]
        public void SubjectOnlyAFewTolerancesLong_StillBehaves()
        {
            // Barely longer than the tolerance: every position is within a tolerance of an end, so there
            // is nowhere left to cut.
            double tiny = Tol.EqualPoint * 3.0;
            var line = new GeoLine(new GeoPoint(0, 0), new GeoPoint(tiny, 0));

            Assert.True(Splition.TrySplitAtDistance(line, tiny * 0.5, out GeoLine a, out GeoLine b, Tol));
            Assert.Equal(tiny, a.Length + b.Length, 12);

            var shorter = new GeoLine(new GeoPoint(0, 0), new GeoPoint(Tol.EqualPoint * 1.5, 0));
            Assert.False(Splition.TrySplitAtDistance(shorter, Tol.EqualPoint * 0.75, out _, out _, Tol));
        }

        private static GeoLine[] ToEdges(GeoPolyline[] polylines)
        {
            var edges = new List<GeoLine>();
            foreach (var polyline in polylines)
            {
                for (int e = 0; e < polyline.EdgeCount; e++)
                {
                    edges.Add(polyline.GetEdgeAt(e));
                }
            }
            return edges.ToArray();
        }

        #endregion
    }
}
