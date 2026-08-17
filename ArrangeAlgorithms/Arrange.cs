using ArrangeAlgorithms.Algorithms;
using ArrangeAlgorithms.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ArrangeAlgorithms
{
    /// <summary>
    /// Biểu diễn một nhãn cần sắp xếp cùng với các đối tượng hình học xung quanh (vật cản).
    /// </summary>
    public class Arrange
    {
        /// <summary>Lấy hoặc đặt hình chữ nhật bao của nhãn — phần hình học sẽ được dịch chuyển.</summary>
        public GeoRectangle GeoRectangle { get; set; }

        /// <summary>
        /// Lấy hoặc đặt đoạn dẫn của đối tượng mà nhãn trỏ tới.
        /// Trung điểm của nó là gốc để loang vị trí ứng viên.
        /// </summary>
        public GeoLine GeoLine { get; set; }

        /// <summary>
        /// Lấy hoặc đặt khoảng hở vuông góc tối thiểu giữa mép nhãn và đường dẫn.
        /// Cộng với nửa chiều cao nhãn sẽ ra khoảng cách từ đường dẫn tới tâm nhãn.
        /// <para>
        /// Thuộc tính này nằm trên từng nhãn chứ không nằm trong <see cref="ArrangeOptions"/> vì
        /// mỗi nhãn có thể cần một khoảng hở riêng — chẳng hạn nhãn chữ lớn phải lùi xa hơn nhãn chữ nhỏ.
        /// </para>
        /// </summary>
        public double MarkOffsetFromLine { get; set; } = 50.0;

        /// <summary>Lấy hoặc đặt danh sách đa giác cấm tĩnh mà nhãn không được đè lên.</summary>
        public List<GeoPolygon> BlockPolygons { get; set; }

        /// <summary>Lấy hoặc đặt danh sách đoạn thẳng cấm tĩnh mà nhãn không được đè lên.</summary>
        public List<GeoLine> BlockLines { get; set; }

        /// <summary>
        /// Cho biết nhãn có được đặt thành công vào một vị trí hoàn toàn trống hay không.
        /// </summary>
        public bool Placed { get; private set; }

        /// <summary>
        /// Thiết lập trạng thái đặt nhãn thành công hay thất bại.
        /// </summary>
        internal void SetPlaced(bool value)
        {
            Placed = value;
        }

        /// <summary>Sắp xếp danh sách nhãn bằng cấu hình tham số mặc định.</summary>
        /// <param name="arranges">Danh sách nhãn cần sắp xếp.</param>
        /// <returns>Danh sách GeoVector dịch chuyển tương ứng cho từng nhãn theo đúng thứ tự đầu vào.</returns>
        public static List<GeoVector> Run(List<Arrange> arranges)
        {
            return Run(arranges, ArrangeOptions.Default);
        }

        /// <summary>
        /// Sắp xếp danh sách nhãn để chúng không đè lên nhau và không đè lên vùng cấm.
        /// </summary>
        /// <param name="arranges">Danh sách nhãn cần sắp xếp.</param>
        /// <param name="options">Cấu hình điều khiển thuật toán.</param>
        /// <returns>Danh sách GeoVector dịch chuyển tương ứng cho từng nhãn theo đúng thứ tự đầu vào.</returns>
        public static List<GeoVector> Run(List<Arrange> arranges, ArrangeOptions options)
        {
            if (arranges == null)
            {
                throw new ArgumentNullException(nameof(arranges));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            // Lựa chọn thuật toán sắp xếp dựa trên cấu hình tùy chọn
            IArrangeAlgorithm algorithm;
            switch (options.Algorithm)
            {
                case ArrangeAlgorithmType.BoundedBacktracking:
                    algorithm = new BoundedBacktrackingAlgorithm();
                    break;
                case ArrangeAlgorithmType.SimulatedAnnealing:
                    algorithm = new SimulatedAnnealingAlgorithm();
                    break;
                case ArrangeAlgorithmType.ForceDirected:
                    algorithm = new ForceDirectedAlgorithm();
                    break;
                case ArrangeAlgorithmType.ConstraintSatisfaction:
                    algorithm = new ConstraintSatisfactionAlgorithm();
                    break;
                case ArrangeAlgorithmType.Greedy:
                default:
                    algorithm = new GreedyAlgorithm();
                    break;
            }

            List<GeoVector> translations = algorithm.Arrange(arranges, options);

            // Cờ Placed do một chỗ duy nhất quyết định, dựa trên bố cục cuối cùng chứ không dựa vào
            // lời tự khai của từng thuật toán. Xem MarkPlacementResults.
            MarkPlacementResults(arranges, translations, options);

            return translations;
        }

        /// <summary>
        /// Thu thập tất cả các vật cản tĩnh (đa giác cấm và đoạn thẳng cấm) từ danh sách nhãn.
        /// <para>
        /// Vật cản trùng nhau chỉ được giữ một bản. Cách dùng thư viện phổ biến là gán cùng một tập vùng cấm
        /// cho mọi nhãn — chẳng hạn mỗi nhãn né tất cả đoạn dẫn còn lại — khiến danh sách phình theo bình phương
        /// số nhãn dù số hình học phân biệt vẫn nhỏ. Khử trùng ở đây có lợi cho toàn bộ các thuật toán.
        /// </para>
        /// </summary>
        internal static List<Obstacle> CollectStaticObstacles(List<Arrange> arranges)
        {
            var occupied = new List<Obstacle>();
            if (arranges == null) return occupied;

            var seenPolygons = new HashSet<GeoPolygon>();
            var seenLines = new HashSet<GeoLine>();

            foreach (Arrange arrange in arranges)
            {
                if (arrange == null) continue;

                if (arrange.BlockPolygons != null)
                {
                    foreach (GeoPolygon block in arrange.BlockPolygons)
                    {
                        if (block != null && seenPolygons.Add(block))
                        {
                            occupied.Add(new Obstacle(block));
                        }
                    }
                }

                if (arrange.BlockLines != null)
                {
                    foreach (GeoLine block in arrange.BlockLines)
                    {
                        if (seenLines.Add(block))
                        {
                            occupied.Add(new Obstacle(block));
                        }
                    }
                }
            }
            return occupied;
        }

        /// <summary>
        /// Kiểm xem hình chữ nhật nhãn sau dịch chuyển có đè lên bất kỳ vật cản nào không.
        /// </summary>
        internal static bool Collides(List<Obstacle> obstacles, GeoRectangle moved, Tolerance tolerance)
        {
            var movedBox = Bounds.Of(moved);

            foreach (Obstacle obstacle in obstacles)
            {
                // Lọc thô bằng bounding box (AABB) trước để tăng hiệu năng kiểm tra va chạm
                if (!movedBox.Overlaps(obstacle.Box))
                {
                    continue;
                }

                // Kiểm tra va chạm chi tiết theo kiểu hình học cụ thể
                switch (obstacle.Type)
                {
                    case ObstacleType.GeoRectangle:
                        // OBB vs OBB: Sử dụng thuật toán SAT (Separating Axis Theorem)
                        if (moved.IntersectsWith(obstacle.GeoRectangle, tolerance))
                            return true;
                        break;
                    case ObstacleType.GeoPolygon:
                        // OBB vs Đa giác: Kiểm tra giao cắt cạnh và bao chứa
                        if (moved.IntersectsWith(obstacle.GeoPolygon, tolerance))
                            return true;
                        break;
                    case ObstacleType.GeoLine:
                        // OBB vs Đoạn thẳng: Kiểm tra giao cắt cạnh và điểm mút
                        if (moved.IntersectsWith(obstacle.GeoLine, tolerance))
                            return true;
                        break;
                }
            }

            return false;
        }

        /// <summary>
        /// Xác minh lại kết quả trên bố cục cuối cùng và cập nhật cờ <see cref="Placed"/> cho từng nhãn.
        /// <para>
        /// Từng thuật toán chỉ biết trạng thái tại thời điểm nó đặt nhãn, nên cờ do chúng tự đặt mang nghĩa
        /// "lúc đến lượt tôi thì chỗ này trống". Một nhãn xếp sau khi bí có thể lùi về vị trí đè lên nhãn đã đặt,
        /// khiến nhãn bị đè vẫn báo thành công. Người dùng cần biết bố cục cuối cùng có chồng lấn hay không,
        /// nên phép kiểm tra dứt điểm phải nằm ở đây, sau khi mọi nhãn đã yên vị.
        /// </para>
        /// </summary>
        internal static void MarkPlacementResults(List<Arrange> arranges, IList<GeoVector> translations, ArrangeOptions options)
        {
            var staticObstacles = CollectStaticObstacles(arranges);

            var finalBoxes = new GeoRectangle[arranges.Count];
            var finalBounds = new Bounds[arranges.Count];
            for (int i = 0; i < arranges.Count; i++)
            {
                if (arranges[i] == null) continue;

                finalBoxes[i] = new GeoRectangle(
                    arranges[i].GeoRectangle.Center.Add(translations[i]),
                    arranges[i].GeoRectangle.Width,
                    arranges[i].GeoRectangle.Height,
                    arranges[i].GeoRectangle.AngleRad);
                finalBounds[i] = Bounds.Of(finalBoxes[i]);
            }

            for (int i = 0; i < arranges.Count; i++)
            {
                if (arranges[i] == null) continue;

                // Nhãn không dựng nổi bố cục thì chưa từng được sắp xếp. Nó ngẫu nhiên không đè ai
                // không có nghĩa là thành công, nên phải loại trước khi xét va chạm.
                if (!arranges[i].TryGetLayout(options, out _))
                {
                    arranges[i].SetPlaced(false);
                    continue;
                }

                bool clean = !Collides(staticObstacles, finalBoxes[i], options.Tolerance);

                for (int j = 0; j < arranges.Count && clean; j++)
                {
                    if (i == j || arranges[j] == null) continue;
                    if (!finalBounds[i].Overlaps(finalBounds[j])) continue;
                    if (finalBoxes[i].IntersectsWith(finalBoxes[j], options.Tolerance)) clean = false;
                }

                arranges[i].SetPlaced(clean);
            }
        }

        /// <summary>Sinh danh sách vị trí ứng viên với cấu hình mặc định.</summary>
        public List<GeoPoint> GetPlacePoints()
        {
            return GetPlacePoints(ArrangeOptions.Default);
        }

        /// <summary>Sinh danh sách các vị trí ứng viên cho tâm nhãn.</summary>
        public List<GeoPoint> GetPlacePoints(ArrangeOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return EnumeratePlacePoints(options).ToList();
        }

        /// <summary>
        /// Bộ sinh lặp (iterator) các điểm ứng viên tiềm năng cho tâm nhãn.
        /// Quá trình loang điểm bắt đầu từ trung điểm của đoạn dẫn (Anchor):
        /// - Dịch chuyển vuông góc (perpendicular offset) để tạo ra các hàng nhãn khác nhau.
        /// - Trượt dọc (longitudinal shift) dọc theo hướng đoạn dẫn song song.
        /// </summary>
        internal IEnumerable<GeoPoint> EnumeratePlacePoints(ArrangeOptions options)
        {
            if (!TryGetLayout(options, out Layout layout))
            {
                yield break;
            }

            int produced = 0;

            // Tính bước trượt dọc động dựa trên 5% chiều dài trượt tối đa.
            // Áp dụng ngưỡng bảo vệ tối thiểu là 0.1 để tránh bước nhảy bằng không gây vòng lặp vô hạn.
            double step = layout.MaximumShift / 20.0;
            if (step < 0.1)
            {
                step = layout.Height;
            }

            // Duyệt qua từng cấp độ khoảng cách vuông góc (từng hàng nhãn)
            for (int level = 0; level < options.PerpendicularLevels; level++)
            {
                double offset = layout.BaseOffset + level * (layout.Height + options.RowGap);

                // Dịch chuyển vuông góc thuần túy (không trượt dọc): bên phải và bên trái
                yield return layout.Anchor + layout.Perpendicular * offset;
                yield return layout.Anchor + layout.Perpendicular * -offset;
                produced += 2;

                double shift = step;

                // Trượt dọc nhãn theo cả hai chiều (tiến và lùi) song song với hướng đối tượng
                while (shift <= layout.MaximumShift && produced < options.MaximumCandidates)
                {
                    // Hàng trên/phải - trượt lùi
                    yield return layout.Anchor + layout.Perpendicular * offset - layout.Direction * shift;
                    // Hàng dưới/trái - trượt lùi
                    yield return layout.Anchor + layout.Perpendicular * -offset - layout.Direction * shift;
                    // Hàng trên/phải - trượt tiến
                    yield return layout.Anchor + layout.Perpendicular * offset + layout.Direction * shift;
                    // Hàng dưới/trái - trượt tiến
                    yield return layout.Anchor + layout.Perpendicular * -offset + layout.Direction * shift;

                    produced += 4;
                    shift += step;
                }
            }
        }

        /// <summary>
        /// Tính toán các tham số bố cục hình học cơ sở dựa trên đường dẫn và kích thước nhãn.
        /// </summary>
        internal bool TryGetLayout(ArrangeOptions options, out Layout layout)
        {
            layout = default(Layout);

            // BƯỚC 1: Kiểm tra tính hợp lệ ban đầu của kích thước hộp nhãn
            // Nếu chiều rộng hoặc chiều cao nhỏ hơn cấu hình tối thiểu, bỏ qua không xử lý để tránh lỗi chia 0 hoặc sai lệch hình học.
            // So sánh chặt (<): nhãn có kích thước đúng bằng ngưỡng vẫn là nhãn hợp lệ.
            if (GeoRectangle.Width < options.MinimumBoxSize || GeoRectangle.Height < options.MinimumBoxSize)
            {
                return false;
            }

            // BƯỚC 2: Xác định trục hướng (GeoVector đơn vị) dọc theo đường dẫn dẫn nhãn (Anchor GeoLine)
            // Trục này chỉ ra hướng mà nhãn có thể di chuyển trượt dọc.
            if (!GeoLine.Direction.TryGetNormal(out GeoVector direction))
            {
                return false;
            }

            // Xác định trục vuông góc với đường dẫn dẫn nhãn
            // Trục này chỉ ra hướng dịch chuyển nhãn ra xa hoặc lại gần đối tượng (tạo các hàng nhãn).
            GeoVector perpendicular = direction.GetPerpendicularVector();

            // Khởi tạo các giá trị cực trị để đo kích thước bao của nhãn sau khi chiếu lên hệ trục cục bộ mới
            double alongMin = double.MaxValue;
            double alongMax = double.MinValue;
            double acrossMin = double.MaxValue;
            double acrossMax = double.MinValue;

            // BƯỚC 3: Đo kích thước bao của nhãn theo hệ trục tọa độ cục bộ mới
            // Duyệt qua 4 đỉnh của hình chữ nhật nhãn (có thể đang bị xoay một góc bất kỳ)
            var vertices = GeoRectangle.GetVertices();
            for (int i = 0; i < vertices.Length; i++)
            {
                // Biến đổi tọa độ đỉnh thành GeoVector từ gốc tọa độ (0,0)
                GeoVector offset = new GeoPoint(0.0, 0.0).GetVectorTo(vertices[i]);

                // Chiếu đỉnh lên trục dọc của đường dẫn (tích vô hướng)
                double along = offset.DotProduct(direction);

                // Chiếu đỉnh lên trục vuông góc của đường dẫn (tích vô hướng)
                double across = offset.DotProduct(perpendicular);

                // Cập nhật các biên tọa độ tối đa, tối thiểu trên cả hai trục
                alongMin = Math.Min(alongMin, along);
                alongMax = Math.Max(alongMax, along);
                acrossMin = Math.Min(acrossMin, across);
                acrossMax = Math.Max(acrossMax, across);
            }

            // Tính toán chiều rộng và chiều cao thực tế của nhãn theo hệ trục cục bộ
            double width = alongMax - alongMin;
            double height = acrossMax - acrossMin;

            // Kiểm tra lại kích thước sau chiếu để đảm bảo không bị suy biến về 0
            if (width < options.MinimumBoxSize || height < options.MinimumBoxSize)
            {
                return false;
            }

            // BƯỚC 4: Thiết lập cấu trúc bố cục Layout hoàn chỉnh
            layout = new Layout(
                GeoLine.MidPoint, // Điểm neo xuất phát (trung điểm của đường dẫn)
                direction,     // Trục dọc cục bộ
                perpendicular, // Trục ngang/vuông góc cục bộ
                height,        // Chiều cao thực tế của nhãn theo trục vuông góc

                // BaseOffset: Khoảng cách vuông góc tối thiểu từ đường dẫn tới tâm hàng nhãn đầu tiên
                // bằng nửa chiều cao nhãn cộng thêm khoảng cách lề riêng của nhãn này (MarkOffsetFromLine)
                height * 0.5 + MarkOffsetFromLine,

                // MaximumShift: Khoảng cách trượt dọc tối đa cho phép dọc theo đường dẫn
                // bằng nửa chiều dài đường dẫn cộng thêm một tỷ lệ chiều rộng nhãn nhô ra ngoài (LongitudinalOvershootRatio)
                GeoLine.Length * 0.5 + width * options.LongitudinalOvershootRatio);

            return true;
        }

        /// <summary>
        /// Tính kích thước đường chéo lớn nhất của hộp nhãn.
        /// </summary>
        internal double GetBoxSpan(ArrangeOptions options)
        {
            var vertices = GeoRectangle.GetVertices();
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;
            foreach (var v in vertices)
            {
                if (v.X < minX) minX = v.X;
                if (v.Y < minY) minY = v.Y;
                if (v.X > maxX) maxX = v.X;
                if (v.Y > maxY) maxY = v.Y;
            }

            return Math.Max(maxX - minX, maxY - minY);
        }
    }

    /// <summary>
    /// Cấu trúc nội bộ chứa thông tin bố cục hình học cơ sở để sinh ứng viên.
    /// </summary>
    internal readonly struct Layout
    {
        internal Layout(GeoPoint anchor, GeoVector direction, GeoVector perpendicular,
            double height, double baseOffset, double maximumShift)
        {
            Anchor = anchor;
            Direction = direction;
            Perpendicular = perpendicular;
            Height = height;
            BaseOffset = baseOffset;
            MaximumShift = maximumShift;
        }

        internal GeoPoint Anchor { get; }
        internal GeoVector Direction { get; }
        internal GeoVector Perpendicular { get; }
        internal double Height { get; }
        internal double BaseOffset { get; }
        internal double MaximumShift { get; }
    }

    internal enum ObstacleType
    {
        GeoPolygon,
        GeoLine,
        GeoRectangle
    }

    /// <summary>
    /// Biểu diễn một vật cản tĩnh hoặc nhãn đã được chiếm dụng.
    /// </summary>
    internal readonly struct Obstacle
    {
        internal ObstacleType Type { get; }
        internal GeoPolygon GeoPolygon { get; }
        internal GeoLine GeoLine { get; }
        internal GeoRectangle GeoRectangle { get; }
        internal Bounds Box { get; }

        internal Obstacle(GeoPolygon polygon)
        {
            Type = ObstacleType.GeoPolygon;
            GeoPolygon = polygon;
            GeoLine = default(GeoLine);
            GeoRectangle = default(GeoRectangle);
            Box = Bounds.Of(polygon);
        }

        internal Obstacle(GeoLine line)
        {
            Type = ObstacleType.GeoLine;
            GeoPolygon = null;
            GeoLine = line;
            GeoRectangle = default(GeoRectangle);
            Box = Bounds.Of(line);
        }

        internal Obstacle(GeoRectangle rectangle)
        {
            Type = ObstacleType.GeoRectangle;
            GeoPolygon = null;
            GeoLine = default(GeoLine);
            GeoRectangle = rectangle;
            Box = Bounds.Of(rectangle);
        }
    }

    /// <summary>
    /// Biểu diễn hộp bao AABB để lọc va chạm nhanh.
    /// </summary>
    internal readonly struct Bounds
    {
        internal double MinX { get; }
        internal double MinY { get; }
        internal double MaxX { get; }
        internal double MaxY { get; }

        private Bounds(double minX, double minY, double maxX, double maxY)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }

        internal static Bounds Of(GeoPolygon GeoPolygon)
        {
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;
            int count = GeoPolygon.VertexCount;
            for (int i = 0; i < count; i++)
            {
                var v = GeoPolygon[i];
                if (v.X < minX) minX = v.X;
                if (v.Y < minY) minY = v.Y;
                if (v.X > maxX) maxX = v.X;
                if (v.Y > maxY) maxY = v.Y;
            }
            return new Bounds(minX, minY, maxX, maxY);
        }

        internal static Bounds Of(GeoLine GeoLine)
        {
            return new Bounds(
                Math.Min(GeoLine.StartPoint.X, GeoLine.EndPoint.X),
                Math.Min(GeoLine.StartPoint.Y, GeoLine.EndPoint.Y),
                Math.Max(GeoLine.StartPoint.X, GeoLine.EndPoint.X),
                Math.Max(GeoLine.StartPoint.Y, GeoLine.EndPoint.Y)
            );
        }

        internal static Bounds Of(GeoRectangle rect)
        {
            var vertices = rect.GetVertices();
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;
            foreach (var v in vertices)
            {
                if (v.X < minX) minX = v.X;
                if (v.Y < minY) minY = v.Y;
                if (v.X > maxX) maxX = v.X;
                if (v.Y > maxY) maxY = v.Y;
            }
            return new Bounds(minX, minY, maxX, maxY);
        }

        internal static Bounds Around(IReadOnlyList<GeoPoint> points)
        {
            double minX = points[0].X;
            double minY = points[0].Y;
            double maxX = minX;
            double maxY = minY;

            for (int i = 1; i < points.Count; i++)
            {
                minX = Math.Min(minX, points[i].X);
                minY = Math.Min(minY, points[i].Y);
                maxX = Math.Max(maxX, points[i].X);
                maxY = Math.Max(maxY, points[i].Y);
            }

            return new Bounds(minX, minY, maxX, maxY);
        }

        internal Bounds Expand(double margin)
        {
            return new Bounds(MinX - margin, MinY - margin, MaxX + margin, MaxY + margin);
        }

        internal bool Overlaps(Bounds other)
        {
            return MinX <= other.MaxX && other.MinX <= MaxX
                                      && MinY <= other.MaxY && other.MinY <= MaxY;
        }
    }
}

