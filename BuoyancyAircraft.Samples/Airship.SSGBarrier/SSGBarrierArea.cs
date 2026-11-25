using System.Diagnostics;
using Aircraft.Core.ConvexHull;
using Airship.Deployment.SSGBarrier;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Spatial.Euclidean;

namespace BuoyancyAircraft.Samples.Airship.SSGBarrier;

public sealed record SSGBarrierArea(string Name, ConvexHullD AreaAtt, ConvexHullD AreaDef, PointGeo2D[] Sources, PointGeo2D[] Sinks, PointGeo2DSortMode PointSortMode, double[] SinkValues) {
    public SSGBarrierArea(string Name, ConvexHullD AreaAtt, ConvexHullD AreaDef, PointGeo2D[] Sources, PointGeo2D[] Sinks, PointGeo2DSortMode PointSortMode) : this(Name, AreaAtt, AreaDef, Sources, Sinks, PointSortMode, Enumerable.Repeat(1.0, Sinks.Length).ToArray()) { }
    public static Point2D[] SampleN(int N, ConvexHullD Area, Random random) {
        Point2D[] p = new Point2D[N];
        for (int i = 0; i < N; ++i) {
            p[i] = Area.Sample(random);
        }
        return p;
    }
    public static PointGeo2D[] SampleNGeo(int N, ConvexHullD Area, Random random) {
        PointGeo2D[] p = new PointGeo2D[N];
        for (int i = 0; i < N; ++i) {
            p[i] = new(Area.Sample(random));
        }
        return p;
    }

    public static SSGBarrierArea Simple(int N_source, int N_sink, int seed) {
        Random random = new(seed);
        List<Point2D> area0 = [new(16.73, 110.8), new(16.7, 117.9), new(22.8, 117.9), new(22.8, 110.8)];
        List<Point2D> area1 = [new(16.73 - 0.5, 110.8 - 1), new(16.7 - 0.5, 117.9 + 1), new(22.8 + 0.5, 117.9 + 1), new(22.8 + 0.5, 110.8 - 1)];
        RectfGeo area2 = new(17, 119, 4.5, 2), area3 = new(18, 110, 3, 1);
        area0.Reverse(); area1.Reverse();
        ConvexHullD areaDef = new(area0), areaAtt = new(area1);
        Debug.Assert(areaDef.Area() > 0);

        PointGeo2D[] sources = SampleNGeo(N_source, area2.GetHull(), random), sinks = SampleNGeo(N_sink, area3.GetHull(), random);

        return new(nameof(Simple) + seed, areaAtt, areaDef, sources, sinks, PointGeo2DSortMode.Lat);
    }

    /// <summary>
    /// 标准样例，矩形，纬度13度，经度8度，导弹的区域大1度
    /// 默认种子20260515，两个区域比较聚集
    /// 种子20260609，起点比较聚集，终点比较稀疏
    /// </summary>
    public static SSGBarrierArea Case2(int N_source, int N_sink, int seed) {
        List<Point2D> area0 = [new(10, 110.8), new(10, 117.9), new(22.8, 117.9), new(22.8, 110.8)];
        List<Point2D> area1 = [new(10 - 0.5, 110.8 - 1), new(10 - 0.5, 117.9 + 1), new(22.8 + 0.5, 117.9 + 1), new(22.8 + 0.5, 110.8 - 1)];
        RectfGeo area2 = new(13, 119, 8, 2), area3 = new(13, 110, 5, 1);
        area0.Reverse(); area1.Reverse();
        ConvexHullD areaDef = new(area0), areaAtt = new(area1);
        Debug.Assert(areaDef.Area() > 0);

        Random random = new(seed);
        PointGeo2D[] sources = SampleNGeo(N_source, area2.GetHull(), random), sinks = SampleNGeo(N_sink, area3.GetHull(), random);
        return new(nameof(Case2) + seed, areaAtt, areaDef, sources, sinks, PointGeo2DSortMode.Lat);
    }
    /// <summary>
    /// 标准样例，矩形，纬度13度，经度8度，导弹的区域大2度，在起点区域
    /// </summary>
    public static SSGBarrierArea Case3(int N_source, int N_sink, int seed) {
        List<Point2D> area0 = [new(10, 110.8), new(10, 117.9), new(22.8, 117.9), new(22.8, 110.8)];
        List<Point2D> area1 = [new(10 - 0.5, 110.8 - 1), new(10 - 0.5, 117.9 + 2), new(22.8 + 0.5, 117.9 + 2), new(22.8 + 0.5, 110.8 - 1)];
        RectfGeo area2 = new(13, 119, 8, 2), area3 = new(13, 110, 5, 1);
        area0.Reverse(); area1.Reverse();
        ConvexHullD areaDef = new(area0), areaAtt = new(area1);
        Debug.Assert(areaDef.Area() > 0);
        Random random = new(seed);
        PointGeo2D[] sources = SampleNGeo(N_source, area2.GetHull(), random), sinks = SampleNGeo(N_sink, area3.GetHull(), random);
        return new(nameof(Case3) + seed, areaAtt, areaDef, sources, sinks, PointGeo2DSortMode.Lat);
    }

