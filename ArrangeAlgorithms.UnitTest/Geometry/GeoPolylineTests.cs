using System;
using System.Collections.Generic;
using ArrangeAlgorithms.Geometry;
using ArrangeAlgorithms.Operations;
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

            var polyline = new GeoPolyline(vertices, isClosed: false);

            Assert.Equal(3, polyline.VertexCount);
            Assert.Equal(2, polyline.EdgeCount);
            Assert.Equal(20.0, polyline.Length, 4);
            Assert.False(polyline.IsClosed);
        }

        [Fact]
        public void ClosedPolyline_IncludesClosingEdge()
        {
            var vertices = new List<GeoPoint>
            {
                new GeoPoint(0, 0),
                new GeoPoint(10, 0),
                new GeoPoint(10, 10),
                new GeoPoint(0, 10)
            };

            var polyline = new GeoPolyline(vertices, isClosed: true);

            Assert.Equal(4, polyline.VertexCount);
            Assert.Equal(4, polyline.EdgeCount);
            Assert.Equal(40.0, polyline.Length, 4);
            Assert.True(polyline.IsClosed);
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
        public void Polyline_TranslateAndRotate_PreserveShapeAndClosedFlag()
        {
            var polyline = new GeoPolyline(true, new GeoPoint(0, 0), new GeoPoint(10, 0), new GeoPoint(10, 10));

            var moved = polyline.Translate(new GeoVector(5, -5));
            Assert.True(moved[0].IsEqualTo(new GeoPoint(5, -5)));
            Assert.True(moved.IsClosed);
            Assert.Equal(polyline.Length, moved.Length, 9);
            Assert.Equal(moved, polyline + new GeoVector(5, -5));
            Assert.Equal(polyline, moved - new GeoVector(5, -5));

            var rotated = polyline.RotateBy(Math.PI / 2.0, new GeoPoint(0, 0));
            Assert.True(rotated[1].IsEqualTo(new GeoPoint(0, 10)));
            Assert.True(rotated.IsClosed);
            Assert.Equal(polyline.Length, rotated.Length, 9);
        }

        [Fact]
        public void ToPolygon_ConvertsBothOpenAndClosedPolylines()
        {
            // 1. Closed polyline with 3 vertices
            var closedPolyline = new GeoPolyline(true, new GeoPoint(0, 0), new GeoPoint(10, 0), new GeoPoint(10, 10));
            var polygon1 = closedPolyline.ToPolygon();
            Assert.Equal(3, polygon1.VertexCount);

            // 2. Open polyline with 3 vertices (automatically closed into a polygon)
            var openPolyline = new GeoPolyline(false, new GeoPoint(0, 0), new GeoPoint(10, 0), new GeoPoint(10, 10));
            var polygon2 = openPolyline.ToPolygon();
            Assert.Equal(3, polygon2.VertexCount);

            // 3. Degenerate case: polyline with only 2 vertices cannot form a polygon
            var degeneratePolyline = new GeoPolyline(false, new GeoPoint(0, 0), new GeoPoint(10, 0));
            Assert.Throws<ArgumentException>(() => degeneratePolyline.ToPolygon());
        }
    }
}
