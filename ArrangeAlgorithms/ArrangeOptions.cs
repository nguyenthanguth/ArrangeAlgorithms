using ArrangeAlgorithms.Geometry;

namespace ArrangeAlgorithms
{
    /// <summary>
    /// Các chiến lược giải thuật sắp xếp nhãn.
    /// </summary>
    public enum ArrangeAlgorithmType
    {
        /// <summary>
        /// Thuật toán tham lam (Greedy):
        /// <para>- Cách hoạt động: Sắp xếp nhãn theo thứ tự ưu tiên (độ tự do hoặc hình học), đặt tuần tự từng nhãn vào vị trí ứng viên tốt nhất. Nhãn đã đặt trở thành vật cản tĩnh cho các nhãn sau.</para>
        /// <para>- Ưu điểm: Cực kỳ nhanh, độ phức tạp tính toán thấp, kết quả ổn định và dễ dự đoán (deterministic).</para>
        /// <para>- Nhược điểm: Dễ rơi vào tối ưu cục bộ, nhãn đặt trước chiếm chỗ tốt làm nhãn đặt sau bị kẹt va chạm.</para>
        /// </summary>
        Greedy,

        /// <summary>
        /// Thuật toán quay lui giới hạn (Bounded Backtracking):
        /// <para>- Cách hoạt động: Đặt nhãn tuần tự, nhưng khi nhãn thứ k bị kẹt va chạm, thuật toán sẽ quay lui lại nhãn k-1 để thử vị trí ứng viên tiếp theo của nó, giúp nhường chỗ. Khống chế tối đa MaxBacktrackSteps để tránh treo luồng.</para>
        /// <para>- Ưu điểm: Khắc phục hiệu quả hiện tượng tối ưu cục bộ của Greedy, tăng cao tỷ lệ sắp xếp thành công sạch va chạm.</para>
        /// <para>- Nhược điểm: Tốc độ chậm hơn Greedy khi gặp bản vẽ quá dày đặc va chạm và phải quay lui thử nhiều trường hợp.</para>
        /// </summary>
        BoundedBacktracking,

        /// <summary>
        /// Thuật toán luyện kim giả lập (Simulated Annealing):
        /// <para>- Cách hoạt động: Xếp toàn bộ nhãn ở vị trí mặc định. Chạy tối ưu hóa ngẫu nhiên dựa trên sự giảm dần của nhiệt độ giả lập. Trong mỗi chu kỳ, thay đổi ngẫu nhiên vị trí của một nhãn và quyết định chấp nhận dựa trên mức độ giảm va chạm toàn cục (hoặc xác suất Boltzmann nếu năng lượng xấu đi).</para>
        /// <para>- Ưu điểm: Khả năng tìm được lời giải tối ưu toàn cục rất tốt ngay cả khi bản vẽ chằng chịt va chạm nặng nề.</para>
        /// <para>- Nhược điểm: Không dự đoán chính xác kết quả giữa các lần chạy khác nhau (non-deterministic) và tốn nhiều CPU hơn.</para>
        /// </summary>
        SimulatedAnnealing,

        /// <summary>
        /// Thuật toán lực đẩy vật lý (Force-directed / Spring embedder):
        /// <para>- Cách hoạt động: Giả lập hệ thống vật lý liên tục nơi các nhãn tự đẩy nhau (lực Coulomb) và lò xo kéo nhãn về vị trí gốc. Sau khi chạy một số bước giả lập, thực hiện ánh xạ rời rạc (Discrete Mapping) tâm nhãn liên tục về vị trí ứng viên rời rạc gần nhất không va chạm.</para>
        /// <para>- Ưu điểm: Nhãn phân bổ rất tự nhiên, đều đặn, trực quan sinh động.</para>
        /// <para>- Nhược điểm: Tính toán lực vật lý liên tục khá phức tạp, đôi khi nhãn có thể bị đẩy lệch lạc nếu lực đẩy quá cực đoan.</para>
        /// </summary>
        ForceDirected,

        /// <summary>
        /// Thuật toán thỏa mãn ràng buộc (Constraint Satisfaction Problem - CSP):
        /// <para>- Cách hoạt động: Coi mỗi nhãn là một biến và các ứng viên rời rạc là miền giá trị. Chạy giải thuật thỏa mãn ràng buộc bằng cách gán giá trị kết hợp MRV Heuristic (biến ít giá trị nhất gán trước) và Forward Checking (cắt tỉa sớm các ứng viên va chạm ở các biến lân cận).</para>
        /// <para>- Ưu điểm: Giải toán bằng logic toán học thuần túy cực kỳ chặt chẽ, tối ưu hóa các ràng buộc tránh chồng chéo tuyệt đối.</para>
        /// <para>- Nhược điểm: Độ phức tạp thuật toán cao, số lượng nhãn lớn có thể làm bùng nổ tổ hợp nếu miền giá trị quá rộng và sâu.</para>
        /// </summary>
        ConstraintSatisfaction
    }

    /// <summary>
    /// Các tham số điều khiển việc sinh vị trí ứng viên và kiểm tra va chạm khi sắp xếp nhãn.
    /// Các giá trị thường được tính theo milimét, phù hợp với bản vẽ kết cấu thông thường.
    /// </summary>
    public sealed class ArrangeOptions
    {
        /// <summary>
        /// Bộ tham số cấu hình mặc định, được sử dụng bởi <see cref="Arrange.Run(System.Collections.Generic.List{Arrange})"/>.
        /// </summary>
        public static ArrangeOptions Default { get; } = new ArrangeOptions();