    /// <summary>
    /// 标准样例，矩形，纬度13度，经度8度，导弹的区域大2度，终点稀疏，2个初始终点区域
    /// </summary>
    public static SSGBarrierArea Case3TargetSparse(int N_source, int N_sink, int seed) {
        List<Point2D> area0 = [new(10, 110.8), new(10, 117.9), new(22.8, 117.9), new(22.8, 110.8)];
        List<Point2D> area1 = [new(10 - 0.5, 110.8 - 1), new(10 - 0.5, 117.9 + 2), new(22.8 + 0.5, 117.9 + 2), new(22.8 + 0.5, 110.8 - 1)];
        RectfGeo area2 = new(13, 119, 8, 2);
        area0.Reverse(); area1.Reverse();
        RectfGeo[] sinks = [new(13, 110, 0.5, 0.5), new(17, 110, 0.5, 0.5)];

        ConvexHullD areaDef = new(area0), areaAtt = new(area1), areaSource = area2.GetHull();
        Debug.Assert(areaDef.Area() > 0);
        ArgumentOutOfRangeException.ThrowIfNotEqual(N_sink % sinks.Length, 0, $"N_sink should be mulitiples of {sinks.Length}, but get {N_sink}");
        Random random = new(seed);
        PointGeo2D[] sinks1 = sinks.SelectMany(s => SampleNGeo(N_sink / sinks.Length, s.GetHull(), random)).ToArray(), sources = SampleNGeo(N_source, areaSource, random);
        return new(nameof(Case3TargetSparse) + seed, areaAtt, areaDef, sources, sinks1, PointGeo2DSortMode.Lat);
    }
    /// <summary>
    /// 标准样例，矩形，纬度13度，经度8度，导弹的区域大2度，起点终点都稀疏
    /// </summary>
    public static SSGBarrierArea Case3BothSparse(int N_source, int N_sink, int seed) {
        List<Point2D> area0 = [new(10, 110.8), new(10, 117.9), new(22.8, 117.9), new(22.8, 110.8)];
        List<Point2D> area1 = [new(10 - 0.5, 110.8 - 1), new(10 - 0.5, 117.9 + 2), new(22.8 + 0.5, 117.9 + 2), new(22.8 + 0.5, 110.8 - 1)];
        RectfGeo area2 = new(13, 119, 8, 2);
        area0.Reverse(); area1.Reverse();
        RectfGeo[] sinks = [new(13, 110, 0.5, 0.5), new(17, 110, 0.5, 0.5)];
        RectfGeo[] sources = [new(12, 119, 0.5, 0.5), new(16, 119, 0.5, 0.5), new(18.5, 119, 0.5, 0.5)];

        ConvexHullD areaDef = new(area0), areaAtt = new(area1), areaSource = area2.GetHull();
        Debug.Assert(areaDef.Area() > 0);
        ArgumentOutOfRangeException.ThrowIfNotEqual(N_sink % sinks.Length, 0, $"N_sink should be mulitiples of {sinks.Length}, but get {N_sink}");
        ArgumentOutOfRangeException.ThrowIfNotEqual(N_source % sources.Length, 0, $"N_source should be mulitiples of {sources.Length}, but get {N_source}");
        Random random = new(seed);
        PointGeo2D[] sinks1 = sinks.SelectMany(s => SampleNGeo(N_sink / sinks.Length, s.GetHull(), random)).ToArray(),
            sources1 = sources.SelectMany(s => SampleNGeo(N_source / sources.Length, s.GetHull(), random)).ToArray();
        return new(nameof(Case3BothSparse) + seed, areaAtt, areaDef, sources1, sinks1, PointGeo2DSortMode.Lat);
    }

