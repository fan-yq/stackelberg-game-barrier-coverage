using System.Diagnostics;
using Aircraft.Core.ConvexHull;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Spatial.Euclidean;
using Microsoft.Extensions.Logging;

namespace Airship.Deployment.Comm;

// 向量编码
// 位置排序
// 凸包搜索
// 半圆采样
public static class DeployOptimShared {


    internal static PointGeo2D[] VectorDecode2D(IList<double> p) {
        int NT = p.Count / 2;
        PointGeo2D[] positions = new PointGeo2D[NT];
        for (int i = 0; i < NT; ++i) {
            positions[i] = new(p[2 * i], p[2 * i + 1]);
        }
        return positions;
    }



    internal static Vector<double> Point2DsEncode(PointGeo2D[] p) {
        int N = p.Length * 2;
        var v = Vector<double>.Build.Dense(N);
        for (int i = 0; i < p.Length; ++i) {
            v[2 * i] = p[i].Lat;
            v[2 * i + 1] = p[i].Lon;
        }
        return v;
    }
    internal static Vector<double> Vector2DsEncode(Vector2D[] p) {
        int N = p.Length * 2;
        var v = Vector<double>.Build.Dense(N);
        for (int i = 0; i < p.Length; ++i) {
            v[2 * i] = p[i].X;
            v[2 * i + 1] = p[i].Y;
        }
        return v;
    }

    internal static Vector2D[] BoundsEncode1(RectfGeo area, int NT) {
        Vector2D latBound = new(area.MinLat, area.MaxLat), lonBound = new(area.MinLon, area.MaxLon);
        Vector2D[] bounds = new Vector2D[NT * 2];
        for (int i = 0; i < NT * 2; ++i) {
            bounds[i] = int.IsOddInteger(i) ? lonBound : latBound;
        }
        return bounds;
    }
    internal static (Vector<double>, Vector<double>) BoundsEncode2(RectfGeo area, int NT) {
        int dimension = NT * 2;
        var lower = Vector<double>.Build.Dense(dimension);
        var upper = Vector<double>.Build.Dense(dimension);

        for (int i = 0; i < NT * 2; ++i) {
            lower[i] = int.IsOddInteger(i) ? area.MinLon : area.MinLat;
            upper[i] = int.IsOddInteger(i) ? area.MaxLon : area.MaxLat;
        }
        return (lower, upper);
    }

    /// <summary>
    /// 将向量编码成极坐标形式用于差分算子
    /// </summary>
    /// <returns></returns>
    internal static Vector<double> PolarEncode(Vector<double> p, PointGeo2D[] current) {
        int NT = current.Length;
        Vector<double> res = Vector<double>.Build.Dense(NT * 2);
        for (int i = 0; i < NT; ++i) {
            // 读取当前点
            double x = p[2 * i], y = p[2 * i + 1];
            double dx = x - current[i].Lat, dy = y - current[i].Lon;
            // 变为极坐标
            double r = Sqrt(dx * dx + dy * dy);
            double theta = Atan2(dy, dx);
            res[2 * i] = r;
            res[2 * i + 1] = theta;
        }
        return res;
    }

    /// <summary>
    /// 将向量编码成极坐标形式用于差分算子
    /// </summary>
    /// <returns></returns>
    internal static Vector<double> PolarEncode(PointGeo2D[] p, PointGeo2D[] current) {
        int NT = current.Length;
        Vector<double> res = Vector<double>.Build.Dense(NT * 2);
        for (int i = 0; i < NT; ++i) {
            // 读取当前点
            double x = p[i].Lat, y = p[i].Lon;
            double dx = x - current[i].Lat, dy = y - current[i].Lon;
            // 变为极坐标
            double r = Sqrt(dx * dx + dy * dy);
            double theta = Atan2(dy, dx);
            res[2 * i] = r;
            res[2 * i + 1] = theta;
        }
        return res;
    }

