using System;
using System.Collections.Generic;
using ArrangeAlgorithms.Geometry;
using ArrangeAlgorithms.Core;
using ArrangeAlgorithms.Enums;
using Xunit;

namespace ArrangeAlgorithms.UnitTest.Geometry
{
    public class GeoPolylineTests
    {
        [Fact]
        public void Constructor_CalculatesVerticesAndLength()
        {
            var vertices = new List<GeoPoint>
            {
                new GeoPoint(0, 0),
                new GeoPoint(10, 0),
                new GeoPoint(10, 10)
            };

            var polyline = new GeoPolyline(vertices);

            Assert.Equal(3, polyline.VertexCount);
            Assert.Equal(2, polyline.EdgeCount);
            Assert.Equal(20.0, polyline.Length, 4);
        }

        [Fact]
        public void Polyline_NeverClosesItself()
        {
            var vertices = new List<GeoPoint>
            {
                new GeoPoint(0, 0),
                new GeoPoint(10, 0),
                new GeoPoint(10, 10),
                new GeoPoint(0, 10)
            };

            var polyline = new GeoPolyline(vertices);

            // Four vertices make three edges, not four: the chain stops at the last vertex and never
            // runs back to the first. Enclosing that area is what GeoPolygon is for.
            Assert.Equal(4, polyline.VertexCount);
            Assert.Equal(3, polyline.EdgeCount);
            Assert.Equal(30.0, polyline.Length, 4);

            GeoPolygon closed = polyline.ToPolygon();
            Assert.Equal(4, closed.EdgeCount);
            Assert.Equal(40.0, closed.Length, 4);
        }

        [Fact]
        public void DistanceAndLocate_CalculatesCorrectly()
        {
            var polyline = new GeoPolyline(new[]
            {
                new GeoPoint(0, 0),
                new GeoPoint(10, 0),
                new GeoPoint(10, 10)
            });

            Assert.Equal(PointLocation.OnSide, polyline.Locate(new GeoPoint(5, 0)));
            Assert.Equal(PointLocation.OnSide, polyline.Locate(new GeoPoint(10, 5)));
            Assert.Equal(PointLocation.OutSide, polyline.Locate(new GeoPoint(5, 5)));

            Assert.Equal(5.0, polyline.DistanceTo(new GeoPoint(5, 5)), 4);
        }

        [Fact]
        public void GetIntersections_FindsSegmentIntersections()
        {
            var polyline = new GeoPolyline(new[]
            {
                new GeoPoint(0, 0),
                new GeoPoint(10, 0),
                new GeoPoint(10, 10)
            });

            var line = new GeoLine(new GeoPoint(5, -5), new GeoPoint(5, 5));
            var pts = polyline.GetIntersections(line);

            Assert.Single(pts);
            Assert.True(pts[0].IsEqualTo(new GeoPoint(5, 0)));
        }
        [Fact]
        public void Polyline_TranslateAndRotate_PreserveShape()
        {
            var polyline = new GeoPolyline(new GeoPoint(0, 0), new GeoPoint(10, 0), new GeoPoint(10, 10));

            var moved = polyline.Translate(new GeoVector(5, -5));
            Assert.True(moved[0].IsEqualTo(new GeoPoint(5, -5)));
            Assert.Equal(polyline.Length, moved.Length, 9);
            Assert.Equal(moved, polyline + new GeoVector(5, -5));
            Assert.Equal(polyline, moved - new GeoVector(5, -5));

            var rotated = polyline.RotateBy(Math.PI / 2.0, new GeoPoint(0, 0));
            Assert.True(rotated[1].IsEqualTo(new GeoPoint(0, 10)));
            Assert.Equal(polyline.Length, rotated.Length, 9);
        }

        [Fact]
        public void ToPolygon_ClosesTheChain()
        {
            // The migration path for geometry that used to be a closed polyline.
            var polyline = new GeoPolyline(new GeoPoint(0, 0), new GeoPoint(10, 0), new GeoPoint(10, 10));
            var polygon = polyline.ToPolygon();

            Assert.Equal(3, polygon.VertexCount);
            Assert.Equal(3, polygon.EdgeCount);

            // Two vertices cannot enclose anything.
            var degenerate = new GeoPolyline(new GeoPoint(0, 0), new GeoPoint(10, 0));
            Assert.Throws<ArgumentException>(() => degenerate.ToPolygon());
        }

        [Fact]
        public void EdgeCount_IsAlwaysOneLessThanVertexCount()
        {
            // Every route into a GeoPolyline upholds the two-vertex minimum, which is what lets
            // EdgeCount subtract one without guarding the result.
            Assert.Equal(1, new GeoPolyline(new GeoPoint(0, 0), new GeoPoint(1, 1)).EdgeCount);
            Assert.Equal(2, new GeoPolyline(new GeoPoint(0, 0), new GeoPoint(1, 1), new GeoPoint(2, 0)).EdgeCount);

            Assert.Throws<ArgumentException>(() => new GeoPolyline(new GeoPoint(0, 0)));
            Assert.Throws<ArgumentException>(
                () => new GeoPolyline(new GeoPoint(0, 0), new GeoPoint(0, 0)));

            // Splitting and cloning go through the trusted constructor, and never produce a shorter one.
            var path = new GeoPolyline(new GeoPoint(0, 0), new GeoPoint(10, 0), new GeoPoint(10, 10));
            Assert.Equal(path.EdgeCount, path.Clone().EdgeCount);

            foreach (GeoPolyline piece in Splition.SplitAtDistances(path, new[] { 5.0, 10.0, 15.0 }))
            {
                Assert.True(piece.VertexCount >= 2);
                Assert.Equal(piece.VertexCount - 1, piece.EdgeCount);
            }
        }

    }
}
