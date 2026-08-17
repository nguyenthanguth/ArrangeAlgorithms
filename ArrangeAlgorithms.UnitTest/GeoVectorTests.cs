using ArrangeAlgorithms.Geometry;
using System;
using Xunit;

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

        [Fact]
        public void Vector_RotateBy_RotatesCounterClockwise()
        {
            var v = GeoVector.XAxis;

            Assert.True(v.RotateBy(Math.PI / 2.0).IsEqualTo(GeoVector.YAxis));
            Assert.True(v.RotateBy(Math.PI).IsEqualTo(new GeoVector(-1.0, 0.0)));
            Assert.True(v.RotateBy(2.0 * Math.PI).IsEqualTo(v));
            Assert.True(v.RotateBy(0.0).IsEqualTo(v));
        }

        [Fact]
        public void Vector_RotateBy_PreservesLength()
        {
            var v = new GeoVector(3.0, 4.0);
            var rotated = v.RotateBy(0.7);

            Assert.Equal(v.Length, rotated.Length, 12);
        }

        [Fact]
        public void Vector_GetPerpendicularVector_TurnsNinetyDegreesCounterClockwise()
        {
            var v = new GeoVector(3.0, 4.0);
            var perpendicular = v.GetPerpendicularVector();

            Assert.Equal(new GeoVector(-4.0, 3.0), perpendicular);
            Assert.Equal(0.0, v.DotProduct(perpendicular), 12);
            Assert.Equal(v.Length, perpendicular.Length, 12);
        }

        [Fact]
        public void Vector_Operators_WorkCorrectly()
        {
            var v = new GeoVector(4.0, -6.0);

            Assert.Equal(new GeoVector(-4.0, 6.0), -v);
            Assert.Equal(new GeoVector(2.0, -3.0), v / 2.0);
            Assert.Equal(new GeoVector(8.0, -12.0), 2.0 * v);
            Assert.Equal(new GeoVector(8.0, -12.0), v * 2.0);
            Assert.Equal(new GeoVector(3.0, -6.0), v - new GeoVector(1.0, 0.0));
        }

        [Fact]
        public void Vector_Equality_AndHashCode_WorkCorrectly()
        {
            var a = new GeoVector(1.5, -2.5);
            var b = new GeoVector(1.5, -2.5);
            var c = new GeoVector(1.5, 2.5);

            Assert.True(a == b);
            Assert.False(a != b);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
            object notAVector = "không phải GeoVector";
            Assert.True(a.Equals((object)b));
            Assert.False(a.Equals(notAVector));

            Assert.True(a != c);
        }

        [Fact]
        public void Vector_Normalize_ThrowsForZeroLength()
        {
            Assert.Throws<InvalidOperationException>(() => GeoVector.Zero.Normalize());
        }

        [Fact]
        public void Vector_AngleFromZeroLengthVector_Throws()
        {
            var v = GeoVector.XAxis;

            Assert.Throws<InvalidOperationException>(() => GeoVector.Zero.GetAngleTo(v));
            Assert.Throws<InvalidOperationException>(() => v.GetAngleTo(GeoVector.Zero));
            Assert.Throws<InvalidOperationException>(() => GeoVector.Zero.GetSignedAngleTo(v));
            Assert.Throws<InvalidOperationException>(() => v.GetSignedAngleTo(GeoVector.Zero));
        }

        [Fact]
        public void Vector_GetAngleTo_StaysAccurateNearCollinear()
        {
            // Acos mất chính xác nghiêm trọng ở lân cận ±1; công thức Atan2 phải giữ được đủ số chữ số.
            var v = new GeoVector(2.0, 2.0);

            Assert.Equal(Math.PI, v.GetAngleTo(-v), 12);
            Assert.Equal(0.0, v.GetAngleTo(v * 3.0), 12);
            Assert.Equal(Math.PI, v.GetAngleTo(v * -7.5), 12);
        }

        [Fact]
        public void Vector_GetAngleTo_IsSymmetric_ButSignedAngleIsNot()
        {
            var a = new GeoVector(1.0, 0.0);
            var b = new GeoVector(1.0, 1.0);

            Assert.Equal(a.GetAngleTo(b), b.GetAngleTo(a), 12);
            Assert.Equal(Math.PI / 4.0, a.GetAngleTo(b), 12);

            Assert.Equal(Math.PI / 4.0, a.GetSignedAngleTo(b), 12);
            Assert.Equal(-Math.PI / 4.0, b.GetSignedAngleTo(a), 12);
        }
    }
}

