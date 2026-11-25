using System.Diagnostics;
using System.Numerics;
namespace Aircraft.Core.ConvexHull;



// public enum PolygonRelation
// {
//     Intersects,   // 一个多项形与另一个多边形相交
//     Contains,     // 一个多项形完全包含另一个多边形
//     Disjoint      // 多边形之间没有公共部分（等同于“不包含”）
// }

// 用于路径规划和几何算法
public sealed record ConvexHullF {
    public List<Vector2> Points { get; init; }
    public float MaxX { get; init; }
    public float MinX { get; init; }
    public float MaxY { get; init; }
    public float MinY { get; init; }

    public ConvexHullF(List<Vector2> points) {
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

    public IEnumerable<LineSegment> Edges() {
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
    public bool IsPointInHull(Vector2 point) {
        var points = Points;
        if (points.Count < 3) return false;

        // For each edge of the hull
        for (int i = 0; i < points.Count; i++) {
            var current = points[i];
            var next = points[(i + 1) % points.Count];

            // If point is on the right side of any edge, it's outside
            if (ConvexHullAlgoF.CrossProduct(current, next, point) <= 0) {
                return false;
            }
        }

        return true;
    }
    /// <summary>
    /// Checks if a point lies inside the convex hull using winding number algorithm
    /// 当点在边上时返回true
    /// </summary>
    public bool IsPointInHullClose(Vector2 point) {
        var points = Points;
        if (points.Count < 3) return false;

        // For each edge of the hull
        for (int i = 0; i < points.Count; i++) {
            var current = points[i];
            var next = points[(i + 1) % points.Count];

            // If point is on the right side of any edge, it's outside
            if (ConvexHullAlgoF.CrossProduct(current, next, point) < 0) {
                return false;
            }
        }

        return true;
    }

    public (float distance, int index) NearestPoint(Vector2 point) {
        float dis = float.PositiveInfinity;
        int index = -1;
        for (int i = 0; i < Points.Count; ++i) {
            float temp = (Points[i] - point).LengthSquared();
            if (dis > temp) {
                dis = temp; index = i;
            }
        }
        return (float.Sqrt(dis), index);
    }

    public sealed record NearestPointLineResult(int Index, LineSegment Line, float Distance) {
        public bool IsPoint => Index >= 0;
    }

    /// <returns>if is point the int index > 0, otherwise is linesegment</returns>
    public NearestPointLineResult NearestLineSegment(Vector2 point) {
        // 先计算最近的点
        float dis = float.PositiveInfinity;
        int index = -1;
        for (int i = 0; i < Points.Count; ++i) {
            float temp = (Points[i] - point).LengthSquared();
            if (dis > temp) {
                dis = temp; index = i;
            }
        }
        dis = float.Sqrt(dis);

        // 计算最近点附近的边的距离
        int N = Points.Count;
        LineSegment line1 = new(Points[index], Points[(index + 1) % N]);
        LineSegment line2 = new(Points[(index - 1 + N) % N], Points[index]);
        float d1 = line1.Distance(point);
        float d2 = line2.Distance(point);

        if (N == 2) {
            if (d1 < dis) { return new(-1, line1, d1); } else { return new(index, new(), dis); }
        }

        if (dis <= Min(d1, d2)) { return new(index, new(), dis); }
        if (d1 < d2) { return new(-1, line1, d1); }
        if (d1 > d2) { return new(-1, line2, d2); }
        // 精度不够 d1=d2
        float sign1 = line1.CrossProduct(point), sign2 = line1.CrossProduct(Points[(index - 1 + N) % N]);
        if (sign1 * sign2 > 0) { return new(-1, line2, d2); } else { return new(-1, line1, d1); }
    }

    public float[][] ToArray() {
        var hullsArray = new float[Points.Count][];

        for (int j = 0; j < Points.Count; j++) {
            hullsArray[j] = [Points[j].X, Points[j].Y];
        }
        return hullsArray;
    }

    /// <summary>
    /// 线和凸包检测，不检测线在凸包内部，凸包视为开集，线在边上不视为相交
    /// </summary>
    public bool IsIntersect(LineSegment line) {
        // 首先检查线段的端点是否在边界之外
        var (p1, p2) = line;
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
}


public static class ConvexHullAlgoF {

    // Implements Andrew Monotone Chain algorithm to find convex hull
    public static ConvexHullF GetHull(List<Vector2> points) {
        if (points.Count <= 2)
            return new ConvexHullF(points);

        if (points.Count == 3) {
            if (CrossProduct(points[0], points[1], points[2]) < 0)
                return new ConvexHullF(Enumerable.Reverse(points).ToList());
            else
                return new ConvexHullF(points);
        }

        // Sort points lexicographically (by X, then by Y)
        var sorted = points.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();

        // Build lower hull
        var lower = new List<Vector2>();
        foreach (var p in sorted) {
            while (lower.Count >= 2 &&
                   CrossProduct(lower[lower.Count - 2], lower[lower.Count - 1], p) <= 0) {
                lower.RemoveAt(lower.Count - 1);
            }
            lower.Add(p);
        }

        // Build upper hull
        var upper = new List<Vector2>();
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

        return new ConvexHullF(hull);
    }

    // Helper method to compute cross product
    public static float CrossProduct(Vector2 p1, Vector2 p2, Vector2 p3) {
        return (p2.X - p1.X) * (p3.Y - p1.Y) -
               (p2.Y - p1.Y) * (p3.X - p1.X);
    }


    // OPTIMIZE
    public static PolygonRelation CheckRelation(ConvexHullF hull1, ConvexHullF hull2) {
        var relation = CheckRelation_(hull1, hull2, false);
        if (relation is not PolygonRelation.Separate) {
            return relation;
        }

        return CheckRelation_(hull2, hull1, true);
    }

    static PolygonRelation CheckRelation_(ConvexHullF hull1, ConvexHullF hull2, bool swapFlag) {
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

    public static ConvexHullF Merge(ConvexHullF hull1, ConvexHullF hull2) => GetHull([.. hull1.Points, .. hull2.Points]);
}