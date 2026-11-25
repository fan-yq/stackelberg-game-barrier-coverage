using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using MathNet.Spatial.Euclidean;

namespace Aircraft.Core.ConvexHull;
public enum PolygonRelation {
    Overlap,      // 多边形部分重叠（相交的一种情况）
    Encloses1,     // 一个多项形完全包围另一个多边形(1)
    Encloses2,     // 一个多项形完全包围另一个多边形(2)
    Separate      // 多边形彼此独立（无交集）
}
// public enum PolygonRelation
// {
//     Intersects,   // 一个多项形与另一个多边形相交
//     Contains,     // 一个多项形完全包含另一个多边形
//     Disjoint      // 多边形之间没有公共部分（等同于“不包含”）
// }

// 用于路径规划和几何算法
public sealed record ConvexHullD : ISamplePoint2D {
    public List<Point2D> Points { get; }
    public double MaxX { get; }
    public double MinX { get; }
    public double MaxY { get; }
    public double MinY { get; }

    public ConvexHullD(List<Point2D> points) {
        Points = points;
        if (points == null || points.Count == 0) {
            throw new ArgumentException("Points list cannot be null or empty", nameof(points));
        }

        MaxX = MinX = points[0].X;
        MaxY = MinY = points[0].Y;

        foreach (var point in points) {
            if (point.X > MaxX) MaxX = point.X;
            if (point.X < MinX) MinX = point.X;
            if (point.Y > MaxY) MaxY = point.Y;
            if (point.Y < MinY) MinY = point.Y;
        }

        if (Area() < 0) {
            throw new ArgumentException("Points not counterclockwise, or not convex hull");
        }
    }

    // Calculates the area of the convex hull using the shoelace formula
    public double Area() {
        var points = Points;
        if (points.Count < 3) return 0;

        double area = 0;
        for (int i = 0; i < points.Count; i++) {
            var current = points[i];
            var next = points[(i + 1) % points.Count];

            // Using shoelace formula: sum of (x1*y2 - x2*y1) / 2
            area += (current.X * next.Y) - (next.X * current.Y);
        }

        return area / 2.0;
    }

    public IEnumerable<LineSegment2D> Edges() {
        for (int i = 0; i < Points.Count; i++) {
            var current = Points[i];
            var next = Points[(i + 1) % Points.Count];

            yield return new(current, next);
        }
    }

    /// <summary>
    /// Checks if a point lies inside the convex hull using winding number algorithm
    /// 当点在边上时返回false
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public bool IsPointInHull(Point2D point) {
        var points = Points;
        if (points.Count < 3) return false;

        // For each edge of the hull
        for (int i = 0; i < points.Count; i++) {
            var current = points[i];
            var next = points[(i + 1) % points.Count];

            // If point is on the right side of any edge, it's outside
            if (ConvexHullAlgoD.CrossProduct(current, next, point) <= 0) {
                return false;
            }
        }

        return true;
    }
    /// <summary>
    /// Checks if a point lies inside the convex hull using winding number algorithm
    /// 当点在边上时返回true
    /// </summary>
    public bool IsPointInHullClose(Point2D point, double eps = 1e-9) {
        var points = Points;
        if (points.Count < 3) return false;
        double scale = Max(MaxY - MinY, MaxX - MinX);

        // For each edge of the hull
        for (int i = 0; i < points.Count; i++) {
            var current = points[i];
            var next = points[(i + 1) % points.Count];

            // If point is on the right side of any edge, it's outside
            if (ConvexHullAlgoD.CrossProduct(current, next, point) < -eps * scale) {
                return false;
            }
        }

        return true;
    }

    public (double distance, int index) NearestPoint(Point2D point) {
        double dis = double.PositiveInfinity;
        int index = -1;
        for (int i = 0; i < Points.Count; ++i) {
            double temp = Points[i].DistanceToSquared(point);
            if (dis > temp) {
                dis = temp; index = i;
            }
        }
        return (Math.Sqrt(dis), index);
    }

    public sealed record NearestPointLineResult(int Index, LineSegment2D Line, double Distance) {
        public bool IsPoint => Index >= 0;
    }

