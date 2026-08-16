using System;
using Xunit;
using ArrangeAlgorithms.Geometry;

namespace ArrangeAlgorithms.UnitTest
{
    public class VectorTests
    {
        [Fact]
        public void Vector_BasicOperations_WorkCorrectly()
        {
            var v1 = new GeoVector(3.0, 4.0);
            Assert.Equal(5.0, v1.Length, 12);
            Assert.Equal(25.0, v1.LengthSquared, 12);

            Assert.True(v1.TryGetNormal(out var unit));
            Assert.Equal(1.0, unit.Length, 12);
            Assert.Equal(new GeoVector(0.6, 0.8), unit);

            var v2 = new GeoVector(1.0, -2.0);
            var sum = v1 + v2;
            Assert.Equal(new GeoVector(4.0, 2.0), sum);

            var scaled = v1 * 2.0;
            Assert.Equal(new GeoVector(6.0, 8.0), scaled);
        }

        [Fact]
        public void Vector_AnglesAndProducts_WorkCorrectly()
        {
            var v1 = GeoVector.XAxis;
            var v2 = GeoVector.YAxis;

            Assert.Equal(0.0, v1.DotProduct(v2), 12);
            Assert.Equal(1.0, v1.CrossProduct(v2), 12);

            Assert.Equal(Math.PI / 2.0, v1.GetAngleTo(v2), 12);
            Assert.Equal(Math.PI / 2.0, v1.GetSignedAngleTo(v2), 12);
            Assert.Equal(-Math.PI / 2.0, v2.GetSignedAngleTo(v1), 12);

            Assert.True(v1.IsPerpendicularTo(v2));
            Assert.False(v1.IsParallelTo(v2));
        }

        [Fact]
        public void Vector_ZeroLengthNormalization_ReturnsFalseAndZero()
        {
            var zeroVec = GeoVector.Zero;
            Assert.False(zeroVec.TryGetNormal(out var normal));
            Assert.Equal(GeoVector.Zero, normal);
        }

        [Fact]
        public void Vector_DotAndCrossProductEdgeCases_WorkCorrectly()
        {
            var v1 = new GeoVector(3.0, 4.0);

            // Tích vô hướng với chính nó = bình phương độ dài
            Assert.Equal(v1.LengthSquared, v1.DotProduct(v1), 12);

            // Tích vô hướng với GeoVector ngược chiều = số âm của bình phương độ dài
            var vInv = v1 * -1.0;
            Assert.Equal(-v1.LengthSquared, v1.DotProduct(vInv), 12);

            // Tích có hướng của 2 GeoVector song song phải bằng 0
            Assert.Equal(0.0, v1.CrossProduct(vInv), 12);
            Assert.Equal(0.0, v1.CrossProduct(v1), 12);
        }

        [Fact]
        public void Vector_AngleEdgeCases_WorkCorrectly()
        {
            var v1 = new GeoVector(2.0, 2.0);
            var v2 = new GeoVector(-2.0, -2.0); // Ngược chiều

            // Góc giữa 2 GeoVector ngược chiều là PI (180 độ)
            Assert.Equal(Math.PI, v1.GetAngleTo(v2), 12);

            // Góc giữa 2 GeoVector cùng chiều là 0
            Assert.Equal(0.0, v1.GetAngleTo(v1), 12);
        }

        [Fact]
        public void Vector_ParallelAndPerpendicularTolerance_WorksCorrectly()
        {
            var v1 = new GeoVector(1.0, 0.0);
            // Một GeoVector rất gần vuông góc (lệch một lượng cực nhỏ 1e-10)
            var vNearlyPerpendicular = new GeoVector(1e-10, 1.0);

            Assert.True(v1.IsPerpendicularTo(vNearlyPerpendicular, new Tolerance(1e-9, 1e-9)));
            Assert.False(v1.IsPerpendicularTo(vNearlyPerpendicular, new Tolerance(1e-11, 1e-11)));

            // Một GeoVector rất gần song song
            var vNearlyParallel = new GeoVector(1.0, 1e-10);
            Assert.True(v1.IsParallelTo(vNearlyParallel, new Tolerance(1e-9, 1e-9)));
            Assert.False(v1.IsParallelTo(vNearlyParallel, new Tolerance(1e-11, 1e-11)));
        }
    }
}

