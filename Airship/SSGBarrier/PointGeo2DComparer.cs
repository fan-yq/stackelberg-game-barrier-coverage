using Aircraft.Core.ConvexHull;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Spatial.Euclidean;

namespace Airship.Deployment.SSGBarrier;

public enum PointGeo2DSortMode {
    Lat, Lon
}
/// <summary>
/// 单点比较器：假设 PointGeo2D 包含 X, Y 坐标
/// 如果 PointGeo2D 已实现 IComparable，可直接使用 Comparer<PointGeo2D>.Default
/// </summary>
class PointGeo2DComparerLat : IComparer<PointGeo2D> {
    public static PointGeo2DComparerLat Instance = new();
    public int Compare(PointGeo2D x, PointGeo2D y) {
        // 先比 X，再比 Y
        int cmpX = x.Lat.CompareTo(y.Lat);
        if (cmpX != 0) return cmpX;

        return x.Lon.CompareTo(y.Lon);
    }
}
class PointGeo2DComparerLon : IComparer<PointGeo2D> {
    public static PointGeo2DComparerLon Instance = new();
    public int Compare(PointGeo2D x, PointGeo2D y) {
        // 先比 lon，再比 lat
        int cmpX = x.Lon.CompareTo(y.Lon);
        if (cmpX != 0) return cmpX;

        return x.Lat.CompareTo(y.Lat);
    }
}
class Point2DComparerX : IComparer<Point2D> {
    public static Point2DComparerX Instance = new();
    public int Compare(Point2D x, Point2D y) {
        // 先比 X，再比 Y
        int cmpX = x.X.CompareTo(y.X);
        if (cmpX != 0) return cmpX;

        return x.Y.CompareTo(y.Y);
    }
}
class Point2DComparerY : IComparer<Point2D> {
    public static Point2DComparerY Instance = new();
    public int Compare(Point2D x, Point2D y) {
        // 先比 lon，再比 lat
        int cmpX = x.X.CompareTo(y.X);
        if (cmpX != 0) return cmpX;

        return x.Y.CompareTo(y.Y);
    }
}

class VectorComparer(int N, IComparer<Point2D> comparer) : IComparer<Vector<double>> {
    public int Compare(Vector<double>? x, Vector<double>? y) {
        if (x is not null && y is not null) {
            for (int i = 0; i < N; ++i) {
                Point2D p1 = new(x[2 * i], x[2 * i + 1]), p2 = new(y[2 * i], y[2 * i + 1]);
                var res = comparer.Compare(p1, p2);
                if (res != 0) { return res; }
            }
        }
        return 0;
    }
}

class Point2DComparerLine(LineSegment2D line) : IComparer<Point2D> {
    public int Compare(Point2D x, Point2D y) {
        double tx = line.GetParameterT(x);
        double ty = line.GetParameterT(y);
        return tx.CompareTo(ty);
    }
}

static class PointGeo2DComparerFactory {
    public static IComparer<PointGeo2D> Create(PointGeo2DSortMode mode) {
        IComparer<PointGeo2D> comparer = mode switch {
            PointGeo2DSortMode.Lat => PointGeo2DComparerLat.Instance,
            _ => PointGeo2DComparerLon.Instance
        };
        return comparer;
    }
    public static IComparer<Point2D> Create2(PointGeo2DSortMode mode) {
        IComparer<Point2D> comparer = mode switch {
            PointGeo2DSortMode.Lat => Point2DComparerX.Instance,
            _ => Point2DComparerY.Instance
        };
        return comparer;
    }
}