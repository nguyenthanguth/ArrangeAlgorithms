using System;
using System.Linq;
using ArrangeAlgorithms.Geometry;
using ArrangeAlgorithms.Core;
using Xunit;

namespace ArrangeAlgorithms.UnitTest.Core
{
    public class SplitionTests
    {
        // A 10 unit segment along X from the origin.
        private static GeoLine Segment() => new GeoLine(new GeoPoint(0, 0), new GeoPoint(10, 0));

        // An L of three vertices: 10 along X, then 10 up. Total length 20, corner at distance 10.
        private static GeoPolyline OpenPath() => new GeoPolyline(
            new GeoPoint(0, 0), new GeoPoint(10, 0), new GeoPoint(10, 10));

        #region Line - split at a distance

        [Fact]
        public void Line_SplitAtDistance_CutsWhereAsked()
        {
            Assert.True(Splition.TrySplitAtDistance(Segment(), 4.0, out GeoLine first, out GeoLine second));

            Assert.True(first.StartPoint.IsEqualTo(new GeoPoint(0, 0)));
            Assert.True(first.EndPoint.IsEqualTo(new GeoPoint(4, 0)));
            Assert.True(second.StartPoint.IsEqualTo(new GeoPoint(4, 0)));
            Assert.True(second.EndPoint.IsEqualTo(new GeoPoint(10, 0)));
        }

        [Theory]
        [InlineData(0.0)]      // on the start point
        [InlineData(10.0)]     // on the end point
        [InlineData(-1.0)]     // before the start
        [InlineData(11.0)]     // past the end
        [InlineData(double.NaN)]
        public void Line_SplitAtDistance_RefusesPositionsThatCutNothing(double distance)
        {
            Assert.False(Splition.TrySplitAtDistance(Segment(), distance, out _, out _));
        }

        [Fact]
        public void Line_SplitAtDistance_RefusesPositionsWithinToleranceOfAnEnd()
        {
            var tolerance = new Tolerance(0.5, 0.5);

            Assert.False(Splition.TrySplitAtDistance(Segment(), 0.4, out _, out _, tolerance));
            Assert.False(Splition.TrySplitAtDistance(Segment(), 9.6, out _, out _, tolerance));
            Assert.True(Splition.TrySplitAtDistance(Segment(), 0.6, out _, out _, tolerance));
        }

        #endregion

        #region Line - split by a point

        [Fact]
        public void Line_SplitByPoint_CutsAtThePoint()
        {
            Assert.True(Splition.TrySplitBy(Segment(), new GeoPoint(7, 0), out GeoLine first, out GeoLine second));

            Assert.True(first.EndPoint.IsEqualTo(new GeoPoint(7, 0)));
            Assert.True(second.StartPoint.IsEqualTo(new GeoPoint(7, 0)));
        }

        [Fact]
        public void Line_SplitByPoint_RefusesAPointOffTheSegment()
        {
            // Off to the side, and beyond the far end: neither is on the segment, and neither is
            // projected onto it.
            Assert.False(Splition.TrySplitBy(Segment(), new GeoPoint(5, 3), out _, out _));
            Assert.False(Splition.TrySplitBy(Segment(), new GeoPoint(20, 0), out _, out _));
        }

        [Fact]
        public void Line_SplitByPoint_RefusesTheEndpoints()
        {
            Assert.False(Splition.TrySplitBy(Segment(), new GeoPoint(0, 0), out _, out _));
            Assert.False(Splition.TrySplitBy(Segment(), new GeoPoint(10, 0), out _, out _));
        }

        #endregion

        #region Line - split by a line

        [Fact]
        public void Line_SplitByLine_CutsWhereTheyCross()
        {
            var cutter = new GeoLine(new GeoPoint(6, -5), new GeoPoint(6, 5));

            Assert.True(Splition.TrySplitBy(Segment(), cutter, out GeoLine first, out GeoLine second));

            Assert.True(first.EndPoint.IsEqualTo(new GeoPoint(6, 0)));
            Assert.True(second.StartPoint.IsEqualTo(new GeoPoint(6, 0)));
        }

