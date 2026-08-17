using ArrangeAlgorithms.Geometry;
using System;
using Xunit;

namespace ArrangeAlgorithms.UnitTest
{
    public class PointTests
    {
        [Fact]
        public void Point_BasicOperations_WorkCorrectly()
        {
            var p1 = new GeoPoint(1.0, 2.0);
            var v = new GeoVector(3.0, 4.0);

            var p2 = p1 + v; // (4.0, 6.0)
            Assert.Equal(new GeoPoint(4.0, 6.0), p2);

            var p3 = p2 - v; // (1.0, 2.0)
            Assert.Equal(p1, p3);

            var vOffset = p2 - p1;
            Assert.Equal(v, vOffset);

            Assert.Equal(5.0, p1.DistanceTo(p2), 12);
            Assert.Equal(25.0, p1.GetDistanceSquaredTo(p2), 12);
        }

        [Fact]
        public void Point_RotationAndScaling_WorkCorrectly()
        {
            var origin = new GeoPoint(0.0, 0.0);
            var p = new GeoPoint(1.0, 0.0);

            // Rotate 90 degrees counter-clockwise
            var rotated = p.RotateBy(Math.PI / 2.0, origin);
            Assert.True(rotated.IsEqualTo(new GeoPoint(0.0, 1.0)));

            // Scale by factor of 2.5
            var scaled = p.ScaleBy(2.5, origin);
            Assert.True(scaled.IsEqualTo(new GeoPoint(2.5, 0.0)));
        }

        [Fact]
        public void Point_OperationsWithZeroAndSelf_WorkCorrectly()
        {
            var p = new GeoPoint(5.0, -3.0);
            Assert.Equal(0.0, p.DistanceTo(p));
            Assert.Equal(0.0, p.GetDistanceSquaredTo(p));

            var zeroVec = GeoVector.Zero;
            Assert.Equal(p, p + zeroVec);
            Assert.Equal(p, p - zeroVec);
            Assert.Equal(zeroVec, p - p);
        }

        [Fact]
        public void Point_RotationEdgeCases_WorkCorrectly()
        {
            var origin = new GeoPoint(2.0, 2.0);
            var p = new GeoPoint(5.0, 6.0);

            // Rotate 0 degrees
            Assert.True(p.RotateBy(0.0, origin).IsEqualTo(p));

            // Rotate 360 degrees (2 * PI)
            Assert.True(p.RotateBy(2.0 * Math.PI, origin).IsEqualTo(p));

            // Rotate 180 degrees (PI): Symmetric point across center
            var rotated180 = p.RotateBy(Math.PI, origin);
            Assert.True(rotated180.IsEqualTo(new GeoPoint(-1.0, -2.0)));
        }

        [Fact]
        public void Point_ScalingWithZeroAndNegative_WorkCorrectly()
        {
            var origin = new GeoPoint(1.0, 1.0);
            var p = new GeoPoint(3.0, 5.0);

            // Scale by factor 0: Returns exactly to scaling center
            Assert.True(p.ScaleBy(0.0, origin).IsEqualTo(origin));

            // Scale by negative factor (-1.0): Symmetric point in opposite direction across center
            Assert.True(p.ScaleBy(-1.0, origin).IsEqualTo(new GeoPoint(-1.0, -3.0)));
        }
    }
}