    /// <summary>
    /// 不规则区域，N_source需要为3的倍数，3个初始起点区域，source分散
    /// </summary>
    public static SSGBarrierArea HullArea4(int N_source, int N_sink, int seed) {
        List<Point2D> area0 = [new(19, 110), new(14.5, 111), new(21.6, 123.5), new(28, 123.4)];
        List<Point2D> area1 = [new(19, 110), new(12, 111), new(20, 124), new(28, 123.4)];
        List<Point2D> area2 = [new(22, 110.8), new(25.7, 119.6), new(27.4, 118), new(23, 110)];
        area0.Reverse(); area1.Reverse(); area2.Reverse();
        RectfGeo[] sources = [new(13.5, 113, 1, 1), new(17, 120, 1, 1), new(20.5, 123.5, 1, 1)];

        ConvexHullD areaDef = new(area0), areaAtt = new(area1), areaSink = new(area2);
        Debug.Assert(areaDef.Area() > 0);
        ArgumentOutOfRangeException.ThrowIfNotEqual(N_source % sources.Length, 0, $"N_source should be mulitiples of {sources.Length}, but get {N_source}");
        Random random = new(seed);
        PointGeo2D[] sources1 = sources.SelectMany(s => SampleNGeo(N_source / sources.Length, s.GetHull(), random)).ToArray(), sinks = SampleNGeo(N_sink, areaSink, random);
        return new(nameof(HullArea4) + seed, areaAtt, areaDef, sources1, sinks, PointGeo2DSortMode.Lat);
    }
    /// <summary>
    /// 不规则区域，N_source需要为3的倍数，3个初始起点区域，source集中
    /// </summary>
    public static SSGBarrierArea HullArea5(int N_source, int N_sink, int seed) {
        List<Point2D> area0 = [new(19, 110), new(14.5, 111), new(21.6, 123.5), new(28, 123.4)];
        List<Point2D> area1 = [new(19, 110), new(12, 111), new(20, 124), new(28, 123.4)];
        List<Point2D> area2 = [new(22, 110.8), new(25.7, 119.6), new(27.4, 118), new(23, 110)];
        area0.Reverse(); area1.Reverse(); area2.Reverse();
        RectfGeo[] sources = [new(15, 117, 2, 2)];

        ConvexHullD areaDef = new(area0), areaAtt = new(area1), areaSink = new(area2);
        Debug.Assert(areaDef.Area() > 0);
        ArgumentOutOfRangeException.ThrowIfNotEqual(N_source % sources.Length, 0, $"N_source should be mulitiples of {sources.Length}, but get {N_source}");
        Random random = new(seed);
        PointGeo2D[] sources1 = sources.SelectMany(s => SampleNGeo(N_source / sources.Length, s.GetHull(), random)).ToArray(), sinks = SampleNGeo(N_sink, areaSink, random);
        return new(nameof(HullArea5) + seed, areaAtt, areaDef, sources1, sinks, PointGeo2DSortMode.Lat);
    }

    public static SSGBarrierArea LargeArea(int N_source, int N_sink, int seed) {
        List<Point2D> area0 = [new(15, 111), new(18, 118), new(33, 128), new(43, 120), new(25, 100)];
        List<Point2D> area1 = [new(10, 107), new(10, 117), new(36, 140), new(43, 120), new(25, 100)];
        area0.Reverse(); area1.Reverse();
        RectfGeo[] sources = [new(16, 123, 2, 2), new(12, 111, 2, 2), new(32, 131, 2, 2)];
        // RectfGeo[] sinks = [new(23, 113, 2, 2), new(26, 118, 2, 2), new(40, 116, 2, 2), new(31, 121, 2, 2)];
        RectfGeo[] sinks = [new(23, 113, 2, 2), new(40, 116, 2, 2), new(31, 121, 2, 2)];

        ConvexHullD areaDef = new(area0), areaAtt = new(area1);
        Debug.Assert(areaDef.Area() > 0);

        int N_source1 = (int)(Ceiling(N_source / 3.0) * 3);
        int N_sink1 = (int)(Ceiling(N_sink / (double)sinks.Length) * sinks.Length);
        Random random = new(seed);
        PointGeo2D[] sources1 = sources.SelectMany(s => SampleNGeo(N_source1 / sources.Length, s.GetHull(), random)).ToArray(),
         sinks1 = sinks.SelectMany(s => SampleNGeo(N_sink1 / sinks.Length, s.GetHull(), random)).ToArray();
        return new(nameof(LargeArea) + seed, areaAtt, areaDef, sources1[..N_source], sinks1[..N_sink], PointGeo2DSortMode.Lat);
    }

