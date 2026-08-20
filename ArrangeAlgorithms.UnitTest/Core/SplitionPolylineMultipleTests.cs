using System;
using System.Linq;
using ArrangeAlgorithms.Geometry;
using ArrangeAlgorithms.Core;
using Xunit;

namespace ArrangeAlgorithms.UnitTest.Core
{
    public class SplitionPolylineMultipleTests
    {
        private static readonly Tolerance Tol = new Tolerance(1E-9, 1E-9);

        [Fact]
        public void Polyline_SplitByMultiplePoints_SplitsCorrectly()
        {
            // Subject runs: (0, 0) -> (5, 0) -> (5, 5)
            var subject = new GeoPolyline(new GeoPoint(0, 0), new GeoPoint(5, 0), new GeoPoint(5, 5));
            var points = new[]
            {
                new GeoPoint(2, 0),
                new GeoPoint(5, 3),
                new GeoPoint(5, 6) // Outside
            };

            Assert.True(subject.TrySplitBy(points, out GeoPolyline[] pieces));
            Assert.Equal(3, pieces.Length);

            // Piece 0: (0,0) -> (2,0)
            Assert.Equal(2, pieces[0].VertexCount);
            Assert.True(pieces[0][0].IsEqualTo(new GeoPoint(0, 0), Tol));
            Assert.True(pieces[0][1].IsEqualTo(new GeoPoint(2, 0), Tol));

            // Piece 1: (2,0) -> (5,0) -> (5,3)
            Assert.Equal(3, pieces[1].VertexCount);
            Assert.True(pieces[1][0].IsEqualTo(new GeoPoint(2, 0), Tol));
            Assert.True(pieces[1][1].IsEqualTo(new GeoPoint(5, 0), Tol));
            Assert.True(pieces[1][2].IsEqualTo(new GeoPoint(5, 3), Tol));

            // Piece 2: (5,3) -> (5,5)
            Assert.Equal(2, pieces[2].VertexCount);
            Assert.True(pieces[2][0].IsEqualTo(new GeoPoint(5, 3), Tol));
            Assert.True(pieces[2][1].IsEqualTo(new GeoPoint(5, 5), Tol));

            Assert.Equal(subject.Length, pieces.Sum(p => p.Length), 9);
        }

        [Fact]
        public void Polyline_SplitByMultipleLines_SplitsCorrectly()
        {
            var subject = new GeoPolyline(new GeoPoint(0, 0), new GeoPoint(5, 0), new GeoPoint(5, 5));
            var cutters = new[]
            {
                new GeoLine(new GeoPoint(2, -1), new GeoPoint(2, 1)),
                new GeoLine(new GeoPoint(4, 3), new GeoPoint(6, 3))
            };

            Assert.True(subject.TrySplitBy(cutters, out GeoPolyline[] pieces));
            Assert.Equal(3, pieces.Length);

            Assert.True(pieces[0][0].IsEqualTo(new GeoPoint(0, 0), Tol));
            Assert.True(pieces[0][pieces[0].VertexCount - 1].IsEqualTo(new GeoPoint(2, 0), Tol));

            Assert.True(pieces[1][0].IsEqualTo(new GeoPoint(2, 0), Tol));
            Assert.True(pieces[1][pieces[1].VertexCount - 1].IsEqualTo(new GeoPoint(5, 3), Tol));

            Assert.True(pieces[2][0].IsEqualTo(new GeoPoint(5, 3), Tol));
            Assert.True(pieces[2][pieces[2].VertexCount - 1].IsEqualTo(new GeoPoint(5, 5), Tol));

            Assert.Equal(subject.Length, pieces.Sum(p => p.Length), 9);
        }

        [Fact]
        public void Polyline_SplitByMultiplePolylines_SplitsCorrectly()
        {
            var subject = new GeoPolyline(new GeoPoint(0, 0), new GeoPoint(10, 0));
            var cutters = new[]
            {
                new GeoPolyline(new GeoPoint(3, -2), new GeoPoint(3, 2)),
                new GeoPolyline(new GeoPoint(7, -2), new GeoPoint(7, 2))
            };

            Assert.True(subject.TrySplitBy(cutters, out GeoPolyline[] pieces));
            Assert.Equal(3, pieces.Length);

            Assert.True(pieces[0][0].IsEqualTo(new GeoPoint(0, 0), Tol));
            Assert.True(pieces[0][1].IsEqualTo(new GeoPoint(3, 0), Tol));

            Assert.True(pieces[1][0].IsEqualTo(new GeoPoint(3, 0), Tol));
            Assert.True(pieces[1][1].IsEqualTo(new GeoPoint(7, 0), Tol));

            Assert.True(pieces[2][0].IsEqualTo(new GeoPoint(7, 0), Tol));
            Assert.True(pieces[2][1].IsEqualTo(new GeoPoint(10, 0), Tol));

            Assert.Equal(subject.Length, pieces.Sum(p => p.Length), 9);
        }