    internal static PointGeo2D[] PolarDecode(Vector<double> polar_delta, PointGeo2D[] current) {
        int NT = current.Length;
        var res = new PointGeo2D[NT];
        for (int i = 0; i < NT; ++i) {
            double r = polar_delta[2 * i], theta = polar_delta[2 * i + 1];
            double lat = current[i].Lat + r * Cos(theta);
            double lon = current[i].Lon + r * Sin(theta);
            res[i] = new(lat, lon);
        }
        return res;
    }

    // --------------------------相对位置经纬度编码-----------------------------
    internal static Vector<double> RelativeEncode(PointGeo2D[] p, PointGeo2D[] current) {
        int NT = current.Length;
        Vector<double> res = Vector<double>.Build.Dense(NT * 2);
        for (int i = 0; i < NT; ++i) {
            // 读取当前点
            double x = p[i].Lat, y = p[i].Lon;
            double dx = x - current[i].Lat, dy = y - current[i].Lon;
            res[2 * i] = dx;
            res[2 * i + 1] = dy;
        }
        return res;
    }

    internal static PointGeo2D[] RelativeDecode(Vector<double> relative, PointGeo2D[] current) {
        int NT = current.Length;
        var res = new PointGeo2D[NT];
        for (int i = 0; i < NT; ++i) {
            double dx = relative[2 * i], dy = relative[2 * i + 1];
            double lat = current[i].Lat + dx;
            double lon = current[i].Lon + dy;
            res[i] = new(lat, lon);
        }
        return res;
    }

    // ----------------------------------------------------------------------------



    // ------------------------------------------------------------
    internal static bool PointInHulls(ConvexHullF[] convexHulls, PointGeo2D p) => convexHulls.Any(hull => hull.IsPointInHull(new((float)p.Lat, (float)p.Lon)));
    public static bool PointsInHulls(ConvexHullF[] convexHulls, PointGeo2D[] ps) => ps.Any(p => PointInHulls(convexHulls, p));



    internal static bool IsPointInRadius(PointGeo2D p_center, PointGeo2D newPoint, double radiusDegree, double eps = 0) {
        double dlat = p_center.Lat - newPoint.Lat, dlon = p_center.Lon - newPoint.Lon;
        double cosphi = Cos(p_center.Latitude.Radians);
        ArgumentOutOfRangeException.ThrowIfEqual(cosphi, 0);
        if (!double.IsFinite(newPoint.Lat) || !double.IsFinite(newPoint.Lon)) {
            throw new ArgumentOutOfRangeException($"new point {newPoint} is not finite");
        }
        dlat = AngleUtil.NormalizeAngleDegree(dlat);
        dlon = AngleUtil.NormalizeAngleDegree(dlon);
        return dlat * dlat + dlon * dlon * cosphi * cosphi <= radiusDegree * radiusDegree * (1 + eps * eps);
    }










    /// <summary>
    /// 距离和梯度的计算方式,
    /// 如果在hull外面，距离为正
    /// 如果在hull内部，距离为负
    /// </summary>
    /// <param name="q"></param>
    /// <param name="convexHull"></param>
    /// <returns></returns>
    internal static (double, Vector2D) DistanceAndGrad(PointGeo2D q, ConvexHullF convexHull) {
        double d, gradx, grady;

        var nearest = convexHull.NearestLineSegment(q.Vector2);
        var hullPoints = convexHull.Points;
        if (nearest.IsPoint) {
            var from = hullPoints[nearest.Index];
            double dx = q.Lat - from.X, dy = q.Lon - from.Y;
            d = Sqrt(dx * dx + dy * dy);

            if (d < 1e-12) {
                // 点与顶点重合：计算顶点处的外法向量（取相邻两边法向量平均）
                int n = hullPoints.Count;
                var prev = hullPoints[(nearest.Index - 1 + n) % n];
                var next = hullPoints[(nearest.Index + 1) % n];

                // 边1: prev -> from
                var e1 = from - prev;
                // 边2: from -> next
                var e2 = next - from;

                // 外法向量（假设逆时针，左侧为内，右侧为外 → 外法向量 = (dy, -dx) 归一化？）
                // 更稳健：用边的“右手法向量”作为外法
                var n1 = new Vector2D(e1.Y, -e1.X).Normalize(); // 右侧法向量
                var n2 = new Vector2D(e2.Y, -e2.X).Normalize();

                var avgNormal = (n1 + n2).Normalize();
                gradx = avgNormal.X;
                grady = avgNormal.Y;
            } else {
                gradx = dx / d;
                grady = dy / d;
            }
        } else {
            var pi = nearest.Line.From;
            var pj = nearest.Line.To;
            var pj_pi = pj - pi;
            var q_pi = q.Vector2 - pi;
            double dx = pj_pi.X, dy = pj_pi.Y, norm_pj_pi = pj_pi.Length();

            d = nearest.Line.CrossProduct(q.Vector2) / norm_pj_pi;
            gradx = -dy / norm_pj_pi;
            grady = dx / norm_pj_pi;

            bool d_is_nonpos = d <= 0;
            bool inHull = convexHull.IsPointInHull(q.Vector2);
            // in hull  | d < 0 | true true
            // out hull | d > 0 | false false
            // 如果异或为true，换号
            if (d_is_nonpos ^ inHull) {
                d = -d;
                gradx = -gradx;
                grady = -grady;
            }
        }

        return (d, new(gradx, grady));
    }


}


