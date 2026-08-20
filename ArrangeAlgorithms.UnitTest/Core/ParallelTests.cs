using System;
using ArrangeAlgorithms.Geometry;
using ArrangeAlgorithms.Core;
using Xunit;

namespace ArrangeAlgorithms.UnitTest.Core
{
    public class ParallelTests
    {
        #region Vector - Vector Tests

        [Fact]
        public void VectorVector_ParallelAndPerpendicular()
        {
            var v1 = new GeoVector(1, 0);

            // Same direction -> Parallel
            var vSame = new GeoVector(5, 0);
            Assert.True(Parallel.IsParallel(v1, vSame));
            Assert.False(Parallel.IsPerpendicular(v1, vSame));

            // Opposite direction -> Parallel
            var vOpposite = new GeoVector(-3, 0);
            Assert.True(Parallel.IsParallel(v1, vOpposite));
            Assert.False(Parallel.IsPerpendicular(v1, vOpposite));

            // Perpendicular -> 90 degrees
            var vPerp = new GeoVector(0, 4);
            Assert.False(Parallel.IsParallel(v1, vPerp));
            Assert.True(Parallel.IsPerpendicular(v1, vPerp));
        }

        [Fact]
        public void VectorVector_AngleTolerance()
        {
            var v1 = new GeoVector(1.0, 0.0);
            var vDeviated = new GeoVector(1.0, 1e-4); // ~1e-4 radians deviation

            // Within 1e-3 rad tolerance -> True
            Assert.True(Parallel.IsParallel(v1, vDeviated, new Tolerance(1e-9, 1e-9, 1e-3)));

            // Outside 1e-5 rad tolerance -> False
            Assert.False(Parallel.IsParallel(v1, vDeviated, new Tolerance(1e-9, 1e-9, 1e-5)));
        }

        #endregion

        #region Line - Line Tests

        [Fact]
        public void LineLine_ParallelAndPerpendicular()
        {
            var l1 = new GeoLine(new GeoPoint(0, 0), new GeoPoint(10, 0));

            // Parallel line
            var lParallel = new GeoLine(new GeoPoint(0, 5), new GeoPoint(10, 5));
            Assert.True(Parallel.IsParallel(l1, lParallel));
            Assert.False(Parallel.IsPerpendicular(l1, lParallel));

            // Perpendicular line
            var lPerp = new GeoLine(new GeoPoint(5, -5), new GeoPoint(5, 5));
            Assert.False(Parallel.IsParallel(l1, lPerp));
            Assert.True(Parallel.IsPerpendicular(l1, lPerp));

            // Skew 45-degree line
            var lSkew = new GeoLine(new GeoPoint(0, 0), new GeoPoint(10, 10));
            Assert.False(Parallel.IsParallel(l1, lSkew));
            Assert.False(Parallel.IsPerpendicular(l1, lSkew));
        }

        [Fact]
        public void LineLine_OppositeDirections_Parallel()
        {
            var l1 = new GeoLine(new GeoPoint(0, 0), new GeoPoint(10, 0));
            var lOpposite = new GeoLine(new GeoPoint(10, 5), new GeoPoint(0, 5));

            Assert.True(Parallel.IsParallel(l1, lOpposite));
        }

        [Fact]
        public void LineVector_ParallelAndPerpendicular()
        {
            var line = new GeoLine(new GeoPoint(0, 0), new GeoPoint(10, 0));

            var vParallel = new GeoVector(2, 0);
            Assert.True(Parallel.IsParallel(line, vParallel));
            Assert.False(Parallel.IsPerpendicular(line, vParallel));

            var vPerp = new GeoVector(0, -3);
            Assert.False(Parallel.IsParallel(line, vPerp));
            Assert.True(Parallel.IsPerpendicular(line, vPerp));
        }

        #endregion

        #region Rectangle - Shapes Tests