        [Fact]
        public void Line_SplitByLine_CutsAtATJunction()
        {
            // The cutter only touches; it does not pass through.
            var cutter = new GeoLine(new GeoPoint(3, 0), new GeoPoint(3, 8));

            Assert.True(Splition.TrySplitBy(Segment(), cutter, out GeoLine first, out _));
            Assert.True(first.EndPoint.IsEqualTo(new GeoPoint(3, 0)));
        }

        [Fact]
        public void Line_SplitByLine_RefusesWhenTheyMiss()
        {
            var cutter = new GeoLine(new GeoPoint(6, 2), new GeoPoint(6, 5));

            Assert.False(Splition.TrySplitBy(Segment(), cutter, out _, out _));
        }

        [Fact]
        public void Line_SplitByLine_RefusesParallelAndCollinearCutters()
        {
            var parallel = new GeoLine(new GeoPoint(0, 3), new GeoPoint(10, 3));
            var collinear = new GeoLine(new GeoPoint(3, 0), new GeoPoint(8, 0));

            Assert.False(Splition.TrySplitBy(Segment(), parallel, out _, out _));

            // A collinear overlap has no single position to cut at, so it is treated as no cut.
            Assert.False(Splition.TrySplitBy(Segment(), collinear, out _, out _));
        }

        [Fact]
        public void Line_SplitByLine_RefusesACrossingAtAnEndpoint()
        {
            var cutter = new GeoLine(new GeoPoint(10, -5), new GeoPoint(10, 5));

            Assert.False(Splition.TrySplitBy(Segment(), cutter, out _, out _));
        }

        #endregion

        #region Line - split at several distances

        [Fact]
        public void Line_SplitAtDistances_ProducesOnePieceMoreThanCuts()
        {
            GeoLine[] pieces = Splition.SplitAtDistances(Segment(), new[] { 2.0, 5.0, 8.0 });

            Assert.Equal(4, pieces.Length);
            Assert.True(pieces[0].StartPoint.IsEqualTo(new GeoPoint(0, 0)));
            Assert.True(pieces[1].StartPoint.IsEqualTo(new GeoPoint(2, 0)));
            Assert.True(pieces[2].StartPoint.IsEqualTo(new GeoPoint(5, 0)));
            Assert.True(pieces[3].EndPoint.IsEqualTo(new GeoPoint(10, 0)));
        }

        [Fact]
        public void Line_SplitAtDistances_SortsAndDropsUnusablePositions()
        {
            // Unsorted, with one duplicate, one at the start, and two outside the segment.
            GeoLine[] pieces = Splition.SplitAtDistances(
                Segment(), new[] { 8.0, -3.0, 2.0, 8.0, 0.0, 40.0 });

            Assert.Equal(3, pieces.Length);
            Assert.True(pieces[0].EndPoint.IsEqualTo(new GeoPoint(2, 0)));
            Assert.True(pieces[1].EndPoint.IsEqualTo(new GeoPoint(8, 0)));
        }

        [Fact]
        public void Line_SplitAtDistances_MergesPositionsCloserThanTolerance()
        {
            var tolerance = new Tolerance(0.5, 0.5);

            GeoLine[] pieces = Splition.SplitAtDistances(Segment(), new[] { 4.0, 4.2 }, tolerance);

            Assert.Equal(2, pieces.Length);
            Assert.True(pieces[0].EndPoint.IsEqualTo(new GeoPoint(4, 0)));
        }

        [Fact]
        public void Line_SplitAtDistances_ReturnsTheSubjectWhenNothingIsUsable()
        {
            GeoLine[] pieces = Splition.SplitAtDistances(Segment(), new double[0]);

            Assert.Single(pieces);
            Assert.True(pieces[0].StartPoint.IsEqualTo(new GeoPoint(0, 0)));
            Assert.True(pieces[0].EndPoint.IsEqualTo(new GeoPoint(10, 0)));
        }

