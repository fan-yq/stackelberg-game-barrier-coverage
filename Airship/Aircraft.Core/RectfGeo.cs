using System.Diagnostics;
using Aircraft.Core.ConvexHull;
using MathNet.Numerics.Random;
using MathNet.Spatial.Euclidean;

namespace Aircraft.Core;


public readonly struct RectfGeo(double x, double y, double width, double height)
{
    public double width { get; } = width;
    public double height { get; } = height;

    public static RectfGeo From(double minLat, double maxLat, double minLon, double maxLon) => new(minLat, minLon, maxLat - minLat, maxLon - minLon);

    /// <summary>
    /// min latitude
    /// </summary>
    public double MinLat
    {
        get
        {
            return x;
        }
    }
    /// <summary>
    /// max latitude
    /// </summary>
    public double MaxLat
    {
        get
        {
            return x + width;
        }
    }
    /// <summary>
    /// min longitude
    /// </summary>
    public double MinLon
    {
        get
        {
            return y;
        }
    }
    /// <summary>
    /// max longitude
    /// </summary>
    public double MaxLon
    {
        get
        {
            return y + height;
        }
    }

    public PointGeo2D Center() => new(x + width / 2, y + height / 2);
    public ConvexHullD GetHull()
    {
        List<Point2D> area0 = [new(x, y), new(x + width, y), new(x + width, y + height), new(x, y + height)];
        ConvexHullD hull = new(area0);
        Debug.Assert(hull.Area() > 0);
        return hull;
    }

    public bool IsPointInArea(PointGeo2D p)
    {
        bool x = MinLat <= p.Lat && p.Lat <= MaxLat;
        bool y = MinLon <= p.Lon && p.Lon <= MaxLon;
        return x && y;
    }

    public override string ToString()
    {
        PointGeo2D point = new(MinLat, MinLon);
        return point.ToString() + $"_w{width * 10:0}_h{height * 10:0}";
    }

    public PointGeo2D[] SampleUniform(int N, Random random, ConvexHullF[] convexHulls)
    {
        PointGeo2D[] points = new PointGeo2D[N];
        for (int i = 0; i < N; ++i)
        {
            double vx = width * random.NextDouble() + MinLat;
            double vy = height * random.NextDouble() + MinLon;
            points[i] = Resample(convexHulls, new(vx, vy), random);
        }

        return points;
    }






    public PointGeo2D Resample(ConvexHullF[] convexHulls, PointGeo2D p, Random random)
    {
        var area = this;
        for (; convexHulls.Length > 0 && PointInHulls(convexHulls, p); p = new(random.NextDouble() * area.width + area.MinLat, random.NextDouble() * area.height + area.MinLon)) ;
        return p;
    }

    static bool PointInHulls(ConvexHullF[] convexHulls, PointGeo2D p) => convexHulls.Any(hull => hull.IsPointInHull(new((float)p.Lat, (float)p.Lon)));

    bool PointInRectf(PointGeo2D p)
    {
        bool f1 = p.Lat <= MaxLat && p.Lat >= MinLat;
        bool f2 = p.Lon <= MaxLon && p.Lon >= MinLon;
        return f1 && f2;
    }
}
