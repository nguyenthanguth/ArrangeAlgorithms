# ArrangeAlgorithms

Thư viện sắp xếp nhãn (label placement) 2D cho bản vẽ kỹ thuật: cho một tập nhãn, mỗi nhãn gắn với một
đoạn dẫn và các vùng cấm xung quanh, thư viện tính vector dịch chuyển để các nhãn không đè lên nhau và
không đè lên vùng cấm.

Thư viện thuần hình học, không phụ thuộc AutoCAD. Project `ArrangeAlgorithms.CadTest` là plugin dùng để
chạy thử trực quan trong AutoCAD, tách riêng.

## Cấu trúc

| Project | Vai trò | Target |
|---|---|---|
| `ArrangeAlgorithms` | Thư viện lõi: kiểu hình học + 5 thuật toán | net48 |
| `ArrangeAlgorithms.UnitTest` | Bộ test xUnit | net48 |
| `ArrangeAlgorithms.CadTest` | Plugin AutoCAD 2021 để chạy thử trực quan | net48 |

## Dùng nhanh

```csharp
var leader = new GeoLine(0.0, 0.0, 2000.0, 0.0);

var arranges = new List<Arrange>
{
    new Arrange
    {
        // Hộp bao nhãn: tâm, rộng, cao, góc xoay (radian, ngược chiều kim đồng hồ)
        GeoRectangle = new GeoRectangle(new GeoPoint(1000.0, 0.0), 2000.0, 1000.0),
        // Đoạn dẫn: trung điểm của nó là gốc để loang vị trí ứng viên
        GeoLine      = leader,
        // Các vùng nhãn không được đè lên
        BlockPolygons = new List<GeoPolygon>(),
        BlockLines    = new List<GeoLine>()
    }
};

// Trả về vector dịch chuyển cho từng nhãn, đúng thứ tự đầu vào
List<GeoVector> moves = Arrange.Run(arranges);

for (int i = 0; i < arranges.Count; i++)
{
    GeoPoint viTriMoi = arranges[i].GeoRectangle.Center + moves[i];
    bool datDuoc = arranges[i].Placed; // false = phải lùi về phương án dự phòng, vẫn còn chồng lấn
}
```

Muốn đổi thuật toán hoặc tinh chỉnh tham số thì truyền `ArrangeOptions`:

```csharp
var options = new ArrangeOptions
{
    Algorithm           = ArrangeAlgorithmType.BoundedBacktracking,
    MarkOffsetFromLine  = 50.0,
    RowGap              = 20.0,
    PerpendicularLevels = 3
};

List<GeoVector> moves = Arrange.Run(arranges, options);
```

## Cách sinh vị trí ứng viên

Cả 5 thuật toán đều dùng chung một bộ ứng viên rời rạc, loang ra từ trung điểm đoạn dẫn:

- **Dịch vuông góc** — mỗi cấp trong `PerpendicularLevels` tạo một hàng nhãn, đối xứng hai bên đoạn dẫn.
  Cấp đầu cách đoạn dẫn nửa chiều cao nhãn cộng `MarkOffsetFromLine`, mỗi cấp sau cộng thêm chiều cao
  nhãn cộng `RowGap`.
- **Trượt dọc** — trong mỗi hàng, nhãn trượt song song đoạn dẫn theo cả hai chiều, xa nhất là nửa chiều
  dài đoạn dẫn cộng `LongitudinalOvershootRatio` lần chiều rộng nhãn.

Các thuật toán chỉ khác nhau ở cách **chọn** trong tập ứng viên đó.

## Năm thuật toán

| `ArrangeAlgorithmType` | Cách chọn | Đánh đổi |
|---|---|---|
| `Greedy` (mặc định) | Đặt tuần tự, ưu tiên nhãn bị bó hẹp nhất; trong nhóm ứng viên trống đầu tiên chọn chỗ thoáng nhất | Nhanh nhất, kết quả tái lập được, nhưng dễ kẹt ở tối ưu cục bộ |
| `BoundedBacktracking` | Như Greedy nhưng quay lui khi nhãn sau bị kẹt, chặn bởi `MaxBacktrackSteps` | Tỷ lệ đặt sạch cao hơn, chậm hơn khi bản vẽ dày đặc |
| `SimulatedAnnealing` | Tối ưu toàn cục theo hàm năng lượng phạt va chạm, hạ nhiệt dần | Tốt với bản vẽ chằng chịt, tốn CPU |
| `ForceDirected` | Mô phỏng lò xo và lực đẩy, sau đó ánh xạ về ứng viên rời rạc gần nhất | Phân bố đều và tự nhiên |
| `ConstraintSatisfaction` | CSP với heuristic MRV và forward checking | Chặt chẽ nhất, có thể bùng nổ tổ hợp khi số nhãn lớn |