        [Fact]
        public void Line_SplitAtDistances_RejectsNullPositions()
        {
            Assert.Throws<ArgumentNullException>(() => Splition.SplitAtDistances(Segment(), null));
        }

        #endregion

        #region Polyline - split at a distance

        [Fact]
        public void Polyline_SplitAtDistance_CutsInsideAnEdge()
        {
            Assert.True(Splition.TrySplitAtDistance(OpenPath(), 5.0, out GeoPolyline first, out GeoPolyline second));

            // The first piece stops halfway along the horizontal leg.
            Assert.Equal(2, first.VertexCount);
            Assert.True(first[1].IsEqualTo(new GeoPoint(5, 0)));

            // The second keeps the corner and the far end.
            Assert.Equal(3, second.VertexCount);
            Assert.True(second[0].IsEqualTo(new GeoPoint(5, 0)));
            Assert.True(second[1].IsEqualTo(new GeoPoint(10, 0)));
            Assert.True(second[2].IsEqualTo(new GeoPoint(10, 10)));
        }

        [Fact]
        public void Polyline_SplitAtDistance_CutsExactlyOnACorner()
        {
            // Distance 10 is the corner vertex itself; neither piece should gain a duplicate vertex.
            Assert.True(Splition.TrySplitAtDistance(OpenPath(), 10.0, out GeoPolyline first, out GeoPolyline second));

            Assert.Equal(2, first.VertexCount);
            Assert.True(first[0].IsEqualTo(new GeoPoint(0, 0)));
            Assert.True(first[1].IsEqualTo(new GeoPoint(10, 0)));

            Assert.Equal(2, second.VertexCount);
            Assert.True(second[0].IsEqualTo(new GeoPoint(10, 0)));
            Assert.True(second[1].IsEqualTo(new GeoPoint(10, 10)));
        }

        [Fact]
        public void Polyline_SplitAtDistance_SnapsAPositionNearACornerOntoIt()
        {
            var tolerance = new Tolerance(0.5, 0.5);

            // Just past the corner: snapping keeps the second piece from starting with a sliver edge.
            Assert.True(Splition.TrySplitAtDistance(OpenPath(), 10.2, out GeoPolyline first, out GeoPolyline second, tolerance));

            Assert.Equal(2, first.VertexCount);
            Assert.True(first[1].IsEqualTo(new GeoPoint(10, 0)));
            Assert.Equal(2, second.VertexCount);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(20.0)]
        [InlineData(-4.0)]
        [InlineData(25.0)]
        public void Polyline_SplitAtDistance_RefusesPositionsThatCutNothing(double distance)
        {
            Assert.False(Splition.TrySplitAtDistance(OpenPath(), distance, out _, out _));
        }

        [Fact]
        public void Polyline_SplitAtDistance_RejectsNullSubject()
        {
            Assert.Throws<ArgumentNullException>(
                () => Splition.TrySplitAtDistance(null, 5.0, out GeoPolyline _, out GeoPolyline _));
        }

        #endregion

        #region Polyline - split by a point

        [Fact]
        public void Polyline_SplitByPoint_CutsAtThePoint()
        {
            Assert.True(Splition.TrySplitBy(OpenPath(), new GeoPoint(10, 4), out GeoPolyline first, out GeoPolyline second));

            Assert.True(first[first.VertexCount - 1].IsEqualTo(new GeoPoint(10, 4)));
            Assert.True(second[0].IsEqualTo(new GeoPoint(10, 4)));
        }

        [Fact]
        public void Polyline_SplitByPoint_RefusesAPointOffThePath()
        {
            Assert.False(Splition.TrySplitBy(OpenPath(), new GeoPoint(4, 4), out _, out _));
        }

