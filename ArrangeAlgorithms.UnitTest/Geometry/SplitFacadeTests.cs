using System.Linq;
using ArrangeAlgorithms.Geometry;
using ArrangeAlgorithms.Core;
using Xunit;

namespace ArrangeAlgorithms.UnitTest.Geometry
{
    /// <summary>
    /// The instance methods on GeoLine and GeoPolyline that forward to <see cref="Splition"/>.
    /// <para>
    /// A forwarding method has its own failure mode: it compiles and returns something plausible while
    /// handing the operation the wrong arguments — the two out parameters swapped, or Tolerance.Global
    /// substituted for the tolerance the caller passed. Neither shows up in a test that only checks the
    /// result looks reasonable, so every case here pins the instance call against the static one, and
    /// every tolerance overload is given a tolerance that changes the answer.
    /// </para>
    /// </summary>
    public class SplitFacadeTests
    {
        private static GeoLine Segment() => new GeoLine(new GeoPoint(0, 0), new GeoPoint(10, 0));

        private static GeoPolyline Path() => new GeoPolyline(
            new GeoPoint(0, 0), new GeoPoint(10, 0), new GeoPoint(10, 10));

        private static GeoPolygon Square() => new GeoPolygon(
            new GeoPoint(0, 0), new GeoPoint(10, 0), new GeoPoint(10, 10), new GeoPoint(0, 10));

        // Wide enough that a position 0.4 from an endpoint stops counting as a split.
        private static Tolerance Wide() => new Tolerance(0.5, 0.5);

        #region GeoLine

        [Fact]
        public void Line_SplitByPoint_MatchesTheStaticCall()
        {
            var point = new GeoPoint(4, 0);

            Assert.True(Segment().TrySplitBy(point, out GeoLine a1, out GeoLine a2));
            Assert.True(Splition.TrySplitBy(Segment(), point, out GeoLine b1, out GeoLine b2));

            Assert.Equal(b1, a1);
            Assert.Equal(b2, a2);

            // The piece holding the start point comes first; swapping the out parameters would pass a
            // length check but fail this.
            Assert.True(a1.StartPoint.IsEqualTo(new GeoPoint(0, 0)));
            Assert.True(a2.EndPoint.IsEqualTo(new GeoPoint(10, 0)));
        }

        [Fact]
        public void Line_SplitByPoint_HonoursTheSuppliedTolerance()
        {
            var nearTheEnd = new GeoPoint(0.4, 0);

            Assert.True(Segment().TrySplitBy(nearTheEnd, out _, out _));
            Assert.False(Segment().TrySplitBy(nearTheEnd, out _, out _, Wide()));
        }

        [Fact]
        public void Line_SplitByLine_MatchesTheStaticCall()
        {
            var cutter = new GeoLine(new GeoPoint(6, -5), new GeoPoint(6, 5));

            Assert.True(Segment().TrySplitBy(cutter, out GeoLine a1, out GeoLine a2));
            Assert.True(Splition.TrySplitBy(Segment(), cutter, out GeoLine b1, out GeoLine b2));

            Assert.Equal(b1, a1);
            Assert.Equal(b2, a2);
            Assert.True(a1.EndPoint.IsEqualTo(new GeoPoint(6, 0)));
        }

        [Fact]
        public void Line_SplitByLine_HonoursTheSuppliedTolerance()
        {
            // Crossing 0.4 from the end: a split under the default tolerance, none under a wide one.
            var cutter = new GeoLine(new GeoPoint(9.6, -5), new GeoPoint(9.6, 5));

            Assert.True(Segment().TrySplitBy(cutter, out _, out _));
            Assert.False(Segment().TrySplitBy(cutter, out _, out _, Wide()));
        }

        [Fact]
        public void Line_SplitByPolygon_MatchesTheStaticCall()
        {
            var line = new GeoLine(new GeoPoint(-5, 5), new GeoPoint(15, 5));

            Assert.True(line.TrySplitBy(Square(), out GeoLine[] aIn, out GeoLine[] aOut));
            Assert.True(Splition.TrySplitBy(line, Square(), out GeoLine[] bIn, out GeoLine[] bOut));

            Assert.Equal(bIn, aIn);
            Assert.Equal(bOut, aOut);

            // Inside and outside must not be handed over the wrong way round.
            Assert.Single(aIn);
            Assert.Equal(10.0, aIn[0].Length, 9);
            Assert.Equal(2, aOut.Length);
        }

