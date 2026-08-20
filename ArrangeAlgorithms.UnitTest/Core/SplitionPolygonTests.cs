using System;
using System.Collections.Generic;
using System.Linq;
using ArrangeAlgorithms.Geometry;
using ArrangeAlgorithms.Core;
using Xunit;

namespace ArrangeAlgorithms.UnitTest.Core
{
    /// <summary>
    /// Splitting a line or a polyline against a polygon, which sorts the result into the parts inside the
    /// polygon and the parts outside it.
    /// </summary>
    public class SplitionPolygonTests
    {
        // A 10 by 10 square with its lower left corner at the origin.
        private static GeoPolygon Square() => new GeoPolygon(
            new GeoPoint(0, 0), new GeoPoint(10, 0), new GeoPoint(10, 10), new GeoPoint(0, 10));

        // A 20 by 20 square with a notch cut up from its bottom edge, spanning x in (5, 15) up to
        // y = 15. A horizontal probe below y = 15 therefore enters, leaves through the notch, and
        // enters again: the shape a convex-only implementation gets wrong.
        private static GeoPolygon NotchedSquare() => new GeoPolygon(
            new GeoPoint(0, 0), new GeoPoint(5, 0), new GeoPoint(5, 15), new GeoPoint(15, 15),
            new GeoPoint(15, 0), new GeoPoint(20, 0), new GeoPoint(20, 20), new GeoPoint(0, 20));

        private static double TotalLength(GeoLine[] parts) => parts.Sum(p => p.Length);

        #region Line

        [Fact]
        public void Line_CrossingRightThrough_SplitsIntoThree()
        {
            // Enters at x = 0 and leaves at x = 10.
            var line = new GeoLine(new GeoPoint(-5, 5), new GeoPoint(15, 5));

            Assert.True(Splition.TrySplitBy(line, Square(), out GeoLine[] inside, out GeoLine[] outside));

            Assert.Single(inside);
            Assert.Equal(2, outside.Length);

            Assert.True(inside[0].StartPoint.IsEqualTo(new GeoPoint(0, 5)));
            Assert.True(inside[0].EndPoint.IsEqualTo(new GeoPoint(10, 5)));
            Assert.Equal(10.0, inside[0].Length, 9);

            // Order follows the subject, so the leading tail comes first.
            Assert.True(outside[0].StartPoint.IsEqualTo(new GeoPoint(-5, 5)));
            Assert.True(outside[1].EndPoint.IsEqualTo(new GeoPoint(15, 5)));

            Assert.Equal(line.Length, TotalLength(inside) + TotalLength(outside), 9);
        }

        [Fact]
        public void Line_EndingInside_KeepsBothParts()
        {
            var line = new GeoLine(new GeoPoint(-5, 5), new GeoPoint(5, 5));

            Assert.True(Splition.TrySplitBy(line, Square(), out GeoLine[] inside, out GeoLine[] outside));

            Assert.Single(inside);
            Assert.Single(outside);
            Assert.Equal(5.0, inside[0].Length, 9);
            Assert.Equal(5.0, outside[0].Length, 9);
        }

        [Fact]
        public void Line_EntirelyInside_ReportsNoCrossing()
        {
            var line = new GeoLine(new GeoPoint(2, 5), new GeoPoint(8, 5));

            // false means the boundary never crossed, not that the call failed.
            Assert.False(Splition.TrySplitBy(line, Square(), out GeoLine[] inside, out GeoLine[] outside));

            Assert.Single(inside);
            Assert.Empty(outside);
            Assert.Equal(line.Length, inside[0].Length, 9);
        }

        [Fact]
        public void Line_EntirelyOutside_ReportsNoCrossing()
        {
            var line = new GeoLine(new GeoPoint(50, 50), new GeoPoint(60, 60));

            Assert.False(Splition.TrySplitBy(line, Square(), out GeoLine[] inside, out GeoLine[] outside));

            Assert.Empty(inside);
            Assert.Single(outside);
            Assert.Equal(line.Length, outside[0].Length, 9);
        }

        [Fact]
        public void Line_GrazingACorner_IsNotACrossing()
        {
            // Touches the corner at (10, 10) and turns away without ever entering.
            var line = new GeoLine(new GeoPoint(5, 15), new GeoPoint(15, 5));

            Assert.False(Splition.TrySplitBy(line, Square(), out GeoLine[] inside, out GeoLine[] outside));

            // The touch is an intersection point, but both sides of it are outside, so no cut survives
            // and the subject comes back whole rather than as two collinear halves.
            Assert.Empty(inside);
            Assert.Single(outside);
            Assert.Equal(line.Length, outside[0].Length, 9);
        }

