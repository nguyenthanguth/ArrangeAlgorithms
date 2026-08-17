using ArrangeAlgorithms.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ArrangeAlgorithms.Algorithms
{
    /// <summary>
    /// Thuật toán sắp xếp nhãn sử dụng chiến lược Luyện kim giả lập (Simulated Annealing).
    /// Sử dụng mô phỏng vật lý nhiệt học để giảm va chạm và tìm vị trí tối ưu toàn cục.
    /// </summary>
    internal class SimulatedAnnealingAlgorithm : IArrangeAlgorithm
    {
        public List<GeoVector> Arrange(List<Arrange> arranges, ArrangeOptions options)
        {
            if (arranges.Count == 0)
            {
                return new List<GeoVector>();
            }

            // BƯỚC 1: Thu thập vật cản tĩnh ban đầu
            var staticObstacles = ArrangeAlgorithms.Arrange.CollectStaticObstacles(arranges);

            // BƯỚC 2: Sinh trước toàn bộ danh sách điểm ứng viên dịch chuyển cho mỗi nhãn
            var candidates = new List<List<(GeoVector Translation, GeoPoint GeoPoint)>>();
            var currentTranslations = new GeoVector[arranges.Count];
            var currentIndices = new int[arranges.Count];

            // Vật cản lân cận của mỗi nhãn, lọc SẴN một lần ở đây.
            // Vùng lọc chỉ phụ thuộc đoạn dẫn và kích thước nhãn nên bất biến suốt quá trình luyện kim,
            // trong khi CalculateEnergy được gọi hàng nghìn lần. Lọc bên trong vòng lặp sẽ lãng phí
            // đúng số lần đó.
            var nearby = new List<Obstacle>[arranges.Count];

            for (int i = 0; i < arranges.Count; i++)
            {
                var arrange = arranges[i];
                var list = new List<(GeoVector Translation, GeoPoint GeoPoint)>();
                nearby[i] = staticObstacles;

                if (arrange != null)
                {
                    GeoPoint centre = arrange.GeoRectangle.Center;
                    foreach (GeoPoint p in arrange.EnumeratePlacePoints(options))
                    {
                        list.Add((centre.GetVectorTo(p), p));
                    }

                    if (PlacementHeuristics.TryGetCandidateBounds(arrange, options, out Bounds region))
                    {
                        nearby[i] = staticObstacles.Where(o => region.Overlaps(o.Box)).ToList();
                    }
                }

                candidates.Add(list);
                // Khởi đầu ở vị trí mặc định cấp 0
                if (list.Count > 0)
                {
                    currentTranslations[i] = list[0].Translation;
                    currentIndices[i] = 0;
                }
                else
                {
                    currentTranslations[i] = GeoVector.Zero;
                    currentIndices[i] = -1;
                }
            }

            double currentEnergy = CalculateEnergy(arranges, currentTranslations, nearby, options);
            double bestEnergy = currentEnergy;
            var bestTranslations = currentTranslations.ToArray();

            // BƯỚC 3: Thiết lập các tham số nhiệt độ luyện kim
            double T = options.AnnealingInitialTemperature;
            double coolingRate = options.AnnealingCoolingRate;
            var random = new Random(42); // Seed cố định để kết quả ổn định

            // BƯỚC 4: Vòng lặp hạ nhiệt
            while (T > 0.1)
            {
                // Thực hiện 50 lần thử thay đổi lân cận ở mỗi mức nhiệt độ
                for (int step = 0; step < 50; step++)
                {
                    // Chọn ngẫu nhiên một nhãn
                    int i = random.Next(arranges.Count);
                    if (candidates[i].Count == 0) continue;

                    // Chọn ngẫu nhiên một ứng viên dịch chuyển khác cho nhãn đó
                    int oldCandidateIndex = currentIndices[i];
                    int newCandidateIndex = random.Next(candidates[i].Count);
                    if (newCandidateIndex == oldCandidateIndex) continue;

                    GeoVector oldTranslation = currentTranslations[i];
                    GeoVector newTranslation = candidates[i][newCandidateIndex].Translation;

                    // Thử cập nhật
                    currentTranslations[i] = newTranslation;
                    currentIndices[i] = newCandidateIndex;

                    double nextEnergy = CalculateEnergy(arranges, currentTranslations, nearby, options);
                    double delta = nextEnergy - currentEnergy;

                    // Chấp nhận trạng thái mới dựa trên năng lượng thấp hơn hoặc phân bố xác suất Boltzmann
                    if (delta < 0 || random.NextDouble() < Math.Exp(-delta / T))
                    {
                        currentEnergy = nextEnergy;

                        // Ghi nhận nếu đây là lời giải tốt nhất
                        if (currentEnergy < bestEnergy)
                        {
                            bestEnergy = currentEnergy;
                            bestTranslations = currentTranslations.ToArray();
                        }
                    }
                    else
                    {
                        // Khôi phục lại trạng thái cũ
                        currentTranslations[i] = oldTranslation;
                        currentIndices[i] = oldCandidateIndex;
                    }
                }

                // Hạ nhiệt độ
                T *= coolingRate;
            }

            // Cờ Placed do Arrange.MarkPlacementResults quyết định trên bố cục cuối cùng.
            return bestTranslations.ToList();
        }

        /// <summary>
        /// Tính toán tổng năng lượng (hàm phạt) của cấu hình nhãn hiện tại.
        /// </summary>
        private static double CalculateEnergy(
            List<Arrange> arranges, GeoVector[] translations, List<Obstacle>[] nearby, ArrangeOptions options)
        {
            double energy = 0.0;

            var movedRects = new GeoRectangle[arranges.Count];
            var movedBoxes = new Bounds[arranges.Count];

            for (int i = 0; i < arranges.Count; i++)
            {
                if (arranges[i] == null) continue;

                movedRects[i] = new GeoRectangle(
                    arranges[i].GeoRectangle.Center.Add(translations[i]),
                    arranges[i].GeoRectangle.Width,
                    arranges[i].GeoRectangle.Height,
                    arranges[i].GeoRectangle.AngleRad);

                movedBoxes[i] = Bounds.Of(movedRects[i]);
            }

            for (int i = 0; i < arranges.Count; i++)
            {
                if (arranges[i] == null) continue;

                GeoRectangle rect = movedRects[i];
                Bounds box = movedBoxes[i];

                // 1. Phạt va chạm với vật cản tĩnh
                if (ArrangeAlgorithms.Arrange.Collides(nearby[i], rect, options.Tolerance))
                {
                    energy += 10000.0;
                }

                // 2. Phạt va chạm chéo giữa các nhãn di động
                for (int j = i + 1; j < arranges.Count; j++)
                {
                    if (arranges[j] == null) continue;

                    if (box.Overlaps(movedBoxes[j]))
                    {
                        if (rect.IntersectsWith(movedRects[j]))
                        {
                            energy += 10000.0;
                        }
                    }
                }

                // 3. Phạt khoảng cách di lệch nhãn khỏi vị trí gốc (ưu tiên nhãn nằm sát đoạn dẫn nhất)
                energy += translations[i].Length * 0.1;
            }

            return energy;
        }
    }
}

