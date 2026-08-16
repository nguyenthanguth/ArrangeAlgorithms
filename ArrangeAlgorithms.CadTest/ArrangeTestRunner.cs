using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using ArrangeAlgorithms;
using ArrangeAlgorithms.Geometry;

[assembly: CommandClass(typeof(ArrangeAlgorithms.CadTest.ArrangeCommands))]

namespace ArrangeAlgorithms.CadTest
{
    /// <summary>
    /// Lệnh thử nghiệm thuật toán sắp xếp nhãn ArrangeDrawing trong AutoCAD.
    /// Quét chọn các thực thể GeoLine và LWPOLYLINE, giả định mỗi đối tượng có một nhãn xoay (GeoRectangle OBB)
    /// tương ứng, chạy sắp xếp nhãn để tránh chồng lấn và vẽ kết quả lên bản vẽ.
    /// </summary>
    public class ArrangeTestRunner
    {
        private const double BoxWidth = 2000.0;
        private const double BoxHeight = 1000.0;

        private const string BoxFromLayer = "BoxFrom";
        private const string BoxToLayer = "BoxTo";
        private const string LineMoveLayer = "LineMove";

        private const short OriginalColour = 8; // Màu xám: Hộp ở vị trí giả định ban đầu
        private const short PlacedColour = 3; // Màu xanh lá: Tìm được chỗ trống và sắp xếp thành công
        private const short FallbackColour = 1; // Màu đỏ: Bị vướng va chạm, phải dùng phương án lùi
        private const short LeaderColour = 253; // Màu xám nhạt: Đường dẫn nối từ đối tượng đến nhãn mới

        /// <summary>
        /// Thực thi logic sắp xếp nhãn chung cho tất cả các loại thuật toán.
        /// </summary>
        public void RunArrangeTest(ArrangeAlgorithmType algorithmType, string algorithmName)
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            if (document == null)
            {
                return;
            }

            Editor editor = document.Editor;
            Database database = document.Database;

            try
            {
                PromptSelectionResult selection = editor.GetSelection();
                if (selection.Status != PromptStatus.OK)
                {
                    editor.WriteMessage("\nĐã hủy: không chọn được đối tượng nào.");
                    return;
                }

                var arranges = new List<Arrange>();
                var skipped = 0;

                using Transaction transaction = database.TransactionManager.StartTransaction();
                var selectedLines = new List<GeoLine>();
                var entities = new List<GeoLine>();

                foreach (ObjectId id in selection.Value.GetObjectIds())
                {
                    DBObject dbObject = transaction.GetObject(id, OpenMode.ForRead);

                    if (dbObject is Entity entity)
                    {
                        if (!TryGetLeaderLine(entity, out GeoLine leader))
                        {
                            skipped++;
                            continue;
                        }

                        selectedLines.Add(leader);
                        entities.Add(leader);
                    }
                }

                foreach (GeoLine leader in entities)
                {
                    arranges.Add(new Arrange
                    {
                        GeoLine = leader,
                        // Tạo hộp nhãn xoay dọc theo hướng của đối tượng dẫn
                        GeoRectangle = MakeBox(leader, BoxWidth, BoxHeight),
                        BlockPolygons = new List<GeoPolygon>(),
                        // Né tránh tất cả các đoạn dẫn được chọn khác
                        BlockLines = selectedLines.FindAll(l => !l.Equals(leader))
                    });
                }

                if (arranges.Count == 0)
                {
                    editor.WriteMessage("\nKhông có đối tượng nào hợp lệ để sắp xếp.");
                    transaction.Commit();
                    return;
                }

                var options = new ArrangeOptions
                {
                    Algorithm = algorithmType
                };

                var stopwatch = Stopwatch.StartNew();
                List<GeoVector> moves = Arrange.Run(arranges, options);
                stopwatch.Stop();

                DrawResult(database, transaction, arranges, moves, algorithmName, stopwatch.ElapsedMilliseconds);
                transaction.Commit();

                Report(editor, arranges, skipped, stopwatch.ElapsedMilliseconds, algorithmName);
            }
            catch (System.Exception ex)
            {
                editor.WriteMessage(string.Format(
                    CultureInfo.CurrentCulture,
                    "\nLỗi khi thực thi sắp xếp nhãn [{0}]: {1}\nChi tiết: {2}",
                    algorithmName, ex.Message, ex.StackTrace));
            }
        }

