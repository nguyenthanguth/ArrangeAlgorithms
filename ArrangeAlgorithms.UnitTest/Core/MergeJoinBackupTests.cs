using System;
using System.Collections.Generic;
using ArrangeAlgorithms.Core;
using ArrangeAlgorithms.Geometry;
using Xunit;

namespace ArrangeAlgorithms.UnitTest.Core
{
    /// <summary>
    /// Covers <see cref="Merge.JoinBackup"/>, both on its own and against <see cref="Merge.Join"/>.
    /// </summary>
    /// <remarks>
    /// The two are meant to answer alike, so most of the ground is covered by holding one against the
    /// other over a spread of inputs rather than by writing every case out twice. What is written out
    /// is what a shared answer would not catch: the two agreeing on something that is wrong.
    /// <para>
    /// Runs are compared without regard to direction. Neither implementation promises one, and they do
    /// in fact differ: the pairwise sweep tends to hand back the run trailing from whichever piece it
    /// happened to keep, where growing from a seed hands back the direction of the seed.
    /// </para>
    /// </remarks>
    public class MergeJoinBackupTests
    {
        #region Argument validation

        [Fact]
        public void JoinBackup_NullLines_Throws()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => Merge.JoinBackup(null, Tolerance.Global));
            Assert.Equal("lines", ex.ParamName);
        }

        #endregion

        #region Degenerate input

        [Fact]
        public void JoinBackup_EmptyInput_ReturnsNothing()
        {
            Assert.Empty(Merge.JoinBackup(new List<GeoLine>(), Tolerance.Global));
        }

        [Fact]
        public void JoinBackup_LoneZeroLengthSegment_ReturnsNothing()
        {
            // The original had no guard here and handed back a polyline with two identical vertices,
            // which the public GeoPolyline constructor refuses. The guard came with the rewrite.
            var segments = new[] { new GeoLine(new GeoPoint(3, 4), new GeoPoint(3, 4)) };

            Assert.Empty(Merge.JoinBackup(segments, Tolerance.Global));
        }

        [Fact]
        public void JoinBackup_AllSegmentsZeroLength_ReturnsNothing()
        {
            var segments = new[]
            {
                new GeoLine(new GeoPoint(0, 0), new GeoPoint(0, 0)),
                new GeoLine(new GeoPoint(5, 5), new GeoPoint(5, 5))
            };

            Assert.Empty(Merge.JoinBackup(segments, Tolerance.Global));
        }

        [Fact]
        public void JoinBackup_ZeroLengthSegmentAmongRealOnes_IsIgnored()
        {
            var segments = new[]
            {
                new GeoLine(new GeoPoint(0, 0), new GeoPoint(5, 0)),
                new GeoLine(new GeoPoint(5, 0), new GeoPoint(5, 0)),   // sits on the junction
                new GeoLine(new GeoPoint(5, 0), new GeoPoint(5, 5))
            };

            var result = Merge.JoinBackup(segments, Tolerance.Global);

            Assert.Single(result);
            AssertRun(result[0], new GeoPoint(0, 0), new GeoPoint(5, 0), new GeoPoint(5, 5));
        }

        [Fact]
        public void JoinBackup_EveryResultHasDistinctNeighbouringVertices()
        {
            foreach (var segments in AwkwardShapes())
            {
                foreach (var polyline in Merge.JoinBackup(segments, Tolerance.Global))
                {
                    Assert.True(polyline.VertexCount >= 2, "a run reached the output with fewer than 2 vertices");
                    for (int i = 1; i < polyline.VertexCount; i++)
                    {
                        Assert.False(
                            polyline[i].IsEqualTo(polyline[i - 1], Tolerance.Global),
                            "vertices " + (i - 1) + " and " + i + " coincide: " + polyline[i]);
                    }
                }
            }
        }

        #endregion

        #region Joining

        [Fact]
        public void JoinBackup_CollinearTouching_MergesAndSimplifies()
        {
            var segments = new[]
            {
                new GeoLine(new GeoPoint(0, 0), new GeoPoint(5, 0)),
                new GeoLine(new GeoPoint(5, 0), new GeoPoint(12, 0)),
                new GeoLine(new GeoPoint(12, 0), new GeoPoint(17, 0))
            };

            var result = Merge.JoinBackup(segments, Tolerance.Global);

            Assert.Single(result);
            AssertRun(result[0], new GeoPoint(0, 0), new GeoPoint(17, 0));
        }

        [Fact]
        public void JoinBackup_CollinearFoldingBack_KeepsTheTurningPoint()
        {
            var segments = new[]
            {
                new GeoLine(new GeoPoint(0, 0), new GeoPoint(5, 0)),
                new GeoLine(new GeoPoint(5, 0), new GeoPoint(12, 0)),
                new GeoLine(new GeoPoint(12, 0), new GeoPoint(7, 0))   // doubles back
            };

            var result = Merge.JoinBackup(segments, Tolerance.Global);

            Assert.Single(result);
            AssertRun(result[0], new GeoPoint(0, 0), new GeoPoint(12, 0), new GeoPoint(7, 0));
            Assert.Equal(17.0, result[0].Length, 9);
        }

        [Fact]
        public void JoinBackup_NonCollinearTouching_KeepsTheBends()
        {
            var segments = new[]
            {
                new GeoLine(new GeoPoint(0, 0), new GeoPoint(5, 0)),
                new GeoLine(new GeoPoint(5, 0), new GeoPoint(5, 5)),
                new GeoLine(new GeoPoint(10, 5), new GeoPoint(5, 5))
            };

            var result = Merge.JoinBackup(segments, Tolerance.Global);

            Assert.Single(result);
            AssertRun(result[0], new GeoPoint(0, 0), new GeoPoint(5, 0), new GeoPoint(5, 5), new GeoPoint(10, 5));
        }

        [Fact]
        public void JoinBackup_SegmentsPointingTheWrongWay_AreTurnedRound()
        {
            var segments = new[]
            {
                new GeoLine(new GeoPoint(0, 0), new GeoPoint(5, 0)),
                new GeoLine(new GeoPoint(5, 5), new GeoPoint(5, 0)),
                new GeoLine(new GeoPoint(10, 5), new GeoPoint(5, 5))
            };

            var result = Merge.JoinBackup(segments, Tolerance.Global);

            Assert.Single(result);
            AssertRun(result[0], new GeoPoint(0, 0), new GeoPoint(5, 0), new GeoPoint(5, 5), new GeoPoint(10, 5));
        }

        [Fact]
        public void JoinBackup_WithGaps_DoesNotMerge()
        {
            var segments = new[]
            {
                new GeoLine(new GeoPoint(0, 0), new GeoPoint(5, 0)),
                new GeoLine(new GeoPoint(7, 0), new GeoPoint(12, 0)),
                new GeoLine(new GeoPoint(15, 0), new GeoPoint(20, 0))
            };

            var result = Merge.JoinBackup(segments, Tolerance.Global);

            Assert.Equal(3, result.Length);
            foreach (var polyline in result)
            {
                Assert.Equal(2, polyline.VertexCount);
            }
        }

        [Fact]
        public void JoinBackup_ClosedSquare_ComesBackAsOneClosedRun()
        {
            var segments = new[]
            {
                new GeoLine(new GeoPoint(0, 0), new GeoPoint(10, 0)),
                new GeoLine(new GeoPoint(10, 0), new GeoPoint(10, 10)),
                new GeoLine(new GeoPoint(10, 10), new GeoPoint(0, 10)),
                new GeoLine(new GeoPoint(0, 10), new GeoPoint(0, 0))
            };

            var result = Merge.JoinBackup(segments, Tolerance.Global);

            Assert.Single(result);
            Assert.Equal(5, result[0].VertexCount);
            Assert.True(result[0][0].IsEqualTo(result[0][4], Tolerance.Global), "the run should close on itself");
            Assert.Equal(40.0, result[0].Length, 9);
        }

        #endregion

        #region Tolerance

        [Fact]
        public void JoinBackup_GapWithinTolerance_IsBridged()
        {
            var segments = new[]
            {
                new GeoLine(new GeoPoint(0, 0), new GeoPoint(5, 0)),
                new GeoLine(new GeoPoint(5.5, 0), new GeoPoint(5.5, 5))
            };

            Assert.Single(Merge.JoinBackup(segments, new Tolerance(1.0, 1.0)));
            Assert.Equal(2, Merge.JoinBackup(segments, new Tolerance(0.1, 0.1)).Length);
        }

        [Fact]
        public void JoinBackup_ZeroTolerance_MatchesExactEndpointsOnly()
        {
            var exact = new[]
            {
                new GeoLine(new GeoPoint(0, 0), new GeoPoint(5, 0)),
                new GeoLine(new GeoPoint(5, 0), new GeoPoint(5, 5))
            };
            var nearMiss = new[]
            {
                new GeoLine(new GeoPoint(0, 0), new GeoPoint(5, 0)),
                new GeoLine(new GeoPoint(5, 1E-12), new GeoPoint(5, 5))
            };

            Assert.Single(Merge.JoinBackup(exact, new Tolerance(0.0, 0.0)));
            Assert.Equal(2, Merge.JoinBackup(nearMiss, new Tolerance(0.0, 0.0)).Length);
        }

        #endregion

        #region Agreement with Join

        [Fact]
        public void JoinBackup_AgreesWithJoin_OnAwkwardShapes()
        {
            foreach (var segments in AwkwardShapes())
            {
                Assert.Equal(
                    Canonical(Merge.Join(segments, Tolerance.Global)),
                    Canonical(Merge.JoinBackup(segments, Tolerance.Global)));
            }
        }

        [Fact]
        public void JoinBackup_AgreesWithJoin_AtAFork()
        {
            // Where three segments meet, both are free to take either branch, and neither promises
            // which. They happen to make the same choice, and this is here to notice if that stops
            // being true rather than to require it of them.
            var segments = new[]
            {
                new GeoLine(new GeoPoint(0, 0), new GeoPoint(5, 0)),
                new GeoLine(new GeoPoint(5, 0), new GeoPoint(10, 0)),
                new GeoLine(new GeoPoint(5, 0), new GeoPoint(5, 5))
            };

            Assert.Equal(
                Canonical(Merge.Join(segments, Tolerance.Global)),
                Canonical(Merge.JoinBackup(segments, Tolerance.Global)));
        }

        [Fact]
        public void JoinBackup_AgreesWithJoin_OnRandomChains()
        {
            for (int trial = 0; trial < 200; trial++)
            {
                var segments = RandomChains(trial);

                Assert.Equal(
                    Canonical(Merge.Join(segments, Tolerance.Global)),
                    Canonical(Merge.JoinBackup(segments, Tolerance.Global)));
            }
        }

        [Fact]
        public void JoinBackup_AgreesWithJoin_AtSeveralTolerances()
        {
            // Each tolerance is paired with a scale that keeps it well under the shortest step in the
            // geometry. A tolerance that reaches as far as the features themselves is a different
            // question, and is asked separately below.
            var cases = new[]
            {
                new { Tolerance = new Tolerance(0.0, 0.0), Scale = 1.0 },
                new { Tolerance = new Tolerance(1E-6, 1E-6), Scale = 1.0 },
                new { Tolerance = Tolerance.Global, Scale = 1.0 },
                new { Tolerance = new Tolerance(0.75, 0.75), Scale = 1.0 },   // shortest step is 2
                new { Tolerance = new Tolerance(3.0, 3.0), Scale = 10.0 },    // shortest step is 20
                new { Tolerance = new Tolerance(30.0, 30.0), Scale = 100.0 }  // shortest step is 200
            };

            foreach (var one in cases)
            {
                for (int trial = 0; trial < 40; trial++)
                {
                    var segments = RandomChains(trial, one.Scale);
                    Assert.Equal(
                        Canonical(Merge.Join(segments, one.Tolerance)),
                        Canonical(Merge.JoinBackup(segments, one.Tolerance)));
                }
            }

            // The fixed shapes have features from 4 units upwards, so they take the finer tolerances.
            foreach (var one in cases)
            {
                if (one.Scale != 1.0)
                {
                    continue;
                }

                foreach (var segments in AwkwardShapes())
                {
                    Assert.Equal(
                        Canonical(Merge.Join(segments, one.Tolerance)),
                        Canonical(Merge.JoinBackup(segments, one.Tolerance)));
                }
            }
        }

        [Fact]
        public void JoinBackup_ToleranceReachingAsFarAsTheFeatures_SwallowsSegmentsInBoth()
        {
            // Once the tolerance is as long as the segments, a chain stops being a chain: short
            // segments count as zero length and go, and vertices that were distinct start matching
            // one another, which turns the run into a thicket of forks. Neither implementation
            // promises which branch it takes at a fork, so their answers may part company here.
            // What they must still agree on is that nothing is invented and nothing invalid escapes.
            var tolerance = new Tolerance(3.0, 3.0);

            for (int trial = 0; trial < 40; trial++)
            {
                var segments = RandomChains(trial);   // steps of 2 to 5, so the tolerance reaches across them

                foreach (var result in new[] { Merge.Join(segments, tolerance), Merge.JoinBackup(segments, tolerance) })
                {
                    foreach (var polyline in result)
                    {
                        Assert.True(polyline.VertexCount >= 2);
                        for (int i = 1; i < polyline.VertexCount; i++)
                        {
                            Assert.False(polyline[i].IsEqualTo(polyline[i - 1], tolerance));
                        }
                    }
                }
            }
        }

        [Fact]
        public void JoinBackup_AgreesWithJoin_OnTotalLength()
        {
            for (int trial = 0; trial < 50; trial++)
            {
                var segments = RandomChains(trial);

                double join = 0.0;
                foreach (var polyline in Merge.Join(segments, Tolerance.Global))
                {
                    join += polyline.Length;
                }

                double backup = 0.0;
                foreach (var polyline in Merge.JoinBackup(segments, Tolerance.Global))
                {
                    backup += polyline.Length;
                }

                double input = 0.0;
                foreach (var segment in segments)
                {
                    input += segment.Length;
                }

                Assert.Equal(input, join, 9);
                Assert.Equal(input, backup, 9);
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// The shapes that stress the two implementations differently: folding back, closing up,
        /// pointing the wrong way, standing apart, and carrying a degenerate segment.
        /// </summary>
        private static IEnumerable<GeoLine[]> AwkwardShapes()
        {
            yield return new GeoLine[0];

            yield return new[] { new GeoLine(new GeoPoint(3, 4), new GeoPoint(3, 4)) };

            yield return new[]
            {
                new GeoLine(new GeoPoint(0, 0), new GeoPoint(5, 0)),
                new GeoLine(new GeoPoint(5, 0), new GeoPoint(0, 0))          // straight back on itself
            };

            yield return new[]
            {
                new GeoLine(new GeoPoint(0, 0), new GeoPoint(5, 0)),
                new GeoLine(new GeoPoint(5, 0), new GeoPoint(12, 0)),
                new GeoLine(new GeoPoint(12, 0), new GeoPoint(7, 0))         // folds back
            };

            yield return new[]
            {
                new GeoLine(new GeoPoint(0, 0), new GeoPoint(10, 0)),
                new GeoLine(new GeoPoint(10, 0), new GeoPoint(10, 10)),
                new GeoLine(new GeoPoint(10, 10), new GeoPoint(0, 10)),
                new GeoLine(new GeoPoint(0, 10), new GeoPoint(0, 0))         // closed square
            };

            yield return new[]
            {
                new GeoLine(new GeoPoint(0, 0), new GeoPoint(4, 0)),
                new GeoLine(new GeoPoint(4, 0), new GeoPoint(4, 4)),
                new GeoLine(new GeoPoint(4, 4), new GeoPoint(0, 0)),         // closed triangle
                new GeoLine(new GeoPoint(0, 0), new GeoPoint(0, 0))          // degenerate on the seam
            };

            yield return new[]
            {
                new GeoLine(new GeoPoint(0, 0), new GeoPoint(5, 0)),
                new GeoLine(new GeoPoint(5, 5), new GeoPoint(5, 0)),         // stored backwards
                new GeoLine(new GeoPoint(10, 5), new GeoPoint(5, 5))         // stored backwards
            };

            yield return new[]
            {
                new GeoLine(new GeoPoint(0, 0), new GeoPoint(5, 0)),
                new GeoLine(new GeoPoint(7, 0), new GeoPoint(12, 0)),
                new GeoLine(new GeoPoint(15, 0), new GeoPoint(20, 0))        // gaps
            };

            yield return new[]
            {
                new GeoLine(new GeoPoint(5, 0), new GeoPoint(5, 5)),
                new GeoLine(new GeoPoint(0, 0), new GeoPoint(5, 0)),
                new GeoLine(new GeoPoint(5, 5), new GeoPoint(10, 5))         // seed lands in the middle
            };
        }

        /// <summary>
        /// Builds separated chains of alternating horizontal and vertical steps, shuffled and half of
        /// them stored backwards, so nothing about the input order or direction is settled.
        /// </summary>
        private static GeoLine[] RandomChains(int seed)
        {
            return RandomChains(seed, 1.0);
        }

        /// <summary>
        /// The same chains enlarged, so that a coarse tolerance can be tried against geometry whose
        /// features still stand well clear of it.
        /// </summary>
        private static GeoLine[] RandomChains(int seed, double scale)
        {
            var random = new Random(seed);
            var segments = new List<GeoLine>();

            int chains = 1 + random.Next(4);
            for (int c = 0; c < chains; c++)
            {
                // Chains are set far enough apart that none of them can touch another.
                var at = new GeoPoint(c * 1000.0 * scale, c * 1000.0 * scale);
                var vertices = new List<GeoPoint> { at };

                int steps = 1 + random.Next(8);
                for (int s = 0; s < steps; s++)
                {
                    double step = (1.0 + random.Next(1, 5)) * scale;
                    at = (s % 2 == 0) ? new GeoPoint(at.X + step, at.Y) : new GeoPoint(at.X, at.Y + step);
                    vertices.Add(at);
                }

                for (int v = 1; v < vertices.Count; v++)
                {
                    segments.Add(random.Next(2) == 0
                        ? new GeoLine(vertices[v - 1], vertices[v])
                        : new GeoLine(vertices[v], vertices[v - 1]));
                }
            }

            for (int i = segments.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                var swap = segments[i];
                segments[i] = segments[j];
                segments[j] = swap;
            }

            return segments.ToArray();
        }

        /// <summary>
        /// Describes a set of runs so that two sets can be compared without reading anything into the
        /// order they arrived in or the direction each one happens to run.
        /// </summary>
        private static string Canonical(GeoPolyline[] polylines)
        {
            var rows = new List<string>(polylines.Length);
            foreach (var polyline in polylines)
            {
                var forward = new System.Text.StringBuilder();
                for (int i = 0; i < polyline.VertexCount; i++)
                {
                    Append(forward, polyline[i]);
                }

                var backward = new System.Text.StringBuilder();
                for (int i = polyline.VertexCount - 1; i >= 0; i--)
                {
                    Append(backward, polyline[i]);
                }

                string a = forward.ToString();
                string b = backward.ToString();
                rows.Add(StringComparer.Ordinal.Compare(a, b) <= 0 ? a : b);
            }

            rows.Sort(StringComparer.Ordinal);
            return string.Join("|", rows.ToArray());
        }

        private static void Append(System.Text.StringBuilder text, GeoPoint point)
        {
            text.Append(point.X.ToString("F6")).Append(',').Append(point.Y.ToString("F6")).Append(';');
        }

        private static void AssertRun(GeoPolyline polyline, params GeoPoint[] expected)
        {
            // Direction is not promised, so the reversed reading counts too.
            Assert.Equal(expected.Length, polyline.VertexCount);

            bool forward = true;
            bool backward = true;
            for (int i = 0; i < expected.Length; i++)
            {
                forward &= expected[i].IsEqualTo(polyline[i], Tolerance.Global);
                backward &= expected[i].IsEqualTo(polyline[expected.Length - 1 - i], Tolerance.Global);
            }

            Assert.True(forward || backward, "unexpected run: " + Canonical(new[] { polyline }));
        }

        #endregion
    }
}
