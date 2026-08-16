using System;
using System.Collections.Generic;
using System.Linq;
using ArrangeAlgorithms.Geometry;

namespace ArrangeAlgorithms.Algorithms
{
    /// <summary>
    /// Thuật toán sắp xếp nhãn dựa trên lý thuyết Thỏa mãn ràng buộc (Constraint Satisfaction Problem - CSP).
    /// Áp dụng kỹ thuật duyệt quay lui kết hợp chọn biến MRV (Minimum Remaining Values) và kỹ thuật lọc sớm Forward Checking.
    /// </summary>
    internal class ConstraintSatisfactionAlgorithm : IArrangeAlgorithm
    {
        private class CSPVariable
        {
            public int OriginalIndex { get; }
            public Arrange Arrange { get; }
            // Danh sách các ứng viên dịch chuyển hợp lệ ban đầu (không va chạm tĩnh)
            public List<GeoVector> Domain { get; set; }
            public GeoVector AssignedValue { get; set; }
            public bool IsAssigned { get; set; }

            public CSPVariable(int originalIndex, Arrange arrange)
            {
                OriginalIndex = originalIndex;
                Arrange = arrange;
                Domain = new List<GeoVector>();
                AssignedValue = GeoVector.Zero;
                IsAssigned = false;
            }
        }

        private int _backtrackSteps;
        private bool _isTimeout;

        public List<GeoVector> Arrange(List<Arrange> arranges, ArrangeOptions options)
        {
            var translations = new GeoVector[arranges.Count];
            // BƯỚC 1: Thu thập vật cản tĩnh ban đầu
            var staticObstacles = ArrangeAlgorithms.Arrange.CollectStaticObstacles(arranges);

            // BƯỚC 2: Khởi tạo các biến CSP và lọc miền giá trị (Domain) ban đầu
            var variables = new List<CSPVariable>();
            for (int i = 0; i < arranges.Count; i++)
            {
                var arrange = arranges[i];
                if (arrange == null) continue;

                var v = new CSPVariable(i, arrange);
                GeoPoint centre = arrange.GeoRectangle.Center;

                // Bỏ trước các vật cản nằm ngoài tầm với của nhãn. Vòng dưới chạy
                // (số ứng viên × số vật cản) lần nên lọc một lượt ở đây cắt được phần lớn công việc.
                // Nhãn suy biến không dựng nổi vùng thì giữ nguyên danh sách đầy đủ.
                List<Obstacle> nearby = PlacementHeuristics.TryGetCandidateBounds(arrange, options, out Bounds region)
                    ? staticObstacles.Where(o => region.Overlaps(o.Box)).ToList()
                    : staticObstacles;

                foreach (GeoPoint candidate in arrange.EnumeratePlacePoints(options))
                {
                    GeoVector trans = centre.GetVectorTo(candidate);
                    GeoRectangle moved = new GeoRectangle(arrange.GeoRectangle.Center.Add(trans), arrange.GeoRectangle.Width, arrange.GeoRectangle.Height, arrange.GeoRectangle.AngleRad);

                    // Chỉ đưa vào Domain nếu ứng viên đó không va chạm với vật cản tĩnh từ đầu
                    if (!ArrangeAlgorithms.Arrange.Collides(nearby, moved, options.Tolerance))
                    {
                        v.Domain.Add(trans);
                    }
                }

                // Sắp xếp trước Domain: các ứng viên có độ trượt dọc nhỏ hơn được xếp trước
                v.Domain = v.Domain.OrderBy(t => t.Length).ToList();
                variables.Add(v);
            }

            _backtrackSteps = 0;
            _isTimeout = false;

            // BƯỚC 3: Giải bài toán thỏa mãn ràng buộc
            bool success = SolveCSP(variables, options);

            // BƯỚC 4: Nếu CSP thất bại hoàn toàn (không có giải pháp sạch),
            // tự động lùi về giải thuật tham lam để giữ tính khả dụng
            if (!success)
            {
                var greedy = new GreedyAlgorithm();
                return greedy.Arrange(arranges, options);
            }

            // BƯỚC 5: Tổng hợp kết quả dịch chuyển
            for (int i = 0; i < arranges.Count; i++)
            {
                if (arranges[i] == null) continue;

                var variable = variables.Find(v => v.OriginalIndex == i);
                translations[i] = variable != null ? variable.AssignedValue : GeoVector.Zero;
            }

            return translations.ToList();
        }

