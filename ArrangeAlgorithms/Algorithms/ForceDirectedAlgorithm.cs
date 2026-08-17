using ArrangeAlgorithms.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ArrangeAlgorithms.Algorithms
{
    /// <summary>
    /// Thuật toán sắp xếp nhãn sử dụng mô phỏng lực vật lý liên tục (Force-directed),
    /// sau đó ánh xạ tâm nhãn về vị trí ứng viên rời rạc gần nhất không va chạm.
    /// </summary>
    internal class ForceDirectedAlgorithm : IArrangeAlgorithm
    {
        /// <summary>Bán kính ảnh hưởng của lực đẩy từ vật cản tĩnh; xa hơn thì không góp lực.</summary>
        private const double PushRadius = 1500.0;

        public List<GeoVector> Arrange(List<Arrange> arranges, ArrangeOptions options)
        {
            if (arranges.Count == 0)
            {
                return new List<GeoVector>();
            }

            var staticObstacles = ArrangeAlgorithms.Arrange.CollectStaticObstacles(arranges);
            var anchors = new GeoPoint[arranges.Count];
            var positions = new GeoPoint[arranges.Count];

            // BƯỚC 1: Ghi nhận vị trí mặc định ban đầu (Anchors)
            for (int i = 0; i < arranges.Count; i++)
            {
                var arrange = arranges[i];
                if (arrange == null) continue;

                anchors[i] = arrange.GeoRectangle.Center;
                positions[i] = arrange.GeoRectangle.Center;
            }

            // BƯỚC 2: Chạy giả lập mô phỏng lực vật lý liên tục
            int iterations = options.ForceIterations;
            double timestep = 0.5;

            for (int step = 0; step < iterations; step++)
            {
                var forces = new GeoVector[arranges.Count];
                for (int i = 0; i < arranges.Count; i++)
                {
                    forces[i] = GeoVector.Zero;
                }

                // 1. Lực lò xo (Spring Force) kéo nhãn về vị trí gốc ban đầu để tránh bay quá xa
                for (int i = 0; i < arranges.Count; i++)
                {
                    if (arranges[i] == null) continue;

                    GeoVector toAnchor = positions[i].GetVectorTo(anchors[i]);
                    forces[i] = forces[i].Add(toAnchor * 0.05); // Hệ số đàn hồi lò xo
                }

                // 2. Lực đẩy Coulomb (Repulsive Force) đẩy các nhãn ra xa nhau
                for (int i = 0; i < arranges.Count; i++)
                {
                    if (arranges[i] == null) continue;

                    for (int j = i + 1; j < arranges.Count; j++)
                    {
                        if (arranges[j] == null) continue;

                        GeoVector toOther = positions[i].GetVectorTo(positions[j]);
                        double distance = Math.Max(toOther.Length, 10.0);

                        // Chỉ đẩy nếu hai nhãn ở quá gần nhau (ngưỡng 2500mm)
                        if (distance < 2500.0)
                        {
                            double pushMagnitude = 150000.0 / (distance * distance);
                            if (!toOther.TryGetNormal(out GeoVector pushDir))
                            {
                                pushDir = GeoVector.XAxis;
                            }
                            forces[i] = forces[i].Subtract(pushDir * pushMagnitude);
                            forces[j] = forces[j].Add(pushDir * pushMagnitude);
                        }
                    }
                }

                // 3. Lực đẩy từ các vật cản tĩnh (đa giác, đoạn thẳng cấm)
                for (int i = 0; i < arranges.Count; i++)
                {
                    if (arranges[i] == null) continue;

                    // Vật cản xa hơn PushRadius không góp lực. Loại chúng bằng phép so hộp bao trước,
                    // vì GetClosestBoundaryPoint phải duyệt từng cạnh nên đắt hơn hẳn.
                    Bounds reach = Bounds.Around(new[] { positions[i] }).Expand(PushRadius);

                    foreach (Obstacle obstacle in staticObstacles)
                    {
                        if (!reach.Overlaps(obstacle.Box))
                        {
                            continue;
                        }

                        // Lấy điểm gần nhất trên biên vật cản, rồi đẩy nhãn theo hướng đi TỪ điểm đó RA nhãn.
                        // Lấy hướng ngược lại (nhãn -> tâm vật cản) sẽ biến lực đẩy thành lực hút.
                        GeoPoint closest = GetClosestBoundaryPoint(obstacle, positions[i]);

                        double dist = Math.Max(closest.DistanceTo(positions[i]), 10.0);
                        if (dist >= PushRadius)
                        {
                            continue;
                        }

                        if (!closest.GetVectorTo(positions[i]).TryGetNormal(out GeoVector pushDir))
                        {
                            pushDir = GeoVector.XAxis;
                        }

                        double pushMagnitude = 200000.0 / (dist * dist);
                        forces[i] = forces[i].Add(pushDir * pushMagnitude);
                    }
                }

                // 4. Cập nhật vị trí nhãn (Khống chế độ dịch tối đa để hệ thống ổn định)
                for (int i = 0; i < arranges.Count; i++)
                {
                    if (arranges[i] == null) continue;

                    GeoVector stepMove = forces[i] * timestep;
                    if (stepMove.Length > 500.0)
                    {
                        stepMove = stepMove.Normalize() * 500.0;
                    }

                    positions[i] = positions[i].Add(stepMove);
                }
            }

            // BƯỚC 3: Ánh xạ rời rạc (Discrete Mapping)
            // Tìm điểm ứng viên rời rạc không va chạm tĩnh gần nhất với vị trí vật lý cuối cùng
            var translations = new GeoVector[arranges.Count];
            var finalOccupied = new List<Obstacle>(staticObstacles);

            for (int i = 0; i < arranges.Count; i++)
            {
                var arrange = arranges[i];
                if (arrange == null) continue;

                GeoPoint centre = arrange.GeoRectangle.Center;
                GeoPoint physTarget = positions[i];
                GeoVector bestTranslation = GeoVector.Zero;
                double bestDist = double.MaxValue;
                bool mapped = false;

                // Bỏ trước các vật cản ngoài tầm với của nhãn. finalOccupied phình dần theo số nhãn
                // đã đặt nên bộ lọc có lãi ngay cả khi bản vẽ không có vùng cấm tĩnh nào.
                List<Obstacle> nearby = PlacementHeuristics.TryGetCandidateBounds(arrange, options, out Bounds region)
                    ? finalOccupied.Where(o => region.Overlaps(o.Box)).ToList()
                    : finalOccupied;

                // Duyệt qua toàn bộ ứng viên rời rạc của nhãn
                foreach (GeoPoint candidate in arrange.EnumeratePlacePoints(options))
                {
                    GeoVector translation = centre.GetVectorTo(candidate);
                    GeoRectangle moved = new GeoRectangle(arrange.GeoRectangle.Center.Add(translation), arrange.GeoRectangle.Width, arrange.GeoRectangle.Height, arrange.GeoRectangle.AngleRad);

                    // Chỉ chấp nhận nếu ứng viên đó không va chạm với vật cản tĩnh và các nhãn đã được đặt trước
                    if (!ArrangeAlgorithms.Arrange.Collides(nearby, moved, options.Tolerance))
                    {
                        double dist = candidate.DistanceTo(physTarget);
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestTranslation = translation;
                            mapped = true;
                        }
                    }
                }

                // Nếu không tìm được vị trí trống nào, lùi về vị trí mặc định cấp 0 của nhãn.
                // Cờ Placed do Arrange.MarkPlacementResults quyết định trên bố cục cuối cùng.
                if (!mapped)
                {
                    var points = arrange.EnumeratePlacePoints(options).ToList();
                    bestTranslation = points.Count > 0 ? centre.GetVectorTo(points[0]) : GeoVector.Zero;
                }

                translations[i] = bestTranslation;

                // Thêm vị trí đã chọn làm vật cản tĩnh cho các nhãn xử lý tiếp theo
                GeoRectangle finalRect = new GeoRectangle(arrange.GeoRectangle.Center.Add(bestTranslation), arrange.GeoRectangle.Width, arrange.GeoRectangle.Height, arrange.GeoRectangle.AngleRad);
                finalOccupied.Add(new Obstacle(finalRect));
            }

            return translations.ToList();
        }

        /// <summary>
        /// Lấy điểm nằm trên biên vật cản gần một điểm cho trước nhất.
        /// </summary>
        private static GeoPoint GetClosestBoundaryPoint(Obstacle obstacle, GeoPoint from)
        {
            switch (obstacle.Type)
            {
                case ObstacleType.GeoLine:
                    return obstacle.GeoLine.GetClosestPointTo(from);
                case ObstacleType.GeoRectangle:
                    return GetClosestPointOnEdges(obstacle.GeoRectangle.GetEdges(), from);
                default:
                    return GetClosestPointOnEdges(obstacle.GeoPolygon.GetEdges(), from);
            }
        }

        /// <summary>
        /// Lấy điểm gần nhất tới một điểm cho trước trong số các điểm gần nhất trên từng cạnh.
        /// </summary>
        private static GeoPoint GetClosestPointOnEdges(IEnumerable<GeoLine> edges, GeoPoint from)
        {
            GeoPoint best = from;
            double bestDistance = double.MaxValue;

            foreach (GeoLine edge in edges)
            {
                GeoPoint candidate = edge.GetClosestPointTo(from);
                double distance = candidate.DistanceTo(from);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            return best;
        }
    }
}