        [Fact]
        public void RectangleLine_ParallelToAxes()
        {
            var rect = new GeoRectangle(new GeoPoint(0, 0), 20, 10, 0);

            // Parallel to width axis
            var lWidth = new GeoLine(new GeoPoint(0, 20), new GeoPoint(10, 20));
            Assert.True(Parallel.IsParallel(rect, lWidth));

            // Parallel to height axis
            var lHeight = new GeoLine(new GeoPoint(30, 0), new GeoPoint(30, 10));
            Assert.True(Parallel.IsParallel(rect, lHeight));

            // Skew line
            var lSkew = new GeoLine(new GeoPoint(0, 0), new GeoPoint(10, 10));
            Assert.False(Parallel.IsParallel(rect, lSkew));
        }

        [Fact]
        public void RectangleLine_RotatedRectangle()
        {
            // Rectangle rotated 45 degrees
            var rect = new GeoRectangle(new GeoPoint(0, 0), 20, 10, Math.PI / 4.0);

            // Line oriented at 45 degrees -> parallel to rectangle local X axis
            var l45 = new GeoLine(new GeoPoint(0, 0), new GeoPoint(10, 10));
            Assert.True(Parallel.IsParallel(rect, l45));

            // Horizontal line -> not parallel to rotated rectangle
            var lHoriz = new GeoLine(new GeoPoint(0, 0), new GeoPoint(10, 0));
            Assert.False(Parallel.IsParallel(rect, lHoriz));
        }

        [Fact]
        public void RectangleRectangle_ParallelAlignments()
        {
            var r1 = new GeoRectangle(new GeoPoint(0, 0), 20, 10, 0);

            // Same angle 0 rad
            var rSame = new GeoRectangle(new GeoPoint(50, 50), 30, 15, 0);
            Assert.True(Parallel.IsParallel(r1, rSame));

            // Rotated 90 deg -> axes still parallel
            var r90 = new GeoRectangle(new GeoPoint(50, 50), 30, 15, Math.PI / 2.0);
            Assert.True(Parallel.IsParallel(r1, r90));

            // Rotated 180 deg -> axes still parallel
            var r180 = new GeoRectangle(new GeoPoint(50, 50), 30, 15, Math.PI);
            Assert.True(Parallel.IsParallel(r1, r180));
        }

        [Fact]
        public void RectangleRectangle_RotatedNonParallel()
        {
            var r1 = new GeoRectangle(new GeoPoint(0, 0), 20, 10, 0);

            // Rotated 30 deg
            var r30 = new GeoRectangle(new GeoPoint(50, 50), 30, 15, Math.PI / 6.0);
            Assert.False(Parallel.IsParallel(r1, r30));

            // Rotated 45 deg
            var r45 = new GeoRectangle(new GeoPoint(50, 50), 30, 15, Math.PI / 4.0);
            Assert.False(Parallel.IsParallel(r1, r45));
        }

        #endregion

        #region Edge Cases & Properties

        [Fact]
        public void Parallel_SymmetryProperties()
        {
            var v1 = new GeoVector(3, 4);
            var v2 = new GeoVector(6, 8);

            Assert.Equal(Parallel.IsParallel(v1, v2), Parallel.IsParallel(v2, v1));
            Assert.Equal(Parallel.IsPerpendicular(v1, v2), Parallel.IsPerpendicular(v2, v1));

            var l1 = new GeoLine(new GeoPoint(0, 0), new GeoPoint(3, 4));
            var l2 = new GeoLine(new GeoPoint(1, 1), new GeoPoint(4, 5));
            Assert.Equal(Parallel.IsParallel(l1, l2), Parallel.IsParallel(l2, l1));
        }

        [Fact]
        public void ZeroLengthLine_ParallelDegenerate()
        {
            var degenerateLine = new GeoLine(new GeoPoint(0, 0), new GeoPoint(0, 0));
            var normalLine = new GeoLine(new GeoPoint(0, 0), new GeoPoint(10, 0));

            Assert.False(Parallel.IsParallel(degenerateLine, normalLine));
            Assert.False(Parallel.IsPerpendicular(degenerateLine, normalLine));
        }

        [Fact]
        public void Parallel_CustomToleranceThresholds()
        {
            var v1 = new GeoVector(1, 0);
            var vSmallDev = new GeoVector(Math.Cos(0.005), Math.Sin(0.005));

            var tolStrict = new Tolerance(1e-9, 1e-9, 0.001);
            var tolLoose = new Tolerance(1e-9, 1e-9, 0.01);

            Assert.False(Parallel.IsParallel(v1, vSmallDev, tolStrict));
            Assert.True(Parallel.IsParallel(v1, vSmallDev, tolLoose));
        }

