using System;
using System.Linq;
using ArrangeAlgorithms.Geometry;
using ArrangeAlgorithms.Core;
using Xunit;

namespace ArrangeAlgorithms.UnitTest.Operations
{
    public class SplitionLineMultipleTests
    {
        private static readonly Tolerance Tol = new Tolerance(1E-9, 1E-9);

        [Fact]
        public void Line_SplitByMultipleLines_SplitsCorrectly()
        {
            // Subject runs along X-axis from (0, 0) to (10, 0)
            var subject = new GeoLine(new GeoPoint(0, 0), new GeoPoint(10, 0));

            // Cutters intersect at X = 3.0 and X = 7.0
            var cutters = new[]
            {
                new GeoLine(new GeoPoint(3, -5), new GeoPoint(3, 5)),
                new GeoLine(new GeoPoint(7, -5), new GeoPoint(7, 5))
            };

            Assert.True(subject.TrySplitBy(cutters, out GeoLine[] pieces));
            Assert.Equal(3, pieces.Length);

            Assert.True(pieces[0].StartPoint.IsEqualTo(new GeoPoint(0, 0), Tol));
            Assert.True(pieces[0].EndPoint.IsEqualTo(new GeoPoint(3, 0), Tol));

            Assert.True(pieces[1].StartPoint.IsEqualTo(new GeoPoint(3, 0), Tol));
            Assert.True(pieces[1].EndPoint.IsEqualTo(new GeoPoint(7, 0), Tol));

            Assert.True(pieces[2].StartPoint.IsEqualTo(new GeoPoint(7, 0), Tol));
            Assert.True(pieces[2].EndPoint.IsEqualTo(new GeoPoint(10, 0), Tol));

            Assert.Equal(subject.Length, pieces.Sum(p => p.Length), 9);
        }

        [Fact]
        public void Line_SplitByMultipleLines_NoIntersections_ReturnsFalse()
        {
            var subject = new GeoLine(new GeoPoint(0, 0), new GeoPoint(10, 0));

            // Cutters are parallel or far away
            var cutters = new[]
            {
                new GeoLine(new GeoPoint(-5, -5), new GeoPoint(-5, 5)),
                new GeoLine(new GeoPoint(15, -5), new GeoPoint(15, 5))
            };

            Assert.False(subject.TrySplitBy(cutters, out GeoLine[] pieces));
            Assert.Single(pieces);
            Assert.True(pieces[0].StartPoint.IsEqualTo(subject.StartPoint, Tol));
            Assert.True(pieces[0].EndPoint.IsEqualTo(subject.EndPoint, Tol));
        }

        [Fact]
        public void Line_SplitByPolyline_SplitsCorrectly()
        {
            // Subject runs along X-axis from (0, 0) to (10, 0)
            var subject = new GeoLine(new GeoPoint(0, 0), new GeoPoint(10, 0));

            // Cutter is a polyline that crosses the X-axis twice at X = 2.0 and X = 8.0
            var cutter = new GeoPolyline(
                new GeoPoint(2, -2),
                new GeoPoint(5, 5),
                new GeoPoint(8, -2)
            );

            Assert.True(subject.TrySplitBy(cutter, out GeoLine[] pieces));
            Assert.Equal(3, pieces.Length);

            Assert.True(pieces[0].StartPoint.IsEqualTo(new GeoPoint(0, 0), Tol));
            Assert.True(pieces[0].EndPoint.IsEqualTo(new GeoPoint(2, 0), Tol));

            Assert.True(pieces[1].StartPoint.IsEqualTo(new GeoPoint(2, 0), Tol));
            Assert.True(pieces[1].EndPoint.IsEqualTo(new GeoPoint(8, 0), Tol));

            Assert.True(pieces[2].StartPoint.IsEqualTo(new GeoPoint(8, 0), Tol));
            Assert.True(pieces[2].EndPoint.IsEqualTo(new GeoPoint(10, 0), Tol));

            Assert.Equal(subject.Length, pieces.Sum(p => p.Length), 9);
        }

        [Fact]
        public void Line_SplitByMultiplePoints_SplitsCorrectly()
        {
            var subject = new GeoLine(new GeoPoint(0, 0), new GeoPoint(10, 0));
            var points = new[]
            {
                new GeoPoint(4, 0),
                new GeoPoint(6, 0),
                new GeoPoint(12, 0) // Outside, should be ignored
            };

            Assert.True(subject.TrySplitBy(points, out GeoLine[] pieces));
            Assert.Equal(3, pieces.Length);
            Assert.True(pieces[0].StartPoint.IsEqualTo(new GeoPoint(0, 0), Tol));
            Assert.True(pieces[0].EndPoint.IsEqualTo(new GeoPoint(4, 0), Tol));
            Assert.True(pieces[1].StartPoint.IsEqualTo(new GeoPoint(4, 0), Tol));
            Assert.True(pieces[1].EndPoint.IsEqualTo(new GeoPoint(6, 0), Tol));
            Assert.True(pieces[2].StartPoint.IsEqualTo(new GeoPoint(6, 0), Tol));
            Assert.True(pieces[2].EndPoint.IsEqualTo(new GeoPoint(10, 0), Tol));
            Assert.Equal(subject.Length, pieces.Sum(p => p.Length), 9);
        }