    /// <returns>if is point the int index > 0, otherwise is linesegment</returns>
    public NearestPointLineResult NearestLineSegment(Point2D point) {
        // 先计算最近的点
        double dis = double.PositiveInfinity;
        int index = -1;
        for (int i = 0; i < Points.Count; ++i) {
            double temp = Points[i].DistanceToSquared(point);
            if (dis > temp) {
                dis = temp; index = i;
            }
        }
        dis = Math.Sqrt(dis);

        // 计算最近点附近的边的距离
        int N = Points.Count;
        LineSegment2D line1 = new(Points[index], Points[(index + 1) % N]);
        LineSegment2D line2 = new(Points[(index - 1 + N) % N], Points[index]);
        double d1 = line1.Distance(point);
        double d2 = line2.Distance(point);

        if (N == 2) {
            if (d1 < dis) { return new(-1, line1, d1); } else { return new(index, new(), dis); }
        }

        if (dis <= Min(d1, d2)) { return new(index, new(), dis); }
        if (d1 < d2) { return new(-1, line1, d1); }
        if (d1 > d2) { return new(-1, line2, d2); }
        // 精度不够 d1=d2
        double sign1 = line1.CrossProduct(point), sign2 = line1.CrossProduct(Points[(index - 1 + N) % N]);
        if (sign1 * sign2 > 0) { return new(-1, line2, d2); } else { return new(-1, line1, d1); }
    }

    public double[][] ToArray() {
        var hullsArray = new double[Points.Count][];

        for (int j = 0; j < Points.Count; j++) {
            hullsArray[j] = new double[] { Points[j].X, Points[j].Y };
        }
        return hullsArray;
    }

