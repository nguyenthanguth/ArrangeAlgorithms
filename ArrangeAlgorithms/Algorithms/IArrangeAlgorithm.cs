using ArrangeAlgorithms.Geometry;
using System.Collections.Generic;

namespace ArrangeAlgorithms.Algorithms
{
    /// <summary>
    /// Giao diện chung cho các thuật toán sắp xếp nhãn.
    /// </summary>
    internal interface IArrangeAlgorithm
    {
        /// <summary>
        /// Thực hiện sắp xếp danh sách nhãn.
        /// </summary>
        /// <param name="arranges">Danh sách nhãn cần sắp xếp.</param>
        /// <param name="options">Cấu hình điều khiển thuật toán.</param>
        /// <returns>Danh sách GeoVector dịch chuyển tương ứng cho từng nhãn.</returns>
        List<GeoVector> Arrange(List<Arrange> arranges, ArrangeOptions options);
    }
}