        /// <summary>
        /// Giải đệ quy CSP sử dụng heuristics MRV và kỹ thuật lọc sớm Forward Checking.
        /// </summary>
        private bool SolveCSP(List<CSPVariable> variables, ArrangeOptions options)
        {
            // Kiểm tra số lượng đã gán
            var unassigned = variables.Where(v => !v.IsAssigned).ToList();
            if (unassigned.Count == 0)
            {
                return true;
            }

            _backtrackSteps++;
            if (_backtrackSteps > options.MaxBacktrackSteps)
            {
                _isTimeout = true;
                return false;
            }

            // HEURISTIC MRV: Chọn biến chưa được gán có số lượng phần tử miền giá trị (Domain) nhỏ nhất
            CSPVariable currentVar = unassigned.OrderBy(v => v.Domain.Count).First();

            if (currentVar.Domain.Count == 0)
            {
                // Bị kẹt: Biến chưa gán nhưng không còn ứng viên hợp lệ nào
                return false;
            }

            // Lưu trữ bản sao của các miền giá trị để khôi phục khi quay lui (backtrack)
            var domainsBackup = variables.ToDictionary(v => v.OriginalIndex, v => v.Domain.ToList());

            // Thử từng giá trị dịch chuyển trong miền của biến hiện tại
            foreach (GeoVector val in currentVar.Domain)
            {
                currentVar.AssignedValue = val;
                currentVar.IsAssigned = true;

                // LỌC SỚM (Forward Checking): Lọc miền giá trị của các biến chưa gán khác
                bool forwardCheckOk = true;
                GeoRectangle currentRect = new GeoRectangle(
                    currentVar.Arrange.GeoRectangle.Center.Add(val),
                    currentVar.Arrange.GeoRectangle.Width,
                    currentVar.Arrange.GeoRectangle.Height,
                    currentVar.Arrange.GeoRectangle.AngleRad);

                foreach (var otherVar in variables.Where(v => !v.IsAssigned))
                {
                    // Loại bỏ các ứng viên của otherVar gây va chạm với nhãn currentVar vừa gán
                    var newDomain = new List<GeoVector>();
                    foreach (GeoVector otherVal in otherVar.Domain)
                    {
                        GeoRectangle otherRect = new GeoRectangle(
                            otherVar.Arrange.GeoRectangle.Center.Add(otherVal),
                            otherVar.Arrange.GeoRectangle.Width,
                            otherVar.Arrange.GeoRectangle.Height,
                            otherVar.Arrange.GeoRectangle.AngleRad);

                        if (!currentRect.IntersectsWith(otherRect))
                        {
                            newDomain.Add(otherVal);
                        }
                    }

                    otherVar.Domain = newDomain;

                    // Nếu miền của bất kỳ biến nào trở thành rỗng -> thất bại sớm
                    if (otherVar.Domain.Count == 0)
                    {
                        forwardCheckOk = false;
                        break;
                    }
                }

                if (forwardCheckOk)
                {
                    // Tiến hành đệ quy gán biến tiếp theo
                    if (SolveCSP(variables, options))
                    {
                        return true;
                    }
                }

                // QUAY LUI: Khôi phục lại trạng thái miền giá trị cũ
                currentVar.IsAssigned = false;
                foreach (var v in variables)
                {
                    v.Domain = domainsBackup[v.OriginalIndex].ToList();
                }

                if (_isTimeout)
                {
                    return false;
                }
            }

            return false;
        }
    }
}