        [Fact]
        public void Line_SplitByPolygon_HonoursTheSuppliedTolerance()
        {
            var line = new GeoLine(new GeoPoint(-5, 5), new GeoPoint(15, 5));

            Assert.True(line.TrySplitBy(Square(), out GeoLine[] _, out GeoLine[] _, new Tolerance(1E-4, 1E-4)));

            // A tolerance wider than the whole crossing swallows both cut positions.
            Assert.False(line.TrySplitBy(Square(), out GeoLine[] wideIn, out GeoLine[] wideOut, new Tolerance(30.0, 30.0)));
            Assert.Equal(1, wideIn.Length + wideOut.Length);
        }

        [Fact]
        public void Line_SplitAtDistance_MatchesTheStaticCall()
        {
            Assert.True(Segment().TrySplitAtDistance(4.0, out GeoLine a1, out GeoLine a2));
            Assert.True(Splition.TrySplitAtDistance(Segment(), 4.0, out GeoLine b1, out GeoLine b2));

            Assert.Equal(b1, a1);
            Assert.Equal(b2, a2);
            Assert.Equal(4.0, a1.Length, 9);
            Assert.Equal(6.0, a2.Length, 9);
        }

        [Fact]
        public void Line_SplitAtDistance_HonoursTheSuppliedTolerance()
        {
            Assert.True(Segment().TrySplitAtDistance(0.4, out _, out _));
            Assert.False(Segment().TrySplitAtDistance(0.4, out _, out _, Wide()));
        }

        [Fact]
        public void Line_SplitAtDistances_MatchesTheStaticCall()
        {
            var cuts = new[] { 2.0, 5.0, 8.0 };

            GeoLine[] viaInstance = Segment().SplitAtDistances(cuts);
            GeoLine[] viaStatic = Splition.SplitAtDistances(Segment(), cuts);

            Assert.Equal(viaStatic, viaInstance);
            Assert.Equal(4, viaInstance.Length);
            Assert.True(viaInstance[0].StartPoint.IsEqualTo(new GeoPoint(0, 0)));
        }

        [Fact]
        public void Line_SplitAtDistances_HonoursTheSuppliedTolerance()
        {
            var cuts = new[] { 4.0, 4.2 };

            // The default tolerance keeps both positions; a wide one merges them.
            Assert.Equal(3, Segment().SplitAtDistances(cuts).Length);
            Assert.Equal(2, Segment().SplitAtDistances(cuts, Wide()).Length);
        }

        #endregion

        #region GeoPolyline

        [Fact]
        public void Polyline_SplitByPoint_MatchesTheStaticCall()
        {
            var point = new GeoPoint(10, 4);

            Assert.True(Path().TrySplitBy(point, out GeoPolyline a1, out GeoPolyline a2));
            Assert.True(Splition.TrySplitBy(Path(), point, out GeoPolyline b1, out GeoPolyline b2));

            Assert.Equal(b1, a1);
            Assert.Equal(b2, a2);
            Assert.True(a1[0].IsEqualTo(new GeoPoint(0, 0)));
            Assert.True(a2[a2.VertexCount - 1].IsEqualTo(new GeoPoint(10, 10)));
        }

        [Fact]
        public void Polyline_SplitByPoint_HonoursTheSuppliedTolerance()
        {
            var nearTheStart = new GeoPoint(0.4, 0);

            Assert.True(Path().TrySplitBy(nearTheStart, out GeoPolyline _, out GeoPolyline _));
            Assert.False(Path().TrySplitBy(nearTheStart, out GeoPolyline _, out GeoPolyline _, Wide()));
        }

        [Fact]
        public void Polyline_SplitByLine_MatchesTheStaticCall()
        {
            var cutter = new GeoLine(new GeoPoint(4, -5), new GeoPoint(4, 5));

            Assert.True(Path().TrySplitBy(cutter, out GeoPolyline[] aPieces));
            Assert.True(Splition.TrySplitBy(Path(), cutter, out GeoPolyline[] bPieces));

            Assert.Equal(bPieces, aPieces);

            Assert.True(Path().TrySplitBy(cutter, out GeoPolyline[] viaInstance));
            Assert.True(Splition.TrySplitBy(Path(), cutter, out GeoPolyline[] viaStatic));
            Assert.Equal(viaStatic, viaInstance);
            Assert.Equal(2, viaInstance.Length);
        }