    public static SSGBarrierArea LargeAreaV(int N_source, int N_sink, int seed) {
        var area = LargeArea(N_source, N_sink, seed);
        Random random = new(seed);
        return area with {
            Name = nameof(LargeAreaV) + seed,
            SinkValues = (Vector<double>.Build.Random(N_sink) * 0.2 + 0.8).ToArray()
        };
    }

    public static SSGBarrierArea FromName(string name, int N_source, int N_sink, int seed) {
        return name switch {
            nameof(Simple) => Simple(N_source, N_sink, seed),
            nameof(Case2) => Case2(N_source, N_sink, seed),
            nameof(Case3) => Case3(N_source, N_sink, seed),
            nameof(Case3TargetSparse) => Case3TargetSparse(N_source, N_sink, seed),
            nameof(Case3BothSparse) => Case3BothSparse(N_source, N_sink, seed),
            nameof(HullArea4) => HullArea4(N_source, N_sink, seed),
            nameof(HullArea5) => HullArea5(N_source, N_sink, seed),
            nameof(LargeArea) => LargeArea(N_source, N_sink, seed),
            nameof(LargeAreaV) => LargeAreaV(N_source, N_sink, seed),
            // AI 生成样例
            nameof(Triangle) => Triangle(N_source, N_sink, seed),
            nameof(Pentagon) => Pentagon(N_source, N_sink, seed),
            nameof(LongCorridor) => LongCorridor(N_source, N_sink, seed),
            // nameof(NarrowNeck) => NarrowNeck(N_source, N_sink, seed), // 非凸
            nameof(MultiSourceMultiSink) => MultiSourceMultiSink(N_source, N_sink, seed),
            nameof(CircleApprox) => CircleApprox(N_source, N_sink, seed),
            nameof(RandomConvex) => RandomConvex(N_source, N_sink, seed), // 跑不通，先不管
            _ => throw new ArgumentException($"{name} does not exists.")
        };
    }

    // -----------------------以下AI生成代码-----------------------
    // ============ 公共辅助 ============

    /// <summary>对凸包每条边沿外法向平移 margin，返回更大的凸包（保证 att ⊇ def）。</summary>
    private static ConvexHullD MakeAtt(ConvexHullD def, double margin) {
        var pts = def.Points;                 // 保证 CCW 凸包
        int n = pts.Count;

        var shifted = new List<Point2D>(n * 2);
        for (int i = 0; i < n; i++) {
            var a = pts[i];
            var b = pts[(i + 1) % n];
            double ex = b.X - a.X, ey = b.Y - a.Y;
            double len = Math.Sqrt(ex * ex + ey * ey);
            if (len < 1e-12) continue;
            // CCW 外法向 = 边方向右转 90° = (ey, -ex)
            double nx = ey / len;
            double ny = -ex / len;
            shifted.Add(new Point2D(a.X + nx * margin, a.Y + ny * margin));
            shifted.Add(new Point2D(b.X + nx * margin, b.Y + ny * margin));
        }
        var att = ConvexHullAlgoD.GetHull(shifted);
        foreach (var p in pts)
            Debug.Assert(att.IsPointInHullClose(p), "areaAtt must contain areaDef");
        return att;
    }
    /// <summary>正 n 边形顶点。</summary>
    private static List<Point2D> RegularPolygonPoints(double cx, double cy, double radius, int n, double phase = 0) {
        var pts = new List<Point2D>(n);
        for (int i = 0; i < n; i++) {
            double a = phase + 2 * Math.PI * i / n;
            pts.Add(new Point2D(cx + radius * Math.Cos(a), cy + radius * Math.Sin(a)));
        }
        return pts;
    }