        [Fact]
        public void Line_SplitByMultiplePolygons_SplitsCorrectly()
        {
            var subject = new GeoLine(new GeoPoint(-5, 0), new GeoPoint(15, 0));
            
            // Two 2x2 squares along the X-axis
            var poly1 = new GeoPolygon(new GeoPoint(0, -1), new GeoPoint(2, -1), new GeoPoint(2, 1), new GeoPoint(0, 1));
            var poly2 = new GeoPolygon(new GeoPoint(8, -1), new GeoPoint(10, -1), new GeoPoint(10, 1), new GeoPoint(8, 1));
            
            Assert.True(subject.TrySplitBy(new[] { poly1, poly2 }, out GeoLine[] inside, out GeoLine[] outside));
            Assert.Equal(2, inside.Length);
            Assert.Equal(3, outside.Length);
            
            Assert.True(outside[0].StartPoint.IsEqualTo(new GeoPoint(-5, 0), Tol));
            Assert.True(outside[0].EndPoint.IsEqualTo(new GeoPoint(0, 0), Tol));
            Assert.True(inside[0].StartPoint.IsEqualTo(new GeoPoint(0, 0), Tol));
            Assert.True(inside[0].EndPoint.IsEqualTo(new GeoPoint(2, 0), Tol));
            Assert.True(outside[1].StartPoint.IsEqualTo(new GeoPoint(2, 0), Tol));
            Assert.True(outside[1].EndPoint.IsEqualTo(new GeoPoint(8, 0), Tol));
            Assert.True(inside[1].StartPoint.IsEqualTo(new GeoPoint(8, 0), Tol));
            Assert.True(inside[1].EndPoint.IsEqualTo(new GeoPoint(10, 0), Tol));
            Assert.True(outside[2].StartPoint.IsEqualTo(new GeoPoint(10, 0), Tol));
            Assert.True(outside[2].EndPoint.IsEqualTo(new GeoPoint(15, 0), Tol));
            
            Assert.Equal(subject.Length, inside.Sum(p => p.Length) + outside.Sum(p => p.Length), 9);
        }

        [Fact]
        public void Line_SplitByOverlappingPolygons_MergesCorrectly()
        {
            var subject = new GeoLine(new GeoPoint(-5, 0), new GeoPoint(15, 0));
            
            // Two 2x2 squares sharing a boundary at X = 2
            var poly1 = new GeoPolygon(new GeoPoint(0, -1), new GeoPoint(2, -1), new GeoPoint(2, 1), new GeoPoint(0, 1));
            var poly2 = new GeoPolygon(new GeoPoint(2, -1), new GeoPoint(4, -1), new GeoPoint(4, 1), new GeoPoint(2, 1));
            
            Assert.True(subject.TrySplitBy(new[] { poly1, poly2 }, out GeoLine[] inside, out GeoLine[] outside));
            
            // Do kề sát nhau, phần inside từ 0 đến 4 được gộp thành 1 đoạn duy nhất
            Assert.Single(inside);
            Assert.True(inside[0].StartPoint.IsEqualTo(new GeoPoint(0, 0), Tol));
            Assert.True(inside[0].EndPoint.IsEqualTo(new GeoPoint(4, 0), Tol));
            
            // Phần outside gồm [-5, 0] và [4, 15]
            Assert.Equal(2, outside.Length);
            Assert.True(outside[0].StartPoint.IsEqualTo(new GeoPoint(-5, 0), Tol));
            Assert.True(outside[0].EndPoint.IsEqualTo(new GeoPoint(0, 0), Tol));
            Assert.True(outside[1].StartPoint.IsEqualTo(new GeoPoint(4, 0), Tol));
            Assert.True(outside[1].EndPoint.IsEqualTo(new GeoPoint(15, 0), Tol));
        }

        [Fact]
        public void Line_SplitByMultiplePolylines_SplitsCorrectly()
        {
            var subject = new GeoLine(new GeoPoint(0, 0), new GeoPoint(10, 0));
            
            var polyline1 = new GeoPolyline(new GeoPoint(3, -2), new GeoPoint(3, 2));
            var polyline2 = new GeoPolyline(new GeoPoint(7, -2), new GeoPoint(7, 2));

            Assert.True(subject.TrySplitBy(new[] { polyline1, polyline2 }, out GeoLine[] pieces));
            Assert.Equal(3, pieces.Length);
            
            Assert.True(pieces[0].StartPoint.IsEqualTo(new GeoPoint(0, 0), Tol));
            Assert.True(pieces[0].EndPoint.IsEqualTo(new GeoPoint(3, 0), Tol));
            Assert.True(pieces[1].StartPoint.IsEqualTo(new GeoPoint(3, 0), Tol));
            Assert.True(pieces[1].EndPoint.IsEqualTo(new GeoPoint(7, 0), Tol));
            Assert.True(pieces[2].StartPoint.IsEqualTo(new GeoPoint(7, 0), Tol));
            Assert.True(pieces[2].EndPoint.IsEqualTo(new GeoPoint(10, 0), Tol));
            Assert.Equal(subject.Length, pieces.Sum(p => p.Length), 9);
        }

        [Fact]
        public void Line_SplitByNullArguments_ThrowsException()
        {
            var subject = new GeoLine(new GeoPoint(0, 0), new GeoPoint(10, 0));

            Assert.Throws<ArgumentNullException>(() => subject.TrySplitBy((GeoLine[])null, out _));
            Assert.Throws<ArgumentNullException>(() => subject.TrySplitBy((GeoPolyline)null, out _));
            Assert.Throws<ArgumentNullException>(() => subject.TrySplitBy((GeoPoint[])null, out _));
            Assert.Throws<ArgumentNullException>(() => subject.TrySplitBy((GeoPolygon[])null, out _, out _));
            Assert.Throws<ArgumentNullException>(() => subject.TrySplitBy((GeoPolyline[])null, out _));
        }
    }
}