        [Fact]
        public void Polyline_SplitByMultiplePolygons_SplitsCorrectly()
        {
            var subject = new GeoPolyline(new GeoPoint(-5, 0), new GeoPoint(15, 0));
            var poly1 = new GeoPolygon(new GeoPoint(0, -1), new GeoPoint(2, -1), new GeoPoint(2, 1), new GeoPoint(0, 1));
            var poly2 = new GeoPolygon(new GeoPoint(8, -1), new GeoPoint(10, -1), new GeoPoint(10, 1), new GeoPoint(8, 1));

            Assert.True(subject.TrySplitBy(new[] { poly1, poly2 }, out GeoPolyline[] inside, out GeoPolyline[] outside));
            Assert.Equal(2, inside.Length);
            Assert.Equal(3, outside.Length);

            Assert.True(outside[0][0].IsEqualTo(new GeoPoint(-5, 0), Tol));
            Assert.True(outside[0][1].IsEqualTo(new GeoPoint(0, 0), Tol));

            Assert.True(inside[0][0].IsEqualTo(new GeoPoint(0, 0), Tol));
            Assert.True(inside[0][1].IsEqualTo(new GeoPoint(2, 0), Tol));

            Assert.True(outside[1][0].IsEqualTo(new GeoPoint(2, 0), Tol));
            Assert.True(outside[1][1].IsEqualTo(new GeoPoint(8, 0), Tol));

            Assert.True(inside[1][0].IsEqualTo(new GeoPoint(8, 0), Tol));
            Assert.True(inside[1][1].IsEqualTo(new GeoPoint(10, 0), Tol));

            Assert.True(outside[2][0].IsEqualTo(new GeoPoint(10, 0), Tol));
            Assert.True(outside[2][1].IsEqualTo(new GeoPoint(15, 0), Tol));

            Assert.Equal(subject.Length, inside.Sum(p => p.Length) + outside.Sum(p => p.Length), 9);
        }

        [Fact]
        public void Polyline_SplitByOverlappingPolygons_MergesCorrectly()
        {
            var subject = new GeoPolyline(new GeoPoint(-5, 0), new GeoPoint(15, 0));
            
            // Two 2x2 squares sharing a boundary at X = 2
            var poly1 = new GeoPolygon(new GeoPoint(0, -1), new GeoPoint(2, -1), new GeoPoint(2, 1), new GeoPoint(0, 1));
            var poly2 = new GeoPolygon(new GeoPoint(2, -1), new GeoPoint(4, -1), new GeoPoint(4, 1), new GeoPoint(2, 1));

            Assert.True(subject.TrySplitBy(new[] { poly1, poly2 }, out GeoPolyline[] inside, out GeoPolyline[] outside));
            
            // Do kề sát nhau, phần inside từ 0 đến 4 được gộp thành 1 polyline duy nhất
            Assert.Single(inside);
            Assert.True(inside[0][0].IsEqualTo(new GeoPoint(0, 0), Tol));
            Assert.True(inside[0][1].IsEqualTo(new GeoPoint(4, 0), Tol));

            // Phần outside gồm [-5, 0] và [4, 15]
            Assert.Equal(2, outside.Length);
            Assert.True(outside[0][0].IsEqualTo(new GeoPoint(-5, 0), Tol));
            Assert.True(outside[0][1].IsEqualTo(new GeoPoint(0, 0), Tol));
            Assert.True(outside[1][0].IsEqualTo(new GeoPoint(4, 0), Tol));
            Assert.True(outside[1][1].IsEqualTo(new GeoPoint(15, 0), Tol));
        }

        [Fact]
        public void Polyline_SplitByNullArguments_ThrowsException()
        {
            var subject = new GeoPolyline(new GeoPoint(0, 0), new GeoPoint(10, 0));

            Assert.Throws<ArgumentNullException>(() => subject.TrySplitBy((GeoPoint[])null, out _));
            Assert.Throws<ArgumentNullException>(() => subject.TrySplitBy((GeoLine[])null, out _));
            Assert.Throws<ArgumentNullException>(() => subject.TrySplitBy((GeoPolyline[])null, out _));
            Assert.Throws<ArgumentNullException>(() => subject.TrySplitBy((GeoPolygon[])null, out _, out _));
        }
    }
}
