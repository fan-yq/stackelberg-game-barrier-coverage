using Aircraft.Core.ConvexHull;
using MathNet.Spatial.Euclidean;

namespace Airship.Deployment.SSGBarrier;



// 用于存储分割结果
public record LineSegmentWithCircles(LineSegment2D Segment, int[] CircleIndices);

public class LineSegCircleGeometry(double Epsilon = 1e-10) {
    // 定义一个极小值，用于处理浮点数精度问题

    /// <summary>
    /// 计算线段和多个圆的相交，将相交的部分分为几段，每一段给出相交的圆的index。
    /// </summary>
    /// <param name="line">输入线段</param>
    /// <param name="circleCenters">圆心数组</param>
    /// <param name="radius">半径数组</param>
    /// <returns>分割后的线段及其所在的圆索引数组</returns>
    public LineSegmentWithCircles[] SplitLine(LineSegment2D line, Point2D[] circleCenters, double[] radius) {
        var result = new List<LineSegmentWithCircles>();

        // 1. 收集所有关键点（线段端点和与圆的交点）
        // 使用 SortedSet 来自动排序和去重，排序依据是点在线段上的投影比例 t
        var keyPoints = new SortedSet<(double t, Point2D point)>(Comparer<(double t, Point2D point)>.Create((a, b) => {
            int cmp = a.t.CompareTo(b.t);
            return cmp != 0 ? cmp : a.point.X.CompareTo(b.point.X); // 如果t相同，按X坐标再排序以保证稳定性
        }));

        // 添加线段的两个端点
        keyPoints.Add((0.0, line.StartPoint));
        keyPoints.Add((1.0, line.EndPoint));

        // 计算线段与每个圆的交点
        for (int i = 0; i < circleCenters.Length; i++) {
            var intersections = GetLineCircleIntersections(line, circleCenters[i], radius[i]);
            foreach (var point in intersections) {
                double t = line.GetParameterT(point);
                // 只添加在线段上的交点
                if (t >= -Epsilon && t <= 1 + Epsilon) {
                    // 将t值钳制到[0, 1]范围内，避免因浮点误差导致的问题
                    t = Math.Max(0, Math.Min(1, t));
                    keyPoints.Add((t, point));
                }
            }
        }

        // 2. 将关键点转换为列表以便遍历
        var sortedPoints = keyPoints.ToList();

        // 3. 遍历相邻的关键点，形成小线段并判断其状态
        for (int i = 0; i < sortedPoints.Count - 1; i++) {
            var p1 = sortedPoints[i].point;
            var p2 = sortedPoints[i + 1].point;

            // 跳过长度为0的线段（重复点）
            if ((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y) < Epsilon) {
                continue;
            }

            var subSegment = new LineSegment2D(p1, p2);
            var activeCircles = new List<int>();

            // 取小线段的中点进行判断
            var midPoint = new Point2D((p1.X + p2.X) / 2.0, (p1.Y + p2.Y) / 2.0);

            for (int j = 0; j < circleCenters.Length; j++) {
                // 如果中点到圆心的距离小于半径，则该小段在圆内
                double distanceSquared = (midPoint.X - circleCenters[j].X) * (midPoint.X - circleCenters[j].X) +
                                         (midPoint.Y - circleCenters[j].Y) * (midPoint.Y - circleCenters[j].Y);
                if (distanceSquared < radius[j] * radius[j] - Epsilon) {
                    activeCircles.Add(j);
                }
            }

            result.Add(new LineSegmentWithCircles(subSegment, activeCircles.ToArray()));
        }

        return result.ToArray();
    }

    /// <summary>
    /// 计算线段与圆的交点
    /// </summary>
    private List<Point2D> GetLineCircleIntersections(LineSegment2D line, Point2D center, double r) {
        var intersections = new List<Point2D>();

        // 将线段表示为参数方程: P(t) = line.StartPoint + t * (line.EndPoint - line.StartPoint), t ∈ [0, 1]
        double x1 = line.StartPoint.X, y1 = line.StartPoint.Y;
        double x2 = line.EndPoint.X, y2 = line.EndPoint.Y;
        double cx = center.X, cy = center.Y;

        double dx = x2 - x1;
        double dy = y2 - y1;

        // 代入圆的方程 (x - cx)^2 + (y - cy)^2 = r^2
        // 得到关于 t 的一元二次方程: at^2 + bt + c = 0
        double a = dx * dx + dy * dy;
        double b = 2 * (dx * (x1 - cx) + dy * (y1 - cy));
        double c = (x1 - cx) * (x1 - cx) + (y1 - cy) * (y1 - cy) - r * r;

        double discriminant = b * b - 4 * a * c;

        if (discriminant < -Epsilon) {
            // 无实数解，不相交
            return intersections;
        }

        // 处理相切或相交的情况
        discriminant = Math.Max(0, discriminant); // 防止因浮点误差导致 discriminant 略小于0
        double sqrtDiscriminant = Math.Sqrt(discriminant);

        double t1 = (-b - sqrtDiscriminant) / (2 * a);
        double t2 = (-b + sqrtDiscriminant) / (2 * a);

        // 检查 t1 是否在线段上
        if (t1 >= -Epsilon && t1 <= 1 + Epsilon) {
            double ix = x1 + t1 * dx;
            double iy = y1 + t1 * dy;
            intersections.Add(new Point2D(ix, iy));
        }

        // 检查 t2 是否在线段上，并且与 t1 不是同一个点（处理相切）
        if (Math.Abs(t2 - t1) > Epsilon && t2 >= -Epsilon && t2 <= 1 + Epsilon) {
            double ix = x1 + t2 * dx;
            double iy = y1 + t2 * dy;
            intersections.Add(new Point2D(ix, iy));
        }

        return intersections;
    }
}