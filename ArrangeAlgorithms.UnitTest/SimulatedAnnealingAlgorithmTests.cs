using System;
using System.Collections.Generic;
using Xunit;
using ArrangeAlgorithms;
using ArrangeAlgorithms.Geometry;

namespace ArrangeAlgorithms.UnitTest
{
    public class SimulatedAnnealingAlgorithmTests
    {
        [Fact]
        public void Arrange_Run_SimulatedAnnealing_FindsSolution()
        {
            var leaderLine = new GeoLine(0.0, 0.0, 10.0, 0.0);
            var a1 = new Arrange
            {
                GeoRectangle = new GeoRectangle(new GeoPoint(5.0, 0.0), 20.0, 10.0),
                GeoLine = leaderLine
            };
            var a2 = new Arrange
            {
                GeoRectangle = new GeoRectangle(new GeoPoint(5.0, 0.0), 20.0, 10.0),
                GeoLine = leaderLine
            };

            var options = new ArrangeOptions
            {
                Algorithm = ArrangeAlgorithmType.SimulatedAnnealing,
                MarkOffsetFromLine = 5.0,
                RowGap = 5.0,
                PerpendicularLevels = 2
            };

            var translations = Arrange.Run(new List<Arrange> { a1, a2 }, options);

            Assert.Equal(2, translations.Count);
            var moved1 = new GeoRectangle(a1.GeoRectangle.Center + translations[0], a1.GeoRectangle.Width, a1.GeoRectangle.Height);
            var moved2 = new GeoRectangle(a2.GeoRectangle.Center + translations[1], a2.GeoRectangle.Width, a2.GeoRectangle.Height);

            Assert.False(moved1.IntersectsWith(moved2));
        }
    }
}

