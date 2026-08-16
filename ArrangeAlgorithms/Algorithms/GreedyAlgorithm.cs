using System.Collections.Generic;
using System.Linq;
using ArrangeAlgorithms.Geometry;

namespace ArrangeAlgorithms.Algorithms
{
    /// <summary>
    /// Thuật toán sắp xếp nhãn theo chiến lược Tham lam (Greedy).
    /// </summary>
    internal class GreedyAlgorithm : IArrangeAlgorithm
    {
        public List<GeoVector> Arrange(List<Arrange> arranges, ArrangeOptions options)
        {
            var translations = new GeoVector[arranges.Count];
            // BƯỚC 1: Thu thập tất cả các vật cản tĩnh từ đầu vào (Đa giác cấm và Đoạn thẳng cấm)
            var occupied = ArrangeAlgorithms.Arrange.CollectStaticObstacles(arranges);

            // BƯỚC 2: Xác định thứ tự đặt nhãn.
            var processingOrder = PlacementHeuristics.GetProcessingOrder(arranges, occupied, options);

            // BƯỚC 3: Tiến hành đặt từng nhãn theo thứ tự đã tính toán.
            foreach (int index in processingOrder)
            {
                translations[index] = Place(arranges[index], occupied, options);
            }

            return translations.ToList();
        }

        /// <summary>
        /// Thực hiện tìm kiếm vị trí đặt tốt nhất cho một nhãn đơn lẻ theo chiến lược tham lam.
        /// </summary>
        private GeoVector Place(Arrange arrange, List<Obstacle> occupied, ArrangeOptions options)
        {
            // Kiểm tra tính hợp lệ của hộp nhãn và tính toán Bound lọc cục bộ
            if (!PlacementHeuristics.TryGetCandidateBounds(arrange, options, out Bounds region))
            {
                // Không tính được ứng viên: nhãn đứng yên, nhưng vẫn phải ghi vùng nó đang chiếm để các nhãn sau không đè lên.
                AddBox(arrange, occupied, GeoVector.Zero);
                return GeoVector.Zero;
            }

            GeoPoint centre = arrange.GeoRectangle.Center;

            // Lọc nhanh: Chỉ giữ lại các vật cản có khả năng va chạm trong vùng lân cận
            List<Obstacle> nearby = occupied.Where(obstacle => region.Overlaps(obstacle.Box)).ToList();

            GeoVector chosen = GeoVector.Zero;
            GeoVector firstCandidate = GeoVector.Zero;
            double bestClearance = -1.0;
            int freeSeen = 0;
            bool hasCandidate = false;

            // Duyệt qua các vị trí loang để tìm kiếm ứng viên trống
            foreach (GeoPoint candidate in arrange.EnumeratePlacePoints(options))
            {
                GeoVector translation = centre.GetVectorTo(candidate);

                // Lưu lại ứng viên đầu tiên để làm phương án dự phòng (fallback) nếu tất cả đều va chạm
                if (!hasCandidate)
                {
                    firstCandidate = translation;
                    hasCandidate = true;
                }

                GeoRectangle moved = new GeoRectangle(arrange.GeoRectangle.Center.Add(translation), arrange.GeoRectangle.Width, arrange.GeoRectangle.Height, arrange.GeoRectangle.AngleRad);

                // Kiểm tra va chạm chi tiết
                if (ArrangeAlgorithms.Arrange.Collides(nearby, moved, options.Tolerance))
                {
                    continue;
                }

                // Đo khoảng hở tới tất cả vật cản xung quanh để đánh giá mức độ thông thoáng
                // Trong nhóm vị trí trống đầu tiên, chọn vị trí thoáng nhất. So sánh chặt
                // nên khi hòa nhau thì ứng viên tìm thấy trước — tức ưu tiên hơn — vẫn thắng.
                double clearance = PlacementHeuristics.MeasureClearance(nearby, moved);

                if (clearance > bestClearance)
                {
                    bestClearance = clearance;
                    chosen = translation;
                }

                freeSeen++;

                // Áp dụng cơ chế nhìn trước (LookAheadCandidates) để dừng sớm khi tìm đủ số ứng viên trống, tránh quét thừa
                if (freeSeen >= options.LookAheadCandidates)
                {
                    break;
                }
            }

            // Mọi ứng viên đều vướng. Vẫn phải đặt nhãn đi đâu đó — chồng lấn ở vị trí dự đoán được
            // vẫn dễ sửa tay hơn là để nhãn nằm lại chỗ cũ tùy tiện.
            //
            // Luôn lùi về ứng viên đầu tiên, KHÔNG đi tìm chỗ ít chồng lấn nhất. Nghe phản trực giác
            // nhưng đã đo: quét tìm chỗ đè ít nhất làm tệ đi rõ rệt (80 nhãn chật, 16 seed: 32.3% -> 22.9%
            // số nhãn sạch). Lý do là nhãn kẹt sẽ di cư sang vùng đang yên tĩnh và phá hỏng những nhãn
            // xếp tốt ở đó, rồi lan tiếp. Quy tắc cố định giữ mọi nhãn kẹt nằm sát đoạn dẫn của chính nó
            // và dồn về cùng một phía, nên thiệt hại không lan ra ngoài.
            if (freeSeen == 0)
            {
                if (!hasCandidate)
                {
                    AddBox(arrange, occupied, GeoVector.Zero);
                    return GeoVector.Zero;
                }

                chosen = firstCandidate;
            }

            // Đưa vị trí mới được chọn vào danh sách vật cản tĩnh của các nhãn xếp sau
            AddBox(arrange, occupied, chosen);

            // Bỏ qua các chuyển dịch cực nhỏ không đáng kể
            return chosen.Length > options.MinimumMoveDistance ? chosen : GeoVector.Zero;
        }

        /// <summary>
        /// Dịch chuyển hộp nhãn và đưa vào danh sách vật cản tĩnh đã chiếm dụng.
        /// </summary>
        private static void AddBox(Arrange arrange, List<Obstacle> occupied, GeoVector translation)
        {
            var moved = new GeoRectangle(arrange.GeoRectangle.Center.Add(translation), arrange.GeoRectangle.Width, arrange.GeoRectangle.Height, arrange.GeoRectangle.AngleRad);
            occupied.Add(new Obstacle(moved));
        }
    }
}
