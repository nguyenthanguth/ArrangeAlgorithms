using ArrangeAlgorithms.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using Tekla.Structures.Datatype;
using Tekla.Structures.Drawing;
using Tekla.Structures.Geometry3d;

namespace ArrangeAlgorithms.TeklaTest
{
    public static class ConvertExtension
    {
        public static GeoPoint ToGeoPoint(this Point point)
        {
            return new GeoPoint(point.X, point.Y);
        }

        public static List<GeoPoint> ToGeoPoints(this List<Point> points)
        {
            return points.Select(ToGeoPoint).ToList();
        }

        public static GeoVector ToGeoVector(this Vector vector)
        {
            return new GeoVector(vector.X, vector.Y);
        }

        public static List<GeoVector> ToGeoVectors(this List<Vector> vectors)
        {
            return vectors.Select(ToGeoVector).ToList();
        }

        public static Point ToTeklaPoint(this GeoPoint point)
        {
            return new Point(point.X, point.Y);
        }

        public static List<Point> ToTeklaPoints(this List<GeoPoint> points)
        {
            return points.Select(ToTeklaPoint).ToList();
        }

        public static Vector ToTeklaVector(this GeoVector vector)
        {
            return new Vector(vector.X, vector.Y, 0.0);
        }

        public static List<Vector> ToTeklaVectors(this List<GeoVector> vectors)
        {
            return vectors.Select(ToTeklaVector).ToList();
        }

        public static GeoLine ToGeoLine(this LineSegment lineSegment)
        {
            return new GeoLine(lineSegment.StartPoint.ToGeoPoint(), lineSegment.EndPoint.ToGeoPoint());
        }

        public static GeoRectangle ToGeoRectangle(this AABB aabb)
        {
            return new GeoRectangle(
                aabb.GetCenterPoint().ToGeoPoint(),
                aabb.MaxPoint.X - aabb.MinPoint.X,
                aabb.MaxPoint.Y - aabb.MinPoint.Y,
                0.0);
        }

        public static GeoRectangle ToGeoRectangle(this RectangleBoundingBox boundingBox)
        {
            return new GeoRectangle(
                boundingBox.GetCenterPoint().ToGeoPoint(),
                boundingBox.Width,
                boundingBox.Height,
                Angle.FromDegrees(boundingBox.AngleToAxis).Radians);
        }

        public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey> comparer = null)
        {
            comparer = comparer ?? Comparer<TKey>.Default;
            using IEnumerator<TSource> enumerator = source.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                throw new InvalidOperationException("Empty sequence");
            }

            TSource val = enumerator.Current;
            TKey y = selector(val);
            while (enumerator.MoveNext())
            {
                TSource current = enumerator.Current;
                TKey val2 = selector(current);
                if (comparer.Compare(val2, y) > 0)
                {
                    val = current;
                    y = val2;
                }
            }

            return val;
        }

        public static List<LineSegment> ToLineSegments(this List<Point> points)
        {
            List<LineSegment> segments = new List<LineSegment>();

            for (int i = 0; i < points.Count - 1; i++)
            {
                segments.Add(new LineSegment(points[i], points[i + 1]));
            }

            return segments;
        }

        public static LineSegment GetLongestLength(this List<LineSegment> segments)
        {
            return segments.MaxBy(mb => mb.Length());
        }
    }
}