    /// <summary>
    /// 线和凸包检测，不检测线在凸包内部，凸包视为开集，线在边上不视为相交
    /// </summary>
    public bool IsIntersect(LineSegment2D line) {
        // 首先检查线段的端点是否在边界之外
        var p1 = line.StartPoint;
        var p2 = line.EndPoint;
        bool
        f1 = p1.X < MinX && p2.X < MinX,
        f2 = p1.X > MaxX && p2.X > MaxX,
        f3 = p1.Y < MinY && p2.Y < MinY,
        f4 = p1.Y > MaxY && p2.Y > MaxY;
        if (f1 || f2 || f3 || f4) {
            return false;
        }

        foreach (var edge in Edges()) {
            var t = line.Intersect(edge);
            if (t is not LineIntersectType.None && t is not LineIntersectType.SinglePointOnLine) {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 将点投影到凸包边界上。
    /// 若点在凸包内部（含边界），直接返回原点；
    /// 若点在凸包外部，返回凸包边界上离该点最近的点。
    /// </summary>
    /// <param name="point">待投影的点</param>
    /// <returns>凸包边界上的投影点</returns>
    public Point2D ProjectToHull(Point2D point, bool check = false) {
        // 点在凸包内（含边），无需投影
        if (check && IsPointInHullClose(point)) {
            return point;
        }

        var points = Points;
        int n = points.Count;

        // 退化情况：不足 3 个点，直接在顶点集合里找最近点
        if (n < 3) {
            var nearestIdx = NearestPoint(point).index;
            return points[nearestIdx];
        }

        Point2D best = default;
        double bestDisSq = double.PositiveInfinity;

        for (int i = 0; i < n; i++) {
            var a = points[i];
            var b = points[(i + 1) % n];
            LineSegment2D line = new(a, b);
            var candidate = line.ClosestPointTo(point);
            double disSq = candidate.DistanceToSquared(point);

            if (disSq < bestDisSq) {
                bestDisSq = disSq;
                best = candidate;
            }
        }
        if (!IsPointInHullClose(best)) {
            Debug.Assert(IsPointInHullClose(best));
        }


        return best;
    }

    public Point2D Sample(Random random) {
        double width = MaxX - MinX, height = MaxY - MinY;
        Point2D p;
        do {
            p = new(random.NextDouble() * width + MinX, random.NextDouble() * height + MinY);
        } while (!IsPointInHullClose(p));
        return p;
    }
}


public static class ConvexHullAlgoD {
    internal static double DistanceToSquared(this Point2D Self, Point2D a) {
        var delta = Self - a;
        return delta.X * delta.X + delta.Y * delta.Y;
    }

    // Implements Andrew Monotone Chain algorithm to find convex hull
    public static ConvexHullD GetHull(List<Point2D> points) {
        if (points.Count <= 2)
            return new ConvexHullD(points);

        if (points.Count == 3) {
            if (CrossProduct(points[0], points[1], points[2]) < 0)
                return new ConvexHullD(Enumerable.Reverse(points).ToList());
            else
                return new ConvexHullD(points);
        }

        // Sort points lexicographically (by X, then by Y)
        var sorted = points.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();

        // Build lower hull
        var lower = new List<Point2D>();
        foreach (var p in sorted) {
            while (lower.Count >= 2 &&
                   CrossProduct(lower[lower.Count - 2], lower[lower.Count - 1], p) <= 0) {
                lower.RemoveAt(lower.Count - 1);
            }
            lower.Add(p);
        }

        // Build upper hull
        var upper = new List<Point2D>();
        for (int i = sorted.Count - 1; i >= 0; i--) {
            var p = sorted[i];
            while (upper.Count >= 2 &&
                   CrossProduct(upper[upper.Count - 2], upper[upper.Count - 1], p) <= 0) {
                upper.RemoveAt(upper.Count - 1);
            }
            upper.Add(p);
        }

        // Concatenate lower and upper hull (remove duplicate endpoints)
        lower.RemoveAt(lower.Count - 1);
        upper.RemoveAt(upper.Count - 1);
        var hull = lower.Concat(upper).ToList();

        return new ConvexHullD(hull);
    }

    // Helper method to compute cross product
    public static double CrossProduct(Point2D p1, Point2D p2, Point2D p3) {
        return (p2.X - p1.X) * (p3.Y - p1.Y) -
               (p2.Y - p1.Y) * (p3.X - p1.X);
    }


    // OPTIMIZE
    public static PolygonRelation CheckRelation(ConvexHullD hull1, ConvexHullD hull2) {
        var relation = CheckRelation_(hull1, hull2, false);
        if (relation is not PolygonRelation.Separate) {
            return relation;
        }

        return CheckRelation_(hull2, hull1, true);
    }

    static PolygonRelation CheckRelation_(ConvexHullD hull1, ConvexHullD hull2, bool swapFlag) {
        bool IsIntersect = true;
        bool IsDisjoint = true;
        foreach (var point in hull2.Points) {
            bool isInternal = hull1.IsPointInHull(point);
            IsIntersect &= isInternal;
            IsDisjoint &= !isInternal;
        }

        return (IsIntersect, IsDisjoint) switch {
            (false, false) => PolygonRelation.Overlap,
            (false, true) => PolygonRelation.Separate,
            (true, false) => swapFlag ? PolygonRelation.Encloses1 : PolygonRelation.Encloses2,
            _ => throw new UnreachableException(),
        };
    }

    public static ConvexHullD Merge(ConvexHullD hull1, ConvexHullD hull2) => GetHull([.. hull1.Points, .. hull2.Points]);
}


public class ConvexHullDConverter : JsonConverter<ConvexHullD> {
    static readonly Point2DConverter pointConverter = new();
    public override ConvexHullD Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType != JsonTokenType.StartArray) {
            throw new JsonException("Expected StartArray for ConvexHullD");
        }

        // 利用已存在的 Point2DConverter 来解析内部的每个点
        var points = new List<Point2D>();

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray) {
            var point = pointConverter.Read(ref reader, typeof(Point2D), options);
            points.Add(point);
        }

        // 通过带参构造函数实例化对象
        return new ConvexHullD(points);
    }

    public override void Write(Utf8JsonWriter writer, ConvexHullD value, JsonSerializerOptions options) {
        writer.WriteStartArray(); // 最外层数组开始
        foreach (var point in value.Points) {
            pointConverter.Write(writer, point, options);
        }

        writer.WriteEndArray(); // 最外层数组结束
    }
}