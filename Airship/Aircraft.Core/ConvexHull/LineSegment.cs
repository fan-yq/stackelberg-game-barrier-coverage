using System.Numerics;

namespace Aircraft.Core.ConvexHull;

public enum LineIntersectType {
    None,       // 线段之间没有交点
    SinglePoint,// 线段相交于一个点，开集
    SinglePointOnLine, // 线段的端点在另一条线上
    Overlap,    // 线段部分或完全重合（有无限多的交点）
    Collinear   // 线段共线但不重叠（可以认为是特殊情况下的“无交点”或者“部分重合”）
}

public record struct LineSegment(Vector2 From, Vector2 To) {
    readonly public LineIntersectType Intersect(LineSegment AB) {
        float A = CrossProduct(AB.From), B = CrossProduct(AB.To);
        if (A * B > 0) { return LineIntersectType.None; } else if (A == 0 && B == 0) {
            return this == AB ? LineIntersectType.Overlap : LineIntersectType.Collinear;
        } else if (A == 0 || B == 0) {
            return LineIntersectType.SinglePointOnLine;
        }
        // AB=-1
        A = AB.CrossProduct(From); B = AB.CrossProduct(To);
        return (A * B) switch {
            0 => LineIntersectType.SinglePointOnLine,
            > 0 => LineIntersectType.None,
            _ => LineIntersectType.SinglePoint,
        };
    }

    /// <summary>
    /// Vector2 point是点，验证左右（面积正负）
    /// </summary>
    /// <param name="point">point</param>
    /// <returns></returns>
    public readonly float CrossProduct(Vector2 point) {
        return ConvexHullAlgoF.CrossProduct(From, To, point);
    }

    public readonly float Distance(Vector2 point) {
        Vector2 line = To - From;
        Vector2 pointToStart = point - From;

        float length = line.LengthSquared();
        if (length == 0) return pointToStart.Length(); // Line segment is actually a point

        // Calculate projection of point onto line
        float t = Vector2.Dot(pointToStart, line) / length;

        // If projection is beyond segment endpoints, return distance to nearest endpoint
        if (t <= 0) return pointToStart.Length();
        if (t >= 1) return (point - To).Length();

        // Return perpendicular distance to line
        return float.Abs(CrossProduct(point)) / float.Sqrt(length);
    }
}