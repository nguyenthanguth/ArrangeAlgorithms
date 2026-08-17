using ArrangeAlgorithms.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ArrangeAlgorithms.Algorithms
{
    /// <summary>
    /// Các phép tính suy nghiệm dùng chung cho những thuật toán đặt nhãn tuần tự
    /// (<see cref="GreedyAlgorithm"/> và <see cref="BoundedBacktrackingAlgorithm"/>):
    /// xác định thứ tự xử lý, đo độ thông thoáng và khoanh vùng lọc vật cản.
    /// </summary>
    internal static class PlacementHeuristics
    {
        /// <summary>
        /// Tính toán thứ tự xử lý (sắp xếp) của các nhãn để tối ưu khả năng đặt thành công.
        /// </summary>
        internal static IEnumerable<int> GetProcessingOrder(
            List<Arrange> arranges, List<Obstacle> staticObstacles, ArrangeOptions options)
        {
            int[] indices = Enumerable.Range(0, arranges.Count)
                .Where(index => arranges[index] != null)
                .ToArray();

            if (indices.Length == 0)
            {
                return indices;
            }

            double centroidX = 0;
            double centroidY = 0;

            // Tính toán trọng tâm hình học của toàn bộ vùng phân bổ nếu chế độ sắp xếp từ trong ra ngoài được kích hoạt
            if (options.PlaceFromInsideOut)
            {
                double sumX = 0;
                double sumY = 0;
                foreach (int index in indices)
                {
                    sumX += arranges[index].GeoRectangle.Center.X;
                    sumY += arranges[index].GeoRectangle.Center.Y;
                }
                centroidX = sumX / indices.Length;
                centroidY = sumY / indices.Length;
            }

            // CHẾ ĐỘ 1: Sắp xếp theo hình học đơn thuần (không tính độ tự do va chạm)
            if (!options.PlaceMostConstrainedFirst)
            {
                if (options.PlaceFromInsideOut)
                {
                    // Sắp xếp tăng dần theo khoảng cách bình phương tới trọng tâm (từ trong lõi ra rìa ngoài)
                    return indices
                        .OrderBy(index => GetSquaredDistanceToCentroid(arranges[index], centroidX, centroidY))
                        .ToArray();
                }

                // Sắp xếp mặc định: từ trái qua phải, từ dưới lên trên
                return indices
                    .OrderBy(index => arranges[index].GeoRectangle.Center.X)
                    .ThenBy(index => arranges[index].GeoRectangle.Center.Y)
                    .ToArray();
            }

            // CHẾ ĐỘ 2: Sắp xếp tối ưu va chạm (Mặc định).
            // Đếm thử số lượng vị trí trống của từng nhãn để đánh giá mức độ bị bó hẹp.
            // Mức tự do đo trên vật cản tĩnh, tính một lần trước khi đặt bất cứ nhãn nào.
            // Đo lại sau mỗi lần đặt thì chính xác hơn nhưng tốn bình phương số nhãn,
            // trong khi phần lớn ràng buộc vốn đến từ vật cản tĩnh.
            var freedom = new int[arranges.Count];
            var room = new int[arranges.Count];

            foreach (int index in indices)
            {
                freedom[index] = CountFreePlaces(arranges[index], staticObstacles, options);
                room[index] = CountAllPlaces(arranges[index], options);
            }

            if (options.PlaceFromInsideOut)
            {
                // Ưu tiên nhãn ít chỗ trống nhất trước. Nếu cùng độ tự do, ưu tiên nhãn gần trọng tâm hơn.
                return indices
                    .OrderBy(index => freedom[index])
                    .ThenBy(index => GetSquaredDistanceToCentroid(arranges[index], centroidX, centroidY))
                    .ToArray();
            }

            // Tiêu chí phụ là tổng số ứng viên. Nhóm ứng viên ưu tiên của hai nhãn có thể
            // giống hệt nhau — cùng gốc, cùng mẫu loang — nên riêng số chỗ trống trong nhóm
            // đó không phân biệt được nhãn đoạn dẫn ngắn với nhãn đoạn dẫn dài. Đường thoát
            // của nhãn đoạn dẫn dài nằm ở các ứng viên trượt xa, ngoài phạm vi lấy mẫu.
            //
            // OrderBy của LINQ ổn định, nên nhãn hòa cả hai tiêu chí vẫn giữ thứ tự đầu vào
            // và kết quả tái lập được.
            return indices
                .OrderBy(index => freedom[index])
                .ThenBy(index => room[index])
                .ToArray();
        }

        /// <summary>
        /// Tính khoảng cách bình phương từ tâm nhãn tới trọng tâm khu vực.
        /// </summary>
        private static double GetSquaredDistanceToCentroid(Arrange arrange, double centroidX, double centroidY)
        {
            double dx = arrange.GeoRectangle.Center.X - centroidX;
            double dy = arrange.GeoRectangle.Center.Y - centroidY;
            return dx * dx + dy * dy;
        }

        /// <summary>
        /// Đếm số lượng vị trí trống thực tế trong nhóm ứng viên mẫu đầu tiên.
        /// </summary>
        private static int CountFreePlaces(Arrange arrange, List<Obstacle> staticObstacles, ArrangeOptions options)
        {
            GeoPoint centre = arrange.GeoRectangle.Center;
            int free = 0;
            int examined = 0;

            foreach (GeoPoint candidate in arrange.EnumeratePlacePoints(options))
            {
                // Chỉ lấy mẫu số lượng nhỏ theo cấu hình FreedomSampleSize (mặc định = 12) để bảo đảm hiệu năng
                if (examined >= options.FreedomSampleSize)
                {
                    return free;
                }

                examined++;

                var translation = centre.GetVectorTo(candidate);
                var moved = new GeoRectangle(arrange.GeoRectangle.Center.Add(translation), arrange.GeoRectangle.Width, arrange.GeoRectangle.Height, arrange.GeoRectangle.AngleRad);

                // Nếu vị trí này không đè lên bất kỳ vật cản nào thì coi là vị trí tự do
                if (!ArrangeAlgorithms.Arrange.Collides(staticObstacles, moved, options.Tolerance))
                {
                    free++;
                }
            }

            return examined == 0 ? -1 : free;
        }

        /// <summary>
        /// Tính toán tổng số lượng ứng viên tối đa có thể sinh ra dọc theo đoạn dẫn.
        /// </summary>
        private static int CountAllPlaces(Arrange arrange, ArrangeOptions options)
        {
            if (!arrange.TryGetLayout(options, out Layout layout))
            {
                return 0;
            }

            double step = layout.MaximumShift / 20.0;
            if (step < 0.1)
            {
                step = layout.Height;
            }

            // Mỗi lớp có 2 vị trí giữa cộng 4 vị trí cho mỗi bước trượt dọc.
            long shiftsPerLevel = (long)Math.Floor(layout.MaximumShift / step);
            long total = (2L + 4L * shiftsPerLevel) * Math.Max(0, options.PerpendicularLevels);

            return (int)Math.Min(total, options.MaximumCandidates);
        }

        /// <summary>
        /// Tính toán khoảng cách biên-biên nhỏ nhất từ nhãn đến tất cả vật cản xung quanh khi không va chạm.
        /// </summary>
        internal static double MeasureClearance(List<Obstacle> obstacles, GeoRectangle moved)
        {
            double best = double.MaxValue;

            foreach (Obstacle obstacle in obstacles)
            {
                double distance = double.MaxValue;

                switch (obstacle.Type)
                {
                    case ObstacleType.GeoRectangle:
                        distance = moved.DistanceTo(obstacle.GeoRectangle);
                        break;
                    case ObstacleType.GeoPolygon:
                        distance = moved.DistanceTo(obstacle.GeoPolygon);
                        break;
                    case ObstacleType.GeoLine:
                        distance = moved.DistanceTo(obstacle.GeoLine);
                        break;
                }

                if (distance < best)
                {
                    best = distance;
                }
            }

            return best;
        }

        /// <summary>
        /// Tính toán phạm vi (Bounding Box) chứa toàn bộ các điểm ứng viên có thể sinh ra.
        /// Phục vụ cho việc lọc thô loại bỏ các vật cản ở quá xa nhãn.
        /// </summary>
        internal static bool TryGetCandidateBounds(Arrange arrange, ArrangeOptions options, out Bounds bounds)
        {
            if (!arrange.TryGetLayout(options, out Layout layout))
            {
                bounds = default(Bounds);
                return false;
            }

            double maximumOffset = layout.BaseOffset
                                   + Math.Max(0, options.PerpendicularLevels - 1) * (layout.Height + options.RowGap);

            GeoVector alongMax = layout.Direction * layout.MaximumShift;
            GeoVector acrossMax = layout.Perpendicular * maximumOffset;

            var corners = new[]
            {
                layout.Anchor + acrossMax + alongMax,
                layout.Anchor + acrossMax - alongMax,
                layout.Anchor - acrossMax + alongMax,
                layout.Anchor - acrossMax - alongMax,
            };

            // Tạo hộp bao quanh 4 góc biên ngoài và nới rộng thêm một lượng NeighbourMargin để an toàn
            bounds = Bounds.Around(corners).Expand(arrange.GetBoxSpan(options) + options.NeighbourMargin);
            return true;
        }
    }
}