        [Fact]
        public void LineRectangle_Parallel_InstanceMethodSymmetry()
        {
            var rect = new GeoRectangle(new GeoPoint(0, 0), 20, 10, 0.0);
            var lineParX = new GeoLine(new GeoPoint(0, 5), new GeoPoint(10, 5));
            var lineParY = new GeoLine(new GeoPoint(5, 0), new GeoPoint(5, 10));
            var lineDiag = new GeoLine(new GeoPoint(0, 0), new GeoPoint(10, 10));

            // Test line.IsParallelTo(rect) and rect.IsParallelTo(line)
            Assert.True(lineParX.IsParallelTo(rect));
            Assert.True(rect.IsParallelTo(lineParX));

            Assert.True(lineParY.IsParallelTo(rect));
            Assert.True(rect.IsParallelTo(lineParY));

            Assert.False(lineDiag.IsParallelTo(rect));
            Assert.False(rect.IsParallelTo(lineDiag));

            // With tolerance
            var lineNearPar = new GeoLine(new GeoPoint(0, 0), new GeoPoint(10, 0.01));
            var tolStrict = new Tolerance(1e-9, 1e-9, 0.0001);
            var tolLoose = new Tolerance(1e-9, 1e-9, 0.01);
            Assert.False(lineNearPar.IsParallelTo(rect, tolStrict));
            Assert.True(lineNearPar.IsParallelTo(rect, tolLoose));
        }

        #endregion
        #region Numerical Stability

        [Fact]
        public void IsParallel_StaysAccurateForNearlyCollinearVectors()
        {
            // Acos loses precision exactly here, which is why the check uses Atan2 of cross over dot.
            var baseline = new GeoVector(1, 0);

            // A hair under the 1 degree threshold is parallel, a hair over is not.
            double justUnder = 0.9 * Math.PI / 180.0;
            double justOver = 1.1 * Math.PI / 180.0;

            Assert.True(Parallel.IsParallel(baseline, new GeoVector(Math.Cos(justUnder), Math.Sin(justUnder))));
            Assert.False(Parallel.IsParallel(baseline, new GeoVector(Math.Cos(justOver), Math.Sin(justOver))));

            // Anti-parallel is parallel, and the same threshold applies around 180 degrees.
            Assert.True(Parallel.IsParallel(baseline, new GeoVector(-1, 0)));
            Assert.True(Parallel.IsParallel(baseline, new GeoVector(-Math.Cos(justUnder), Math.Sin(justUnder))));
            Assert.False(Parallel.IsParallel(baseline, new GeoVector(-Math.Cos(justOver), Math.Sin(justOver))));
        }

        [Fact]
        public void IsParallel_IsScaleIndependent()
        {
            // Both magnitudes stay above EqualVector, so neither counts as a zero vector.
            var small = new GeoVector(1e-2, 0);
            var large = new GeoVector(1e8, 0);

            Assert.True(Parallel.IsParallel(small, large));
            Assert.True(Parallel.IsPerpendicular(small, new GeoVector(0, 1e8)));
        }

        [Fact]
        public void IsParallel_TreatsVectorsShorterThanEqualVectorAsZero()
        {
            // A vector below the zero-length threshold has no usable direction, so it is neither parallel
            // nor perpendicular to anything.
            var belowThreshold = new GeoVector(1e-6, 0);

            Assert.False(Parallel.IsParallel(belowThreshold, new GeoVector(1, 0)));
            Assert.False(Parallel.IsPerpendicular(belowThreshold, new GeoVector(0, 1)));

            // Loosening EqualVector far enough brings it back into play.
            var fine = new Tolerance(1e-9, 1e-9);
            Assert.True(Parallel.IsParallel(belowThreshold, new GeoVector(1, 0), fine));
            Assert.True(Parallel.IsPerpendicular(belowThreshold, new GeoVector(0, 1), fine));
        }

        #endregion
    }
}