    /// <summary>正 n 边形凸包（用于源/汇区域）。</summary>
    private static ConvexHullD RegularPolygonHull(double cx, double cy, double radius, int n, double phase = 0)
        => new(RegularPolygonPoints(cx, cy, radius, n, phase));

    /// <summary>小矩形区域凸包。</summary>
    private static ConvexHullD RectHull(double x, double y, double w, double h)
        => new RectfGeo(x, y, w, h).GetHull();

    /// <summary>把 n 个点均匀分到各 hull 采样，截断到 n。</summary>
    private static PointGeo2D[] SampleAcross(int n, ConvexHullD[] hulls, Random random) {
        int n1 = (int)(Math.Ceiling(n / (double)hulls.Length) * hulls.Length);
        return hulls.SelectMany(h => SampleNGeo(n1 / hulls.Length, h, random)).Take(n).ToArray();
    }

    // ============ 样例 ============

    /// <summary>三角形 def（小尺度），单源单汇，源汇最近距离 &lt; 16。</summary>
    public static SSGBarrierArea Triangle(int N_source, int N_sink, int seed) {
        // 原三角形围绕质心 (27.33, 122) 缩放 0.6
        List<Point2D> area0 = [new(18.13, 116), new(37.33, 117.2), new(26.53, 132.8)];
        ConvexHullD areaDef = new(area0);
        ConvexHullD areaAtt = MakeAtt(areaDef, 1.5);
        Debug.Assert(areaDef.Area() > 0);

        Random random = new(seed);
        var sources = new[] { RectHull(21.13, 119, 1.2, 1.2) };
        var sinks = new[] { RectHull(25.33, 128, 1.2, 1.2) };
        return new(nameof(Triangle) + seed, areaAtt, areaDef,
            SampleAcross(N_source, sources, random), SampleAcross(N_sink, sinks, random),
            PointGeo2DSortMode.Lat);
    }

    /// <summary>不规则五边形 def（小尺度），单源单汇，源汇最近距离 &lt; 16。</summary>
    public static SSGBarrierArea Pentagon(int N_source, int N_sink, int seed) {
        // 原五边形围绕质心 (28, 122.4) 缩放 0.6
        List<Point2D> area0 = [new(18.4, 116.16), new(29.2, 113.76), new(38.8, 120.96), new(32.8, 132.96), new(20.8, 128.16)];
        ConvexHullD areaDef = new(area0), areaAtt = MakeAtt(areaDef, 1.8);
        Debug.Assert(areaDef.Area() > 0);

        Random random = new(seed);
        var sources = new[] { RectHull(21.4, 119.76, 1.2, 1.2) };
        var sinks = new[] { RectHull(31.6, 126.96, 1.2, 1.2) };
        return new(nameof(Pentagon) + seed, areaAtt, areaDef,
            SampleAcross(N_source, sources, random), SampleAcross(N_sink, sinks, random),
            PointGeo2DSortMode.Lat);
    }

    /// <summary>细长斜走廊（小尺度），源近端、汇远端，源汇最近距离 &lt; 16。</summary>
    public static SSGBarrierArea LongCorridor(int N_source, int N_sink, int seed) {
        // 原走廊围绕质心 (28, 124) 缩放 0.5
        List<Point2D> area0 = [
            new(19.25, 118.5),
        new(16.75, 117.5),
        new(35.25, 126.5),
        new(32.75, 127.5),
    ];
        ConvexHullD areaDef = ConvexHullAlgoD.GetHull(area0), areaAtt = MakeAtt(areaDef, 1.0);
        Debug.Assert(areaDef.Area() > 0);

        Random random = new(seed);
        var sources = new[] { RectHull(19.5, 118.5, 0.8, 0.8) };
        var sinks = new[] { RectHull(32.5, 124.5, 0.8, 0.8) };
        return new(nameof(LongCorridor) + seed, areaAtt, areaDef,
            SampleAcross(N_source, sources, random), SampleAcross(N_sink, sinks, random),
            PointGeo2DSortMode.Lat);
    }