        /// <summary>
        /// Trích xuất đoạn dẫn hình học từ thực thể AutoCAD.
        /// Đối với LWPOLYLINE thì lấy đoạn thẳng dài nhất.
        /// </summary>
        private static bool TryGetLeaderLine(Entity entity, out GeoLine leader)
        {
            leader = new GeoLine(new GeoPoint(), new GeoPoint());

            if (entity is Line lineEntity)
            {
                leader = new GeoLine(
                    lineEntity.StartPoint.X, lineEntity.StartPoint.Y,
                    lineEntity.EndPoint.X, lineEntity.EndPoint.Y);

                return leader.Length > 0.0;
            }

            if (!(entity is Polyline polyline) || polyline.NumberOfVertices < 2)
            {
                return false;
            }

            double longest = 0.0;
            int segmentCount = polyline.Closed ? polyline.NumberOfVertices : polyline.NumberOfVertices - 1;

            for (int i = 0; i < segmentCount; i++)
            {
                Point2d start = polyline.GetPoint2dAt(i);
                Point2d end = polyline.GetPoint2dAt((i + 1) % polyline.NumberOfVertices);

                var candidate = new GeoLine(start.X, start.Y, end.X, end.Y);
                if (candidate.Length > longest)
                {
                    longest = candidate.Length;
                    leader = candidate;
                }
            }

            return longest > 0.0;
        }

        /// <summary>
        /// Tạo một hình chữ nhật OBB xoay dọc song song theo hướng của đoạn thẳng dẫn,
        /// có điểm tâm đặt tại trung điểm của đoạn dẫn đó.
        /// </summary>
        private static GeoRectangle MakeBox(GeoLine leader, double width, double height)
        {
            GeoPoint centre = leader.MidPoint;

            if (!leader.Direction.TryGetNormal(out GeoVector along))
            {
                along = GeoVector.XAxis;
            }

            double angleRad = Math.Atan2(along.Y, along.X);
            return new GeoRectangle(centre, width, height, angleRad);
        }