`BoundedBacktracking` và `ConstraintSatisfaction` tự động lùi về `Greedy` nếu không tìm được lời giải
sạch va chạm nào, nên mọi nhãn luôn có vị trí hiển thị.

Kết quả của `SimulatedAnnealing` dùng seed cố định nên vẫn tái lập được giữa các lần chạy.

## Tham số chính của `ArrangeOptions`

| Tham số | Mặc định | Ý nghĩa |
|---|---|---|
| `Algorithm` | `Greedy` | Thuật toán sử dụng |
| `MarkOffsetFromLine` | 50.0 | Khoảng hở vuông góc tối thiểu giữa mép nhãn và đoạn dẫn |
| `RowGap` | 20.0 | Khoảng hở giữa hai hàng nhãn liên tiếp |
| `PerpendicularLevels` | 3 | Số cấp lùi vuông góc thử ở mỗi bên |
| `LongitudinalOvershootRatio` | 0.75 | Tỷ lệ chiều rộng nhãn được phép nhô ra ngoài hai đầu đoạn dẫn |
| `MinimumBoxSize` | 10.0 | Nhãn nhỏ hơn kích thước này bị bỏ qua |
| `MinimumMoveDistance` | 0.1 | Dịch chuyển nhỏ hơn ngưỡng này bị làm tròn về không |
| `NeighbourMargin` | 50.0 | Biên nới rộng khi lọc vật cản lân cận |
| `PlaceMostConstrainedFirst` | true | Đặt nhãn ít lựa chọn nhất trước |
| `PlaceFromInsideOut` | true | Ưu tiên nhãn gần trọng tâm khu vực |
| `LookAheadCandidates` | 3 | Số vị trí trống được cân nhắc trước khi chọn |
| `MaxBacktrackSteps` | 1000 | Trần số bước quay lui |
| `Tolerance` | `Tolerance.Global` | Dung sai cho các phép so sánh hình học |

Các giá trị mặc định tính theo milimét, hợp với bản vẽ kết cấu thông thường.

## Kiểu hình học

`GeoPoint`, `GeoVector`, `GeoLine`, `GeoRectangle` (hình chữ nhật xoay — OBB), `GeoPolygon`. Ba kiểu
hình có `IntersectsWith` đối xứng đầy đủ với nhau, mỗi cặp đều có overload nhận `Tolerance` tường minh:

```csharp
rect.IntersectsWith(line);         line.IntersectsWith(rect);
rect.IntersectsWith(poly);         poly.IntersectsWith(rect);
line.IntersectsWith(poly);         poly.IntersectsWith(line);
rect.IntersectsWith(otherRect);    poly.IntersectsWith(otherPoly);    line.IntersectsWith(otherLine);
```

`Tolerance.Global` là dung sai áp dụng cho các overload không truyền dung sai. Nó có setter tĩnh, cố ý
làm giống `Autodesk.AutoCAD.Geometry.Tolerance.Global`; đổi nó ảnh hưởng toàn ứng dụng nên chỉ nên đặt
một lần lúc khởi động.

## Build và test

```bash
dotnet build ArrangeAlgorithms/ArrangeAlgorithms.csproj
dotnet test  ArrangeAlgorithms.UnitTest/ArrangeAlgorithms.UnitTest.csproj
```

`ArrangeAlgorithms.CadTest` cần AutoCAD 2021 đã cài sẵn. Nếu cài ở đường dẫn khác, sửa `AutoCadPath`
trong file `.csproj`. Nạp DLL kết quả vào AutoCAD bằng lệnh `NETLOAD` rồi chạy một trong các lệnh
`T111_Greedy`, `T111_Backtracking`, `T111_SimulatedAnnealing`, `T111_ForceDirected`, `T111_CSP`: chọn
các đối tượng LINE hoặc LWPOLYLINE, plugin sẽ vẽ hộp nhãn trước và sau khi sắp xếp cùng số liệu thống kê.
