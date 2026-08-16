using System;
using System.Collections.Generic;
using Xunit;
using ArrangeAlgorithms;
using ArrangeAlgorithms.Geometry;

namespace ArrangeAlgorithms.UnitTest
{
    public class ArrangeTests
    {
        [Fact]
        public void Arrange_InvalidBoxSize_ReturnsZeroTranslations()
        {
            var leaderLine = new GeoLine(0.0, 0.0, 10.0, 0.0);
            var a1 = new Arrange
            {
                GeoRectangle = new GeoRectangle(new GeoPoint(5.0, 0.0), 2.0, 2.0), // Quá nhỏ
                GeoLine = leaderLine
            };

            var options = new ArrangeOptions
            {
                MinimumBoxSize = 5.0
            };

            var translations = Arrange.Run(new List<Arrange> { a1 }, options);
            Assert.Equal(GeoVector.Zero, translations[0]);
            Assert.False(a1.Placed);
        }
    }
}