        private static void DrawResult(
            Database database, Transaction transaction, List<Arrange> arranges, List<GeoVector> moves, string algorithmName, long elapsedMilliseconds)
        {
            ObjectId boxFromLayerId = EnsureLayer(database, transaction, BoxFromLayer);
            ObjectId boxToLayerId = EnsureLayer(database, transaction, BoxToLayer);
            ObjectId lineMoveLayerId = EnsureLayer(database, transaction, LineMoveLayer);
            ObjectId connectionLayerId = EnsureLayer(database, transaction, "connection");

            var blockTable = (BlockTable)transaction.GetObject(database.BlockTableId, OpenMode.ForRead);
            var space = (BlockTableRecord)transaction.GetObject(
                blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

            var createdEntities = new List<Entity>();
            double maxConnectionLength = 0.0;
            double sumConnectionLength = 0.0;

            for (int i = 0; i < arranges.Count; i++)
            {
                Arrange arrange = arranges[i];

                // Vẽ hộp giả định ở vị trí ban đầu (xám)
                Polyline original = ToAcadPolyline(arrange.GeoRectangle);
                original.LayerId = boxFromLayerId;
                original.ColorIndex = OriginalColour;

                space.AppendEntity(original);
                transaction.AddNewlyCreatedDBObject(original, true);
                createdEntities.Add(original);

                // Dịch chuyển hộp nhãn theo GeoVector kết quả sắp xếp
                GeoRectangle movedRect = new GeoRectangle(
                    arrange.GeoRectangle.Center + moves[i],
                    arrange.GeoRectangle.Width,
                    arrange.GeoRectangle.Height,
                    arrange.GeoRectangle.AngleRad);

                // Vẽ hộp sau khi sắp xếp (xanh lá nếu thành công, đỏ nếu bị chồng lấn/thất bại)
                Polyline moved = ToAcadPolyline(movedRect);
                moved.LayerId = boxToLayerId;
                moved.ColorIndex = arrange.Placed ? PlacedColour : FallbackColour;

                space.AppendEntity(moved);
                transaction.AddNewlyCreatedDBObject(moved, true);
                createdEntities.Add(moved);

                // Vẽ đường nối từ trung điểm đối tượng dẫn đến tâm mới của nhãn
                var leader = new Line(
                    new Point3d(arrange.GeoLine.MidPoint.X, arrange.GeoLine.MidPoint.Y, 0.0),
                    new Point3d(movedRect.Center.X, movedRect.Center.Y, 0.0))
                {
                    LayerId = lineMoveLayerId,
                    ColorIndex = LeaderColour
                };

                space.AppendEntity(leader);
                transaction.AddNewlyCreatedDBObject(leader, true);
                createdEntities.Add(leader);

                // Vẽ đường connection từ BoxTo (tâm nhãn mới) tới điểm gần nhất trên GeoLine
                GeoPoint closestOnLine = arrange.GeoLine.GetClosestPointTo(movedRect.Center);
                var connection = new Line(
                    new Point3d(movedRect.Center.X, movedRect.Center.Y, 0.0),
                    new Point3d(closestOnLine.X, closestOnLine.Y, 0.0))
                {
                    LayerId = connectionLayerId,
                    ColorIndex = 4 // Cyan
                };

                space.AppendEntity(connection);
                transaction.AddNewlyCreatedDBObject(connection, true);
                createdEntities.Add(connection);

                double connLen = movedRect.Center.DistanceTo(closestOnLine);
                sumConnectionLength += connLen;
                if (connLen > maxConnectionLength)
                {
                    maxConnectionLength = connLen;
                }
            }

            if (createdEntities.Count > 0)
            {
                Extents3d totalExtents = new Extents3d();
                bool first = true;

                foreach (Entity ent in createdEntities)
                {
                    try
                    {
                        Extents3d ext = ent.GeometricExtents;
                        if (first)
                        {
                            totalExtents = ext;
                            first = false;
                        }
                        else
                        {
                            totalExtents.AddExtents(ext);
                        }
                    }
                    catch
                    {
                        // Bỏ qua nếu đối tượng không lấy được Extents
                    }
                }

                if (!first)
                {
                    double minX = totalExtents.MinPoint.X;
                    double maxX = totalExtents.MaxPoint.X;
                    double minY = totalExtents.MinPoint.Y;

                    double centerX = (minX + maxX) * 0.5;
                    double textY = minY - 2000.0;

                    int placedCount = 0;
                    for (int i = 0; i < arranges.Count; i++)
                    {
                        if (arranges[i].Placed) placedCount++;
                    }

                    double avgConnectionLength = arranges.Count > 0 ? sumConnectionLength / arranges.Count : 0.0;

                    var dbText = new DBText
                    {
                        TextString = $"{algorithmName} ({placedCount}/{arranges.Count} placed, {elapsedMilliseconds} ms) - Max Conn: {maxConnectionLength:F1}, Avg Conn: {avgConnectionLength:F1}",
                        Height = 800.0,
                        LayerId = boxToLayerId,
                        ColorIndex = PlacedColour
                    };
                    dbText.Justify = AttachmentPoint.BaseCenter;
                    dbText.AlignmentPoint = new Point3d(centerX, textY, 0.0);

                    space.AppendEntity(dbText);
                    transaction.AddNewlyCreatedDBObject(dbText, true);
                }
            }
        }

        /// <summary>
        /// Chuyển đối tượng hình chữ nhật xoay GeoRectangle thành lwpolyline AutoCAD khép kín 4 đỉnh.
        /// </summary>
        private static Polyline ToAcadPolyline(GeoRectangle rect)
        {
            var result = new Polyline();
            var vertices = rect.GetVertices(); // Trả về mảng 4 đỉnh: LowerLeft, LowerRight, UpperRight, UpperLeft

            for (int i = 0; i < vertices.Length; i++)
            {
                result.AddVertexAt(i, new Point2d(vertices[i].X, vertices[i].Y), 0.0, 0.0, 0.0);
            }

            result.Closed = true;
            return result;
        }

        /// <summary>
        /// Đảm bảo layer tồn tại trong bản vẽ, nếu chưa có sẽ tự động tạo mới.
        /// </summary>
        private static ObjectId EnsureLayer(Database database, Transaction transaction, string name)
        {
            var table = (LayerTable)transaction.GetObject(database.LayerTableId, OpenMode.ForRead);
            if (table.Has(name))
            {
                return table[name];
            }

            table.UpgradeOpen();
            var record = new LayerTableRecord { Name = name };
            ObjectId id = table.Add(record);
            transaction.AddNewlyCreatedDBObject(record, true);

            return id;
        }

        /// <summary>
        /// Xuất báo cáo thống kê kết quả sắp xếp ra màn hình Command GeoLine của AutoCAD.
        /// </summary>
        private static void Report(Editor editor, List<Arrange> arranges, int skipped, long milliseconds, string algorithmName)
        {
            int placed = 0;
            foreach (Arrange arrange in arranges)
            {
                if (arrange.Placed)
                {
                    placed++;
                }
            }

            editor.WriteMessage(string.Format(
                CultureInfo.CurrentCulture,
                "\nSắp xếp {0} nhãn bằng thuật toán [{1}] trong {2} ms." +
                "\n  Đặt được       : {3}" +
                "\n  Phải lùi (đỏ)  : {4}" +
                "\n  Bỏ qua         : {5}" +
                "\nLớp {6}: hộp {7}x{8} ở vị trí giả định ban đầu (xám)." +
                "\nLớp {9}: hộp sau khi sắp xếp (xanh lá/đỏ)." +
                "\nLớp {10}: đường nối chỉ sự di chuyển.",
                arranges.Count, algorithmName, milliseconds, placed, arranges.Count - placed, skipped,
                BoxFromLayer, BoxWidth, BoxHeight, BoxToLayer, LineMoveLayer));
        }
    }
}