        [Fact]
        public void Polyline_SplitByPoint_RefusesTheEndpoints()
        {
            Assert.False(Splition.TrySplitBy(OpenPath(), new GeoPoint(0, 0), out _, out _));
            Assert.False(Splition.TrySplitBy(OpenPath(), new GeoPoint(10, 10), out _, out _));
        }

        #endregion

        #region Polyline - split by a line

        [Fact]
        public void Polyline_SplitByLine_CutsAtASingleCrossing()
        {
            var cutter = new GeoLine(new GeoPoint(4, -5), new GeoPoint(4, 5));

            Assert.True(Splition.TrySplitBy(OpenPath(), cutter, out GeoPolyline[] pieces));

            Assert.Equal(2, pieces.Length);
            Assert.True(pieces[0][pieces[0].VertexCount - 1].IsEqualTo(new GeoPoint(4, 0)));
        }

        [Fact]
        public void Polyline_SplitByLine_CutsEverywhereAZigzagCrossesIt()
        {
            // A zigzag that crosses y = 0 three times.
            var zigzag = new GeoPolyline(
                new GeoPoint(0, -4), new GeoPoint(2, 4), new GeoPoint(4, -4),
                new GeoPoint(6, 4), new GeoPoint(8, 4));
            var cutter = new GeoLine(new GeoPoint(-1, 0), new GeoPoint(9, 0));

            Assert.True(Splition.TrySplitBy(zigzag, cutter, out GeoPolyline[] pieces));

            Assert.Equal(4, pieces.Length);
        }

        [Fact]
        public void Polyline_SplitByLine_TrySplitBySupportsMoreThanOneCrossing()
        {
            var zigzag = new GeoPolyline(
                new GeoPoint(0, -4), new GeoPoint(2, 4), new GeoPoint(4, -4),
                new GeoPoint(6, 4), new GeoPoint(8, 4));
            var cutter = new GeoLine(new GeoPoint(-1, 0), new GeoPoint(9, 0));

            Assert.True(Splition.TrySplitBy(zigzag, cutter, out GeoPolyline[] pieces));
            Assert.Equal(4, pieces.Length);
        }

        [Fact]
        public void Polyline_SplitByLine_ReturnsFalseWhenNothingCrosses()
        {
            var cutter = new GeoLine(new GeoPoint(-5, -5), new GeoPoint(-5, 5));

            Assert.False(Splition.TrySplitBy(OpenPath(), cutter, out GeoPolyline[] pieces));

            // false says nothing was cut, not that the call failed, so the subject still comes back.
            Assert.Single(pieces);
            Assert.Equal(OpenPath().Length, pieces[0].Length, 9);
        }

        [Fact]
        public void Polyline_SplitByLine_RejectsNullSubject()
        {
            Assert.Throws<ArgumentNullException>(() => Splition.TrySplitBy(null, Segment(), out _));
        }

        #endregion

        #region Tolerance independence

        [Fact]
        public void Split_UsesTheSuppliedToleranceNotTheGlobalOne()
        {
            // The trusted constructor exists so that results are not re-filtered against Tolerance.Global.
            // With the global widened past the edge lengths, the public constructor would collapse the
            // pieces; splitting must not care.
            Tolerance saved = Tolerance.Global;
            try
            {
                Tolerance.Global = new Tolerance(5.0, 5.0);

                GeoPolyline[] pieces = Splition.SplitAtDistances(
                    OpenPath(), new[] { 5.0 }, new Tolerance(1E-4, 1E-4));

                Assert.Equal(2, pieces.Length);
                Assert.Equal(2, pieces[0].VertexCount);
                Assert.Equal(3, pieces[1].VertexCount);
                Assert.Equal(20.0, pieces.Sum(p => p.Length), 9);
            }
            finally
            {
                Tolerance.Global = saved;
            }
        }

        #endregion
    }
}