        [Fact]
        public void Line_AlongTheBoundary_CountsAsInside()
        {
            // Lying on the bottom edge. Contains treats the boundary as part of the polygon.
            var line = new GeoLine(new GeoPoint(2, 0), new GeoPoint(8, 0));

            Splition.TrySplitBy(line, Square(), out GeoLine[] inside, out GeoLine[] outside);

            Assert.Single(inside);
            Assert.Empty(outside);
            Assert.Equal(line.Length, inside[0].Length, 9);
        }

        [Fact]
        public void Line_ThroughAConcaveNotch_LeavesAndReenters()
        {
            // y = 5 passes through the notch: outside, inside, outside, inside, outside.
            var line = new GeoLine(new GeoPoint(-5, 5), new GeoPoint(30, 5));

            Assert.True(Splition.TrySplitBy(line, NotchedSquare(), out GeoLine[] inside, out GeoLine[] outside));

            Assert.Equal(2, inside.Length);
            Assert.Equal(3, outside.Length);
            Assert.Equal(5.0, inside[0].Length, 9);   // x from 0 to 5
            Assert.Equal(5.0, inside[1].Length, 9);   // x from 15 to 20
            Assert.Equal(10.0, outside[1].Length, 9); // the notch, x from 5 to 15
            Assert.Equal(line.Length, TotalLength(inside) + TotalLength(outside), 9);
        }

        [Fact]
        public void Line_RejectsANullPolygon()
        {
            var line = new GeoLine(new GeoPoint(0, 0), new GeoPoint(10, 0));
            Assert.Throws<ArgumentNullException>(
                () => Splition.TrySplitBy(line, (GeoPolygon)null, out GeoLine[] _, out GeoLine[] _));
        }

        #endregion

        #region Polyline

        [Fact]
        public void Polyline_CrossingOnce_SplitsAtTheBoundary()
        {
            // Starts outside, crosses the left edge at (0, 5), then bends upward inside the square.
            var path = new GeoPolyline(
                new GeoPoint(-5, 5), new GeoPoint(5, 5), new GeoPoint(5, 9));

            Assert.True(Splition.TrySplitBy(path, Square(), out GeoPolyline[] inside, out GeoPolyline[] outside));

            Assert.Single(outside);
            Assert.True(outside[0][0].IsEqualTo(new GeoPoint(-5, 5)));
            Assert.True(outside[0][outside[0].VertexCount - 1].IsEqualTo(new GeoPoint(0, 5)));

            // The inside run bends, so it arrives as one continuous polyline segment.
            Assert.Single(inside);
            Assert.True(inside[0][0].IsEqualTo(new GeoPoint(0, 5)));
            Assert.True(inside[0][inside[0].VertexCount - 1].IsEqualTo(new GeoPoint(5, 9)));
            Assert.Equal(3, inside[0].VertexCount);

            Assert.Equal(path.Length, inside.Sum(p => p.Length) + outside.Sum(p => p.Length), 9);
        }

        [Fact]
        public void Polyline_SegmentsChainEndToEndAcrossBothBuckets()
        {
            var path = new GeoPolyline(
                new GeoPoint(-5, 5), new GeoPoint(5, 5), new GeoPoint(5, 15), new GeoPoint(15, 15));

            Assert.True(Splition.TrySplitBy(path, Square(), out GeoPolyline[] inside, out GeoPolyline[] outside));

            GeoLine[] insideEdges = ToEdges(inside);
            GeoLine[] outsideEdges = ToEdges(outside);

            // Putting the two buckets back in order along the subject must retrace the original path.
            GeoLine[] all = insideEdges.Concat(outsideEdges)
                                  .OrderBy(seg => path.GetDistanceAtPoint(seg.MidPoint))
                                  .ToArray();

            Assert.True(all[0].StartPoint.IsEqualTo(path[0]));
            Assert.True(all[all.Length - 1].EndPoint.IsEqualTo(path[path.VertexCount - 1]));
            for (int i = 1; i < all.Length; i++)
            {
                Assert.True(all[i - 1].EndPoint.IsEqualTo(all[i].StartPoint));
            }

            Assert.Equal(path.Length, TotalLength(insideEdges) + TotalLength(outsideEdges), 9);
        }

        [Fact]
        public void Polyline_EntirelyInside_ReportsNoCrossingAndKeepsEverySegment()
        {
            var path = new GeoPolyline(
                new GeoPoint(2, 2), new GeoPoint(8, 2), new GeoPoint(8, 8));

            Assert.False(Splition.TrySplitBy(path, Square(), out GeoPolyline[] inside, out GeoPolyline[] outside));

            Assert.Single(inside);
            Assert.Equal(path.VertexCount, inside[0].VertexCount);
            Assert.Empty(outside);
            Assert.Equal(path.Length, inside[0].Length, 9);
        }

        [Fact]
        public void Polyline_EntirelyOutside_ReportsNoCrossing()
        {
            var path = new GeoPolyline(
                new GeoPoint(50, 50), new GeoPoint(60, 50), new GeoPoint(60, 60));

            Assert.False(Splition.TrySplitBy(path, Square(), out GeoPolyline[] inside, out GeoPolyline[] outside));

            Assert.Empty(inside);
            Assert.Single(outside);
            Assert.Equal(path.VertexCount, outside[0].VertexCount);
        }

