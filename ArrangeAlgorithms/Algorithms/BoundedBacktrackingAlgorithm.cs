using ArrangeAlgorithms.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace ArrangeAlgorithms.Algorithms
{
    /// <summary>
    /// Thuật toán sắp xếp nhãn sử dụng chiến lược Quay lui có giới hạn (Bounded Backtracking).
    /// Giúp sửa chữa các tối ưu cục bộ bằng cách thử các ứng viên dự phòng khi nhãn sau bị kẹt.
    /// </summary>
    internal class BoundedBacktrackingAlgorithm : IArrangeAlgorithm
    {
        private int _stepsCount;
        private bool _isTimeout;

        public List<GeoVector> Arrange(List<Arrange> arranges, ArrangeOptions options)
        {
            var translations = new GeoVector[arranges.Count];
            // BƯỚC 1: Thu thập vật cản tĩnh ban đầu
            var occupied = ArrangeAlgorithms.Arrange.CollectStaticObstacles(arranges);

            // BƯỚC 2: Tính thứ tự ưu tiên xử lý nhãn
            var processingOrder = PlacementHeuristics.GetProcessingOrder(arranges, occupied, options).ToArray();
            var sortedArranges = processingOrder.Select(idx => arranges[idx]).ToList();
            var sortedTranslations = new GeoVector[sortedArranges.Count];

            _stepsCount = 0;
            _isTimeout = false;

            // BƯỚC 3: Chạy giải thuật quay lui đệ quy
            bool success = Backtrack(0, sortedArranges, occupied, sortedTranslations, options);

            // BƯỚC 4: Nếu quay lui thất bại hoàn toàn (không có cấu hình nào sạch va chạm),
            // lùi về giải pháp tham lam Greedy để đảm bảo tất cả nhãn vẫn có vị trí hiển thị.
            if (!success)
            {
                var greedy = new GreedyAlgorithm();
                return greedy.Arrange(arranges, options);
            }

            // BƯỚC 5: Ánh xạ kết quả sắp xếp ngược lại theo đúng thứ tự đầu vào
            for (int i = 0; i < processingOrder.Length; i++)
            {
                int originalIndex = processingOrder[i];
                translations[originalIndex] = sortedTranslations[i];
            }

            return translations.ToList();
        }

        /// <summary>
        /// Hàm quay lui đệ quy để đặt nhãn tại vị trí index.
        /// </summary>
        private bool Backtrack(int index, List<Arrange> sortedArranges, List<Obstacle> occupied, GeoVector[] translations, ArrangeOptions options)
        {
            // Điều kiện dừng đệ quy: Đã đặt thành công toàn bộ các nhãn
            if (index >= sortedArranges.Count)
            {
                return true;
            }

            _stepsCount++;
            if (_stepsCount > options.MaxBacktrackSteps)
            {
                _isTimeout = true;
                return false;
            }

            Arrange arrange = sortedArranges[index];
            if (!PlacementHeuristics.TryGetCandidateBounds(arrange, options, out Bounds region))
            {
                // Không dựng được bố cục cho nhãn này: để nó đứng yên và đi tiếp.
                // Arrange.MarkPlacementResults sẽ nhận ra nó chưa được sắp xếp.
                translations[index] = GeoVector.Zero;
                return Backtrack(index + 1, sortedArranges, occupied, translations, options);
            }

            GeoPoint centre = arrange.GeoRectangle.Center;

            // Lọc nhanh vật cản lân cận
            List<Obstacle> nearby = occupied.Where(obstacle => region.Overlaps(obstacle.Box)).ToList();

            // Lấy danh sách ứng viên trống (không va chạm)
            var candidates = new List<(GeoVector translation, double clearance)>();
            foreach (GeoPoint candidate in arrange.EnumeratePlacePoints(options))
            {
                GeoVector translation = centre.GetVectorTo(candidate);
                GeoRectangle moved = new GeoRectangle(arrange.GeoRectangle.Center.Add(translation), arrange.GeoRectangle.Width, arrange.GeoRectangle.Height, arrange.GeoRectangle.AngleRad);

                if (!ArrangeAlgorithms.Arrange.Collides(nearby, moved, options.Tolerance))
                {
                    double clearance = PlacementHeuristics.MeasureClearance(nearby, moved);
                    candidates.Add((translation, clearance));
                }
            }

            // Thử vị trí gần đoạn dẫn trước, hòa nhau thì lấy chỗ thoáng hơn.
            //
            // Trước đây chỗ này chỉ xếp theo khoảng hở giảm dần, tức luôn thử vị trí XA NHẤT trước và
            // gần như luôn dừng lại ở đó. Hệ quả đo được: khoảng cách nhãn tới đoạn dẫn trung bình 2405
            // so với 550 của Greedy — mọi nhãn bị ném ra cấp vuông góc ngoài cùng dù chỗ gần còn trống.
            // Toàn bộ giá trị mặc định (Arrange.MarkOffsetFromLine, LongitudinalOvershootRatio) đều nói rằng
            // nhãn nên bám sát đoạn dẫn, nên độ dịch chuyển mới là tiêu chí chính.
            //
            // Khoảng hở vẫn hữu ích để phá hòa: bộ sinh ứng viên đối xứng nên các vị trí cách đều nhau
            // (trên/dưới đoạn dẫn, trượt tiến/lùi) hòa chính xác rất thường xuyên.
            candidates = candidates
                .OrderBy(c => c.translation.Length)
                .ThenByDescending(c => c.clearance)
                .ToList();

            // Thử đặt nhãn vào từng ứng viên trống tiềm năng
            foreach (var item in candidates)
            {
                translations[index] = item.translation;

                // Thêm hộp nhãn mới được đặt làm vật cản tạm thời cho đệ quy cấp sau
                GeoRectangle moved = new GeoRectangle(arrange.GeoRectangle.Center.Add(item.translation), arrange.GeoRectangle.Width, arrange.GeoRectangle.Height, arrange.GeoRectangle.AngleRad);
                var obs = new Obstacle(moved);
                occupied.Add(obs);

                // Đệ quy cấp tiếp theo
                if (Backtrack(index + 1, sortedArranges, occupied, translations, options))
                {
                    return true;
                }

                // Nếu đệ quy cấp sau thất bại, gỡ bỏ vật cản (Backtrack) và thử ứng viên tiếp theo
                occupied.RemoveAt(occupied.Count - 1);

                if (_isTimeout)
                {
                    return false;
                }
            }

            return false;
        }
    }
}