        /// <summary>
        /// Thuật toán sắp xếp nhãn được sử dụng.
        /// </summary>
        public ArrangeAlgorithmType Algorithm { get; set; } = ArrangeAlgorithmType.Greedy;

        /// <summary>
        /// Khoảng hở vuông góc tối thiểu giữa mép nhãn và đường dẫn.
        /// Cộng với nửa chiều cao nhãn sẽ ra khoảng cách từ đường dẫn tới tâm nhãn.
        /// </summary>
        public double MarkOffsetFromLine { get; set; } = 50.0;

        /// <summary>
        /// Khoảng hở giữa hai hàng nhãn liên tiếp, được cộng vào chiều cao nhãn
        /// khi dịch ra cấp vuông góc tiếp theo. Giúp giữ các hàng nhãn không bị chồng đè lên nhau.
        /// </summary>
        public double RowGap { get; set; } = 20.0;

        /// <summary>
        /// Số cấp lùi vuông góc để thử nghiệm ở mỗi bên của đường dẫn.
        /// </summary>
        public int PerpendicularLevels { get; set; } = 3;

        /// <summary>
        /// Tỷ lệ chiều rộng nhãn được cộng thêm vào nửa chiều dài đoạn dẫn để xác định giới hạn trượt dọc.
        /// Cho phép nhãn nhô ra ngoài hai đầu đoạn dẫn một chút.
        /// </summary>
        public double LongitudinalOvershootRatio { get; set; } = 0.75;

        /// <summary>
        /// Các nhãn nhỏ hơn kích thước này sẽ bị coi là không hợp lệ/lỗi và bị bỏ qua.
        /// </summary>
        public double MinimumBoxSize { get; set; } = 10.0;

        /// <summary>
        /// Khoảng cách dịch chuyển nhỏ hơn giá trị này sẽ bị bỏ qua.
        /// Tránh việc điều chỉnh nhỏ cho các nhãn vốn dĩ đã nằm đúng vị trí.
        /// </summary>
        public double MinimumMoveDistance { get; set; } = 0.1;

        /// <summary>
        /// Biên độ nới rộng hộp lọc lân cận khi lựa chọn các vật cản cần xét va chạm.
        /// Được cộng thêm vào kích thước lớn nhất của nhãn.
        /// </summary>
        public double NeighbourMargin { get; set; } = 50.0;

        /// <summary>
        /// Số lượng vị trí ứng viên tối đa được sinh ra cho một nhãn để tránh vòng lặp vô hạn.
        /// </summary>
        public int MaximumCandidates { get; set; } = 10000;

        /// <summary>
        /// Sắp xếp thứ tự đặt nhãn sao cho nhãn bị ràng buộc nhiều nhất (ít lựa chọn nhất) được đặt trước.
        /// Khuyến nghị sử dụng vì thuật toán đặt tham lam không có cơ chế quay lui (backtrack).
        /// Đặt bằng false để sắp xếp thuần túy theo thứ tự hình học mặc định.
        /// </summary>
        public bool PlaceMostConstrainedFirst { get; set; } = true;

        /// <summary>
        /// Số lượng ứng viên được đánh giá khi đo lường mức độ tự do tự tại của nhãn,
        /// phục vụ cho tùy chọn <see cref="PlaceMostConstrainedFirst"/>.
        /// </summary>
        public int FreedomSampleSize { get; set; } = 12;

        /// <summary>
        /// Số lượng vị trí trống được đánh giá trước khi lựa chọn.
        /// Trong số các ứng viên này, vị trí có khoảng hở lớn nhất sẽ thắng.
        /// Đặt bằng 1 để chọn ngay vị trí trống đầu tiên tìm thấy.
        /// </summary>
        public int LookAheadCandidates { get; set; } = 3;

        /// <summary>
        /// Sắp xếp nhãn theo thứ tự từ bên trong (trọng tâm khu vực) ra bên ngoài biên.
        /// Giúp các nhãn ở vùng trung tâm đông đúc được ưu tiên đặt trước.
        /// </summary>
        public bool PlaceFromInsideOut { get; set; } = true;

        /// <summary>
        /// Số bước quay lui tối đa của thuật toán Bounded Backtracking.
        /// </summary>
        public int MaxBacktrackSteps { get; set; } = 1000;

        /// <summary>
        /// Nhiệt độ ban đầu của thuật toán Simulated Annealing.
        /// </summary>
        public double AnnealingInitialTemperature { get; set; } = 100.0;

        /// <summary>
        /// Tỷ lệ hạ nhiệt của thuật toán Simulated Annealing.
        /// </summary>
        public double AnnealingCoolingRate { get; set; } = 0.95;

        /// <summary>
        /// Số vòng lặp mô phỏng lực vật lý của thuật toán Force-directed.
        /// </summary>
        public int ForceIterations { get; set; } = 100;

        /// <summary>
        /// Dung sai sử dụng cho các tính toán hình học và kiểm tra giao cắt.
        /// </summary>
        public Tolerance Tolerance { get; set; } = Tolerance.Global;
    }
}