        [Fact]
        public void Polyline_TouchingTheBoundaryAndTurningBack_IsNotACrossing()
        {
            // The middle vertex rests exactly on the top edge, then the path retreats upward again.
            var path = new GeoPolyline(
                new GeoPoint(2, 15), new GeoPoint(5, 10), new GeoPoint(8, 15));

            Assert.False(Splition.TrySplitBy(path, Square(), out GeoPolyline[] inside, out GeoPolyline[] outside));

            Assert.Empty(inside);
            Assert.Single(outside);
            Assert.Equal(path.Length, outside[0].Length, 9);
        }

        [Fact]
        public void Polyline_ThroughAConcaveNotch_LeavesAndReenters()
        {
            var path = new GeoPolyline(new GeoPoint(-5, 5), new GeoPoint(30, 5));

            Assert.True(Splition.TrySplitBy(path, NotchedSquare(), out GeoPolyline[] inside, out GeoPolyline[] outside));

            Assert.Equal(2, inside.Length);
            Assert.Equal(3, outside.Length);
            Assert.Equal(path.Length, inside.Sum(p => p.Length) + outside.Sum(p => p.Length), 9);
        }

        [Fact]
        public void Polyline_RejectsNullArguments()
        {
            var path = new GeoPolyline(new GeoPoint(0, 0), new GeoPoint(10, 0));

            Assert.Throws<ArgumentNullException>(
                () => Splition.TrySplitBy(path, (GeoPolygon)null, out GeoPolyline[] _, out GeoPolyline[] _));
            Assert.Throws<ArgumentNullException>(
                () => Splition.TrySplitBy((GeoPolyline)null, Square(), out GeoPolyline[] _, out GeoPolyline[] _));
        }

        #endregion

        #region Invariants

        [Fact]
        public void EverySplit_PreservesLengthAndClassifiesEverySegmentCorrectly()
        {
            var random = new Random(42);
            var tolerance = new Tolerance(1E-4, 1E-4);
            GeoPolygon square = Square();

            for (int c = 0; c < 300; c++)
            {
                var path = new GeoPolyline(
                    new GeoPoint(random.NextDouble() * 30 - 10, random.NextDouble() * 30 - 10),
                    new GeoPoint(random.NextDouble() * 30 - 10, random.NextDouble() * 30 - 10),
                    new GeoPoint(random.NextDouble() * 30 - 10, random.NextDouble() * 30 - 10));

                Splition.TrySplitBy(path, square, out GeoPolyline[] inside, out GeoPolyline[] outside, tolerance);

                GeoLine[] insideEdges = ToEdges(inside);
                GeoLine[] outsideEdges = ToEdges(outside);

                // Nothing is lost and nothing is counted twice.
                Assert.Equal(path.Length, TotalLength(insideEdges) + TotalLength(outsideEdges), 6);

                // Each segment landed in the bucket its own midpoint says it belongs to.
                foreach (GeoLine segment in insideEdges)
                {
                    Assert.True(Containment.Contains(square, segment.MidPoint, tolerance),
                        $"case {c}: segment {segment} filed as inside");
                }
                foreach (GeoLine segment in outsideEdges)
                {
                    Assert.False(Containment.Contains(square, segment.MidPoint, tolerance),
                        $"case {c}: segment {segment} filed as outside");
                }
            }
        }

        [Fact]
        public void LineAndPolylineForms_AgreeOnTheSameGeometry()
        {
            var random = new Random(7);
            var tolerance = new Tolerance(1E-4, 1E-4);
            GeoPolygon square = Square();

            for (int c = 0; c < 300; c++)
            {
                var line = new GeoLine(
                    new GeoPoint(random.NextDouble() * 30 - 10, random.NextDouble() * 30 - 10),
                    new GeoPoint(random.NextDouble() * 30 - 10, random.NextDouble() * 30 - 10));

                // The same segment expressed as a two vertex polyline has to split identically.
                var asPath = new GeoPolyline(line.StartPoint, line.EndPoint);

                bool a = Splition.TrySplitBy(line, square, out GeoLine[] inA, out GeoLine[] outA, tolerance);
                bool b = Splition.TrySplitBy(asPath, square, out GeoPolyline[] inB, out GeoPolyline[] outB, tolerance);

                GeoLine[] inBEdges = ToEdges(inB);
                GeoLine[] outBEdges = ToEdges(outB);

                Assert.Equal(a, b);
                Assert.Equal(inA.Length, inBEdges.Length);
                Assert.Equal(outA.Length, outBEdges.Length);
                Assert.Equal(TotalLength(inA), TotalLength(inBEdges), 6);
                Assert.Equal(TotalLength(outA), TotalLength(outBEdges), 6);
            }
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