public static class DeployOptimHelper {


    /// <summary>
    /// 情况1：椭圆完全在一个凸包内部，通过计算中心到convexHull的距离判断干涉，
    /// 情况2：出现两个凸包包围椭圆的情况
    /// 设置radius为0时可以判断点与凸包干涉
    /// </summary>
    /// <returns>返回干涉的convex hull索引和距离</returns>
    public static List<(int index, double d)> IntersectedHull(ConvexHullF[] convexHulls, PointGeo2D p_center, double radiusDegree) {
        List<(int index, double d)> intersectedHullsIndex = [];
        List<int> mightOverlapHullIndex = []; // 可能会出现两个凸包包围椭圆的情况，计算附近的凸包
        for (int i = 0; i < convexHulls.Length; ++i) {
            var res = convexHulls[i].NearestLineSegment(p_center.Vector2);
            bool pointInHull = convexHulls[i].IsPointInHull(p_center.Vector2);
            if (pointInHull && res.Distance > radiusDegree) {
                intersectedHullsIndex.Add((i, res.Distance));
            }
            if (pointInHull) { mightOverlapHullIndex.Add(i); }
        }

        if (intersectedHullsIndex.Count == 0 && radiusDegree == 0) {
            Debug.Assert(!DeployOptimShared.PointInHulls(convexHulls, p_center));
        }

        bool f = intersectedHullsIndex.Count == 0 && mightOverlapHullIndex.Count > 1 && radiusDegree > 0;
        if (!f) {
            return intersectedHullsIndex;
        }
        // 不是情况1，可能是情况2,采样椭圆的边界，如果都位于mightOverlapHullIndex内部，则返回IntersectedHull
        bool boundsInHull = true;
        double cosphi = Cos(p_center.Latitude.Radians);
        HashSet<int> overlapHullIndices = [];
        for (int i = 0; i < 360 && boundsInHull; ++i) {
            double x = p_center.Lat + radiusDegree * Cos(i / 360.0 * 2 * PI);
            double y = p_center.Lon + radiusDegree * Sin(i / 360.0 * 2 * PI) / cosphi;
            var hulls = Enumerable.Range(0, mightOverlapHullIndex.Count).Where(i => convexHulls[mightOverlapHullIndex[i]].IsPointInHull(new((float)x, (float)y))).ToArray();
            bool boundInHull = hulls.Length > 0;
            boundsInHull &= boundInHull;
            if (boundInHull) {
                foreach (var hull in hulls) {
                    overlapHullIndices.Add(hull);
                }
            }
        }

        if (boundsInHull == false) {
            // 有边界点在外部，说明没有完全包围
            return [];
        }
        return overlapHullIndices.Select(i => (i, (double)convexHulls[i].NearestLineSegment(p_center.Vector2).Distance)).ToList();

    }

}