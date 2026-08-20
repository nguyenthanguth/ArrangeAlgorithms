using System;
using ArrangeAlgorithms.Geometry;
using ArrangeAlgorithms.Core;
using ArrangeAlgorithms.Enums;
using Xunit;

namespace ArrangeAlgorithms.UnitTest.Geometry
{
    public class GeoCircleTests
    {
        [Fact]
        public void Constructor_SetsCenterAndRadiusCorrectly()
        {
            var center = new GeoPoint(10, 20);
            var circle = new GeoCircle(center, 15);

            Assert.Equal(10, circle.Center.X);
            Assert.Equal(20, circle.Center.Y);
            Assert.Equal(15, circle.Radius);
            Assert.Equal(30, circle.Diameter);
            Assert.True(circle.Area > 0);
            Assert.True(circle.Circumference > 0);
        }

        [Fact]
        public void ContainsAndLocate_IdentifiesInsideOutsideOnSide()
        {
            var circle = new GeoCircle(new GeoPoint(0, 0), 10);

            Assert.True(circle.Contains(new GeoPoint(0, 0)));
            Assert.True(circle.Contains(new GeoPoint(5, 5)));
            Assert.Equal(PointLocation.Inside, circle.Locate(new GeoPoint(3, 4)));

            Assert.Equal(PointLocation.OnSide, circle.Locate(new GeoPoint(10, 0)));
            Assert.Equal(PointLocation.OnSide, circle.Locate(new GeoPoint(0, 10)));

            Assert.False(circle.Contains(new GeoPoint(10.1, 0)));
            Assert.Equal(PointLocation.OutSide, circle.Locate(new GeoPoint(15, 15)));
        }

        [Fact]
        public void CollidesWith_DetectsLineAndCircleCollisions()
        {
            var c1 = new GeoCircle(new GeoPoint(0, 0), 10);
            var c2 = new GeoCircle(new GeoPoint(15, 0), 10);
            var c3 = new GeoCircle(new GeoPoint(30, 0), 5);

            Assert.True(c1.CollidesWith(c2));
            Assert.False(c1.CollidesWith(c3));

            var line1 = new GeoLine(new GeoPoint(-20, 0), new GeoPoint(20, 0));
            var line2 = new GeoLine(new GeoPoint(0, 20), new GeoPoint(10, 20));

            Assert.True(c1.CollidesWith(line1));
            Assert.False(c1.CollidesWith(line2));
        }

        [Fact]
        public void GetIntersections_CalculatesAccurateIntersectionPoints()
        {
            var circle = new GeoCircle(new GeoPoint(0, 0), 5);
            var line = new GeoLine(new GeoPoint(-10, 0), new GeoPoint(10, 0));

            var pts = circle.GetIntersections(line);
            Assert.Equal(2, pts.Length);
            Assert.True(pts[0].IsEqualTo(new GeoPoint(-5, 0)) || pts[0].IsEqualTo(new GeoPoint(5, 0)));
            Assert.True(pts[1].IsEqualTo(new GeoPoint(-5, 0)) || pts[1].IsEqualTo(new GeoPoint(5, 0)));
        }

        [Fact]
        public void Circle_ToRectangle_ConvertsToOrientedBoundingSquare()
        {
            var center = new GeoPoint(10.0, 20.0);
            var circle = new GeoCircle(center, 5.0);
            double angleRad = 0.5;

            GeoRectangle rect = circle.ToRectangle(angleRad);

            Assert.True(rect.Center.IsEqualTo(center));
            Assert.Equal(10.0, rect.Width, 9);
            Assert.Equal(10.0, rect.Height, 9);
            Assert.Equal(angleRad, rect.AngleRad, 9);

            // Verify corners are at distance of R * sqrt(2) from center
            GeoPoint[] vertices = rect.GetVertices();
            double expectedDist = 5.0 * Math.Sqrt(2.0);
            foreach (var vertex in vertices)
            {
                Assert.Equal(expectedDist, rect.Center.DistanceTo(vertex), 9);
            }
        }
    }
}