        [Fact]
        public void Polyline_SplitByLine_HonoursTheSuppliedTolerance()
        {
            var cutter = new GeoLine(new GeoPoint(0.4, -5), new GeoPoint(0.4, 5));

            Assert.True(Path().TrySplitBy(cutter, out GeoPolyline[] _));
            Assert.False(Path().TrySplitBy(cutter, out GeoPolyline[] _, Wide()));

            Assert.True(Path().TrySplitBy(cutter, out GeoPolyline[] pieces1));
            Assert.Equal(2, pieces1.Length);
            Assert.False(Path().TrySplitBy(cutter, out GeoPolyline[] pieces2, Wide()));
            Assert.Single(pieces2);
        }

        [Fact]
        public void Polyline_SplitByPolygon_MatchesTheStaticCall()
        {
            var path = new GeoPolyline(new GeoPoint(-5, 5), new GeoPoint(5, 5), new GeoPoint(5, 15));

            Assert.True(path.TrySplitBy(Square(), out GeoPolyline[] aIn, out GeoPolyline[] aOut));
            Assert.True(Splition.TrySplitBy(path, Square(), out GeoPolyline[] bIn, out GeoPolyline[] bOut));

            Assert.Equal(bIn, aIn);
            Assert.Equal(bOut, aOut);
            Assert.Equal(path.Length, aIn.Sum(p => p.Length) + aOut.Sum(p => p.Length), 9);
        }

        [Fact]
        public void Polyline_SplitByPolygon_HonoursTheSuppliedTolerance()
        {
            var path = new GeoPolyline(new GeoPoint(-5, 5), new GeoPoint(15, 5));

            Assert.True(path.TrySplitBy(Square(), out GeoPolyline[] _, out GeoPolyline[] _, new Tolerance(1E-4, 1E-4)));
            Assert.False(path.TrySplitBy(Square(), out GeoPolyline[] wideIn, out GeoPolyline[] wideOut, new Tolerance(30.0, 30.0)));
            Assert.Equal(1, wideIn.Length + wideOut.Length);
        }

        [Fact]
        public void Polyline_SplitAtDistance_MatchesTheStaticCall()
        {
            Assert.True(Path().TrySplitAtDistance(15.0, out GeoPolyline a1, out GeoPolyline a2));
            Assert.True(Splition.TrySplitAtDistance(Path(), 15.0, out GeoPolyline b1, out GeoPolyline b2));

            Assert.Equal(b1, a1);
            Assert.Equal(b2, a2);
            Assert.Equal(15.0, a1.Length, 9);
            Assert.Equal(5.0, a2.Length, 9);
        }

        [Fact]
        public void Polyline_SplitAtDistance_HonoursTheSuppliedTolerance()
        {
            Assert.True(Path().TrySplitAtDistance(0.4, out GeoPolyline _, out GeoPolyline _));
            Assert.False(Path().TrySplitAtDistance(0.4, out GeoPolyline _, out GeoPolyline _, Wide()));
        }

        [Fact]
        public void Polyline_SplitAtDistances_MatchesTheStaticCall()
        {
            var cuts = new[] { 5.0, 15.0 };

            GeoPolyline[] viaInstance = Path().SplitAtDistances(cuts);
            Assert.Equal(Splition.SplitAtDistances(Path(), cuts), viaInstance);
            Assert.Equal(3, viaInstance.Length);
            Assert.Equal(Path().Length, viaInstance.Sum(p => p.Length), 9);
        }

        [Fact]
        public void Polyline_SplitAtDistances_HonoursTheSuppliedTolerance()
        {
            var cuts = new[] { 5.0, 5.2 };

            Assert.Equal(3, Path().SplitAtDistances(cuts).Length);
            Assert.Equal(2, Path().SplitAtDistances(cuts, Wide()).Length);
        }

        #endregion
    }
}