    /// <summary>3 源 3 汇（小尺度，正六边形区域），源汇最近距离 &lt; 16。</summary>
    public static SSGBarrierArea MultiSourceMultiSink(int N_source, int N_sink, int seed) {
        // 原矩形围绕质心 (31, 123) 缩放 0.5
        List<Point2D> area0 = [new(20.5, 115.5), new(40.5, 116.5), new(41.5, 130.5), new(21.5, 129.5)];
        ConvexHullD areaDef = new(area0), areaAtt = MakeAtt(areaDef, 1.5);
        Debug.Assert(areaDef.Area() > 0);

        var sources = new[] {
        RegularPolygonHull(24.5, 119.5, 0.8, 6),
        RegularPolygonHull(30.5, 117.5, 0.8, 6),
        RegularPolygonHull(24.5, 125.5, 0.8, 6),
    };
        var sinks = new[] {
        RegularPolygonHull(37.5, 127.5, 0.8, 6),
        RegularPolygonHull(30.5, 128.5, 0.8, 6),
        RegularPolygonHull(37.5, 119.5, 0.8, 6),
    };
        Random random = new(seed);
        return new(nameof(MultiSourceMultiSink) + seed, areaAtt, areaDef,
            SampleAcross(N_source, sources, random), SampleAcross(N_sink, sinks, random),
            PointGeo2DSortMode.Lat);
    }

    /// <summary>圆形 def/att（小尺度，16/24 边形），中心源、环状 8 汇，源汇最近距离 &lt; 16。</summary>
    public static SSGBarrierArea CircleApprox(int N_source, int N_sink, int seed) {
        double cx = 30, cy = 122, r = 7.5;
        List<Point2D> area0 = RegularPolygonPoints(cx, cy, r, 16);
        var area1 = RegularPolygonPoints(cx, cy, r + 1.25, 24);
        ConvexHullD areaDef = new(area0), areaAtt = new(area1);
        Debug.Assert(areaDef.Area() > 0);

        Random random = new(seed);
        var sources = new[] { RegularPolygonHull(cx, cy, 0.8, 8) };
        var sinks = Enumerable.Range(0, 8).Select(i => {
            double a = 2 * Math.PI * i / 8;
            return RegularPolygonHull(cx + (r - 1.5) * Math.Cos(a), cy + (r - 1.5) * Math.Sin(a), 0.6, 6);
        }).ToArray();
        return new(nameof(CircleApprox) + seed, areaAtt, areaDef,
            SampleAcross(N_source, sources, random), SampleAcross(N_sink, sinks, random),
            PointGeo2DSortMode.Lat);
    }

    /// <summary>随机凸包 def，随机 1~3 源 + 1~3 汇（正六边形区域，中心在 def 内）。</summary>
    public static SSGBarrierArea RandomConvex(int N_source, int N_sink, int seed) {
        Random random = new(seed);
        // 随机凸包 def（内联，去掉单独辅助）
        var raw = new List<Point2D>();
        int nDef = 6 + random.Next(0, 6);
        for (int i = 0; i < nDef; i++) {
            double a = random.NextDouble() * 2 * Math.PI;
            double rad = 16 * Math.Sqrt(random.NextDouble());
            raw.Add(new Point2D(30 + rad * Math.Cos(a), 122 + rad * Math.Sin(a)));
        }
        ConvexHullD areaDef = ConvexHullAlgoD.GetHull(raw), areaAtt = MakeAtt(areaDef, 2.5);
        Debug.Assert(areaDef.Area() > 0);

        int nSrc = 1 + random.Next(0, 3), nSnk = 1 + random.Next(0, 3);
        ConvexHullD[] RandHulls(int count) {
            var list = new List<ConvexHullD>(count);
            int guard = 0;
            while (list.Count < count && guard++ < 1000) {
                double x = 14 + random.NextDouble() * 32, y = 110 + random.NextDouble() * 24;
                if (areaDef.IsPointInHull(new Point2D(x, y)))
                    list.Add(RegularPolygonHull(x, y, 1.3, 6));
            }
            while (list.Count < count) list.Add(RegularPolygonHull(30, 122, 1.3, 6)); // 兜底
            return list.ToArray();
        }
        return new(nameof(RandomConvex) + seed, areaAtt, areaDef,
            SampleAcross(N_source, RandHulls(nSrc), random), SampleAcross(N_sink, RandHulls(nSnk), random),
            PointGeo2DSortMode.Lat);
    }
}