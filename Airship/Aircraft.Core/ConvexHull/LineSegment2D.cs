using Aircraft.Core.ConvexHull;
using MathNet.Spatial.Euclidean;

namespace Aircraft.Core.ConvexHull;

public static class LineSegment2DExt {
    public static LineIntersectType Intersect(this LineSegment2D Self, LineSegment2D AB) {
        double A = CrossProduct(Self, AB.StartPoint), B = CrossProduct(Self, AB.EndPoint);
        if (A * B > 0) { return LineIntersectType.None; } else if (A == 0 && B == 0) {
            return Self == AB ? LineIntersectType.Overlap : LineIntersectType.Collinear;
        } else if (A == 0 || B == 0) {
            return LineIntersectType.SinglePointOnLine;
        }
        // AB=-1
        A = AB.CrossProduct(Self.StartPoint); B = AB.CrossProduct(Self.EndPoint);
        return (A * B) switch {
            0 => LineIntersectType.SinglePointOnLine,
            > 0 => LineIntersectType.None,
            _ => LineIntersectType.SinglePoint,
        };
    }

    /// <summary>
    /// Point2D point是点，验证左右（面积正负）
    /// </summary>
    /// <param name="point">point</param>
    /// <returns></returns>
    public static double CrossProduct(this LineSegment2D Self, Point2D point) {
        return CrossProduct(Self.StartPoint, Self.EndPoint, point);
    }

    public static double Distance(this LineSegment2D Self, Point2D point) {
        Vector2D line = Self.EndPoint - Self.StartPoint;
        Vector2D pointToStart = point - Self.StartPoint;
        var length = line.Length;
        var length2 = length * length;
        if (length2 == 0) return pointToStart.Length; // Line segment is actually a point

        // Calculate projection of point onto line
        var t = pointToStart.DotProduct(line) / length2;

        // If projection is beyond segment endpoints, return distance to nearest endpoint
        if (t <= 0) return pointToStart.Length;
        if (t >= 1) return (point - Self.EndPoint).Length;

        // Return perpendicular distance to line
        return Abs(CrossProduct(Self, point)) / length;
    }

    public static double CrossProduct(Point2D p1, Point2D p2, Point2D p3) {
        return (p2.X - p1.X) * (p3.Y - p1.Y) -
               (p2.Y - p1.Y) * (p3.X - p1.X);
    }

    // 新增：计算点到线段起点的投影比例 t (0 <= t <= 1)
    public static double GetParameterT(this LineSegment2D Self, Point2D point) {
        Vector2D line = Self.EndPoint - Self.StartPoint;
        double lengthSquared = line.Length * line.Length;
        if (lengthSquared == 0) return 0; // 线段退化为一个点

        Vector2D pointToStart = point - Self.StartPoint;
        return pointToStart.DotProduct(line) / lengthSquared;
    }

    public static Point2D GetPointT(this LineSegment2D Self, double t) {
        Vector2D line = Self.EndPoint - Self.StartPoint;
        return Self.StartPoint + t * line;
    }

    public static double SphereAngle(this LineSegment2D Self) {
        PointGeo2D p1 = new(Self.StartPoint), p2 = new(Self.EndPoint);
        return PointGeo2D.SphereAngle(p1, p2);
    }
}

