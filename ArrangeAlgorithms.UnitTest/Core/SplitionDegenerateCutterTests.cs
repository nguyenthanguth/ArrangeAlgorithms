using System.Linq;
using ArrangeAlgorithms.Core;
using ArrangeAlgorithms.Geometry;
using Xunit;

namespace ArrangeAlgorithms.UnitTest.Core
{
    /// <summary>
    /// Cutters that are not the well behaved shapes the ordinary cases use.
    /// <para>
    /// GeoPolygon does not check that its edges avoid each other, so a caller can build one that crosses
    /// itself. What happens then was previously written off as undefined; it is not. Containment counts
    /// ray crossings, which is the even-odd rule, and every operation resting on containment inherits it.
    /// These cases pin that down, so the behaviour is something the library promises rather than
    /// something it happens to do.
    /// </para>
    /// </summary>
    public class SplitionDegenerateCutterTests
    {
        private static readonly Tolerance Tol = new Tolerance(1E-4, 1E-4);

        // A bowtie: the edges (0,0)->(10,10) and (10,0)->(0,10) cross at (5, 5).
        private static GeoPolygon Bowtie() => new GeoPolygon(
            new GeoPoint(0, 0), new GeoPoint(10, 10), new GeoPoint(10, 0), new GeoPoint(0, 10));

        [Fact]
        public void SelfIntersectingCutter_SplitsUnderTheEvenOddRule()
        {
            // At y = 2 the bowtie has two lobes, one either side, meeting at the crossing point.
            var line = new GeoLine(new GeoPoint(-5, 2), new GeoPoint(15, 2));

            Assert.True(line.TrySplitBy(Bowtie(), out GeoLine[] inside, out GeoLine[] outside, Tol));

            Assert.Equal(2, inside.Length);
            Assert.Equal(3, outside.Length);

            Assert.True(inside[0].StartPoint.IsEqualTo(new GeoPoint(0, 2), Tol));
            Assert.True(inside[0].EndPoint.IsEqualTo(new GeoPoint(2, 2), Tol));
            Assert.True(inside[1].StartPoint.IsEqualTo(new GeoPoint(8, 2), Tol));
            Assert.True(inside[1].EndPoint.IsEqualTo(new GeoPoint(10, 2), Tol));

            // The stretch between the lobes is enclosed twice, which even-odd reads as outside.
            Assert.True(outside[1].StartPoint.IsEqualTo(new GeoPoint(2, 2), Tol));
            Assert.True(outside[1].EndPoint.IsEqualTo(new GeoPoint(8, 2), Tol));

            Assert.Equal(line.Length, inside.Sum(p => p.Length) + outside.Sum(p => p.Length), 9);
        }

        [Fact]
        public void SelfIntersectingCutter_ReportsZeroAreaBecauseItsLobesCancel()
        {
            // Worth pinning because it looks like a bug from outside: the shoelace sum gives each lobe
            // its own sign, so a symmetric figure-eight sums to nothing at all.
            Assert.Equal(0.0, Bowtie().GetSignedArea(), 9);
            Assert.Equal(0.0, Bowtie().GetArea(), 9);
        }

        [Fact]
        public void CutterWithCollinearVertices_BehavesLikeTheSameShapeWithout()
        {
            // The extra vertex at (5, 0) carries no corner. It must not change any answer.
            var plain = new GeoPolygon(
                new GeoPoint(0, 0), new GeoPoint(10, 0), new GeoPoint(10, 10), new GeoPoint(0, 10));
            var withExtra = new GeoPolygon(
                new GeoPoint(0, 0), new GeoPoint(5, 0), new GeoPoint(10, 0),
                new GeoPoint(10, 10), new GeoPoint(0, 10));

            Assert.Equal(5, withExtra.VertexCount);
            Assert.Equal(plain.GetArea(), withExtra.GetArea(), 9);

            var line = new GeoLine(new GeoPoint(-5, 5), new GeoPoint(15, 5));

            Assert.True(line.TrySplitBy(plain, out GeoLine[] inA, out GeoLine[] outA, Tol));
            Assert.True(line.TrySplitBy(withExtra, out GeoLine[] inB, out GeoLine[] outB, Tol));

            Assert.Equal(inA.Length, inB.Length);
            Assert.Equal(outA.Length, outB.Length);
            Assert.Equal(inA.Sum(p => p.Length), inB.Sum(p => p.Length), 9);
        }

        [Fact]
        public void ManyCuttersAndManyCuts_StayLinear()
        {
            // Not a benchmark, a guard: the walk is meant to touch each vertex and each cut once, so a
            // subject and a cut list this size must not take a noticeable amount of time.
            var path = new GeoPolyline(Enumerable.Range(0, 500)
                .Select(i => new GeoPoint(i, i % 2 == 0 ? 0 : 1)).ToArray());

            double[] cuts = Enumerable.Range(1, 5000)
                .Select(i => path.Length * i / 5001.0).ToArray();

            GeoPolyline[] pieces = Splition.SplitAtDistances(path, cuts, Tol);

            Assert.Equal(5001, pieces.Length);
            Assert.Equal(path.Length, pieces.Sum(p => p.Length), 6);

            GeoPolygon[] polygons = Enumerable.Range(0, 50)
                .Select(i => new GeoPolygon(
                    new GeoPoint(i * 10, -1), new GeoPoint(i * 10 + 5, -1),
                    new GeoPoint(i * 10 + 5, 2), new GeoPoint(i * 10, 2)))
                .ToArray();

            Assert.True(path.TrySplitBy(polygons, out GeoPolyline[] inside, out GeoPolyline[] outside, Tol));
            Assert.Equal(path.Length, inside.Sum(p => p.Length) + outside.Sum(p => p.Length), 6);
        }
    }
}
