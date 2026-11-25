using System.Diagnostics;
using Aircraft.Core.ConvexHull;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Spatial.Euclidean;
using MathNet.Spatial.Units;
using Microsoft.Extensions.Logging;

namespace Airship.Deployment.Comm;

public static class PopulationGenerator {
    const double perturbDelta = Math.PI / 6.0, alphaMin = 0, alphaMax = 0.9;
    // 获取所有方向策略（排除无效项）
    static readonly DirectionStrategy[] allDirections = Enum.GetValues<DirectionStrategy>()
          // .Where(d => d != DirectionStrategy.None) // 如果有 None 的话
          .ToArray();

    // 获取所有 alpha 策略
    static readonly AlphaStrategy[] allAlphas = Enum.GetValues<AlphaStrategy>().Where(d => d != AlphaStrategy.DegreeAdpative)
         // .Where(a => a != AlphaStrategy.None)
         .ToArray();

    // ----------------------------------vector disk初始化----------------------------------------
    /// <summary>
    /// 在圆盘上采样，经度上会修正
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">phi=±90°</exception>
    /// <exception cref="DiskSampleHullIntersectionException">在convexhull无解</exception>
    public static PointGeo2D DiskSample(PointGeo2D p_center, Random random, double radiusDegree, ConvexHullF[] convexHulls) {
        PointGeo2D target;
        int count = 0, maxRetry = 100;
        double cosphi = Cos(p_center.Latitude.Radians);
        ArgumentOutOfRangeException.ThrowIfEqual(cosphi, 0);
        Vector2D delta;
        do {
            delta = random.ConcentricSampleDisk() * radiusDegree;
            target = new(p_center.Lat + delta.X, p_center.Lon + delta.Y / cosphi);
        } while (DeployOptimShared.PointInHulls(convexHulls, target) && count++ < maxRetry);

        if (count >= maxRetry) {
            // 可能在convexhull内部且最小距离大于radiusDegree，抛出异常
            var intersectedHullsIndex = DeployOptimHelper.IntersectedHull(convexHulls, p_center, radiusDegree);

            if (intersectedHullsIndex.Count > 0) {
                var (i, d) = intersectedHullsIndex[0];
                throw new DiskSampleHullIntersectionException($"point {p_center} in the hull index {i} and distance {d} > radius {radiusDegree}, intersected count: {intersectedHullsIndex.Count}", intersectedHullsIndex.Select(pair => convexHulls[pair.index]).ToArray(), p_center);
            }

            do {
                delta = random.ConcentricSampleDisk() * radiusDegree;
                target = new(p_center.Lat + delta.X, p_center.Lon + delta.Y / cosphi);
            } while (DeployOptimShared.PointInHulls(convexHulls, target));
        }

#if DEBUG
        if (!DeployOptimShared.IsPointInRadius(p_center, target, radiusDegree, 1E-12)) {
            bool f = DeployOptimShared.IsPointInRadius(p_center, target, radiusDegree, 1E-12);
            Debug.Assert(f);
        }
#endif
        return target;
    }


    public sealed class DiskSampleHullIntersectionException(string message, ConvexHullF[] convexHull, PointGeo2D point) : HullIntersectionException(message) {
        public override ConvexHullF[] IntersctionHulls => convexHull;
        public PointGeo2D Point => point;
    }
    /// <summary>
    /// 随机初始化，如果点在convexHulls内部，会删除相应的convexhull，然后logger Error
    /// </summary>
    public static Vector<double> InitializePopulation(PointGeo2D[] p_centers, Random random, double radiusDegree, ConvexHullF[] convexHulls, ILogger? logger = null) {
        return DeployOptimShared.Point2DsEncode(InitializePointGeo2Ds(p_centers, random, radiusDegree, convexHulls, logger).points);
    }

    /// <returns>额外返回删除重叠的hull</returns>
    public static (PointGeo2D[] points, ConvexHullF[] hulls) InitializePointGeo2Ds(PointGeo2D[] p_centers, Random random, double radiusDegree, ConvexHullF[] convexHulls, ILogger? logger = null) {
        int n = p_centers.Length;
        PointGeo2D[] newPolulation = new PointGeo2D[n];
        ConvexHullF[] convexHulls1 = convexHulls;
        for (int i = 0; i < n; ++i) {
            try {
                newPolulation[i] = DiskSample(p_centers[i], random, radiusDegree, convexHulls1);
            } catch (DiskSampleHullIntersectionException exception) {
                // 如果找不到，删除hull
                var forbiddenHulls2 = convexHulls1.ToList();
                foreach (var hull in exception.IntersctionHulls) {
                    forbiddenHulls2.Remove(hull);
                }
                convexHulls1 = forbiddenHulls2.ToArray();
                newPolulation[i] = DiskSample(p_centers[i], random, radiusDegree, forbiddenHulls2.ToArray());
                logger?.LogError("InitializePointGeo2Ds hull intersection: {}, re disk sample", exception.Message);
            }
        }
        return (newPolulation, convexHulls1);
    }

    // -------------------------------------按照策略初始化角度----------------------------------------
    /// <summary>
    /// 根据策略生成节点 i 的移动方向（弧度）
    /// </summary>
    /// <summary>
    /// 为所有节点批量采样移动方向。
    /// </summary>
    /// <param name="p_currents">初始位置数组</param>
    /// <param name="strategy">方向策略</param>
    /// <param name="random">随机数生成器</param>
    /// <param name="perturbDelta">非随机策略下的扰动幅度（弧度）</param>
    /// <param name="centerOverride">可选的中心点覆盖</param>
    /// <returns>每个节点对应的方向 Angle[]</returns>
    static Angle[] SampleDirections(
       PointGeo2D[] p_currents,
       DirectionStrategy strategy,
       Random random,
       double perturbDelta, // ±30° 扰动
       PointGeo2D[] pds,
       PointGeo2D center) {
        if (p_currents == null || p_currents.Length == 0)
            return [];

        var n = p_currents.Length;
        var directions = new Angle[n];

        double randDirection = random.NextDouble() * 2 * Math.PI;

        if (strategy is DirectionStrategy.Voronoi) {
            directions = GenerateDirection(p_currents, pds);
        }

        for (int i = 0; i < n; i++) {
            var xi = p_currents[i];

            if (strategy == DirectionStrategy.Random) {
                directions[i] = Angle.FromRadians(random.NextDouble() * 2 * Math.PI);
                continue;
            }

            // 计算基础方向（注意：使用 Radians 属性）
            double dxLat = center.Latitude.Radians - xi.Latitude.Radians;
            double dxLon = center.Longitude.Radians - xi.Longitude.Radians;

            double baseAngle = strategy switch {
                DirectionStrategy.Inward => Math.Atan2(dxLon, dxLat),
                DirectionStrategy.Outward => Math.Atan2(-dxLon, -dxLat), // 反向
                DirectionStrategy.TangentialClockwise => Math.Atan2(dxLon, dxLat) + Math.PI / 2,
                DirectionStrategy.TangentialCounterClockwise => Math.Atan2(dxLon, dxLat) - Math.PI / 2,
                DirectionStrategy.Single => randDirection,
                DirectionStrategy.Voronoi => directions[i].Radians,
                _ => throw new InvalidOperationException("Unexpected non-random strategy.")
            };

            // 添加扰动
            var noise = (random.NextDouble() - 0.5) * 2 * perturbDelta;
            baseAngle += noise;

            // 归一化并转为 Angle
            directions[i] = Angle.FromRadians(AngleUtil.NormalizeAngle(baseAngle));
            Debug.Assert(double.IsFinite(baseAngle));
        }

        return directions;
    }


    private static PointGeo2D ComputeCentroid(PointGeo2D[] p_currents) {
        double lat = 0, lon = 0;
        foreach (var p in p_currents) {
            lat += p.Lat;
            lon += p.Lon;
        }
        return new PointGeo2D(lat / p_currents.Length, lon / p_currents.Length);
    }

    public enum DirectionStrategy {
        Inward,
        Outward,
        TangentialClockwise,
        TangentialCounterClockwise,
        Random,
        Voronoi,
        Single // 往同一个方向运动
    }

    // -------------------------------------按照策略初始化距离----------------------------------------
    /// <summary>
    /// 根据策略生成节点 i 的移动比例 α ∈ [0,1]
    /// </summary>
    /// <summary>
    /// 生成一个随机的移动比例 α ∈ [minAlpha, maxAlpha]
    /// </summary>
    static double SampleAlpha(Random random, double minAlpha, double maxAlpha) {
        if (minAlpha < 0 || maxAlpha > 1 || minAlpha > maxAlpha)
            throw new ArgumentOutOfRangeException("Alpha bounds must satisfy 0 ≤ minAlpha ≤ maxAlpha ≤ 1.");

        return random.NextDouble() * (maxAlpha - minAlpha) + minAlpha;
    }

    /// <summary>
    /// 基于 Laplacian 矩阵自适应分配移动比例 α：
    /// - 高度数节点（中心/枢纽）→ 小 α（少动）
    /// - 低度数节点（边缘）→ 大 α（多动）
    /// </summary>
    static double[] SampleAlphaDegreeAdaptive(Random random, double[] degrees, double alphaMin, double alphaMax) {
        int n = degrees.Length;
        double maxDegree = degrees.Max();

        // 防止除零（孤立节点）
        if (maxDegree == 0) maxDegree = 1.0;

        var alphas = new double[n];
        for (int i = 0; i < n; i++) {
            // 归一化度数 ∈ [0,1]
            double normDeg = degrees[i] / maxDegree;

            // 反比映射：高度数 → 小 α
            // 使用线性反比：α = αMax - (αMax - αMin) * normDeg
            double alpha = alphaMax - (alphaMax - alphaMin) * normDeg;

            // 可选：加入轻微随机扰动以打破对称性
            double noise = (random.NextDouble() - 0.5) * 0.1; // ±0.05
            alpha = Math.Clamp(alpha + noise, alphaMin, alphaMax);

            alphas[i] = alpha;
        }

        return alphas;
    }
    // FIXME: 当前的移动距离调整是反过来的
    /// <summary>
    /// 基于到中心的距离分配移动比例 α：
    /// - 距离近的移动大（α 大）
    /// - 距离远的移动小（α 小）
    /// </summary>
    static double[] SampleAlphaDistanceAdaptive(
    PointGeo2D[] p_currents, Random random, PointGeo2D center, double alphaMin, double alphaMax) {
        if (alphaMin < 0 || alphaMax > 1 || alphaMin > alphaMax)
            throw new ArgumentOutOfRangeException("Alpha bounds must satisfy 0 ≤ alphaMin ≤ alphaMax ≤ 1.");

        int n = p_currents.Length;
        if (n == 0) return Array.Empty<double>();

        // Step 1: 计算每个点到中心的平面距离（单位：度）
        var distances = new double[n];
        double maxDist = 0;

        for (int i = 0; i < n; i++) {
            distances[i] = p_currents[i].DistanceL2_rad(center);
            if (distances[i] > maxDist) maxDist = distances[i];
        }

        // Step 2: 归一化并反向映射到 [alphaMin, alphaMax]
        var alphas = new double[n];
        const double eps = 1e-9;

        for (int i = 0; i < n; i++) {
            double normDist = (maxDist < eps) ? 0.0 : distances[i] / maxDist; // 防止除零
            double alpha = alphaMin + (alphaMax - alphaMin) * normDist;       // 距离越大，α 越大

            // 可选：加入轻微随机扰动（±2%）
            double noise = (random.NextDouble() - 0.5) * 0.04 * (alphaMax - alphaMin);
            alpha = Math.Clamp(alpha + noise, alphaMin, alphaMax);

            alphas[i] = alpha;
        }

        return alphas;
    }
    /// <summary>
    /// 根据λ2-Capabilities的值分配位置，值越小，移动的距离越小，值越大，说明改进空间越大，越需要探索
    /// </summary>
    /// <param name="p_currents"></param>
    /// <param name="random"></param>
    /// <param name="alphaMin"></param>
    /// <param name="alphaMax"></param>
    /// <returns></returns>
    static double[] SampleAlphaCapacitiesλ2Adaptive(Random random, double[] capacities, double λ2, double alphaMin, double alphaMax) {
        int n = capacities.Length;
        double[] λ2_sub_cap = capacities.Select(c => Max(λ2 - c, 0)).ToArray();
        // Step 2: 归一化并反向映射到 [alphaMin, alphaMax]
        var alphas = new double[n];
        const double eps = 1e-9;
        double maxλ2_sub_cap = λ2_sub_cap.Max();

        for (int i = 0; i < n; i++) {
            double normDist = (maxλ2_sub_cap < eps) ? 0.0 : λ2_sub_cap[i] / maxλ2_sub_cap; // 防止除零
            double alpha = alphaMin + (alphaMax - alphaMin) * normDist;       // 距离越大，α 越大

            // 可选：加入轻微随机扰动（±2%）
            double noise = (random.NextDouble() - 0.5) * 0.04 * (alphaMax - alphaMin);
            alpha = Math.Clamp(alpha + noise, alphaMin, alphaMax);

            alphas[i] = alpha;
        }

        return alphas;
    }



    public enum AlphaStrategy {
        DegreeAdpative,
        Random, DistanceAdaptive, Capacitiesλ2Adaptive
        // Voronoi
    }

    // -------------------------------------给定初始位置和目标位置生成，适用于Voronoi----------------------------------------
    /// <summary>
    /// 根据初始位置和目标位置，计算每个节点的移动方向（弧度）。
    /// 若两点重合，返回 0（无方向）。
    /// </summary>
    public static Angle[] GenerateDirection(PointGeo2D[] p_currents, PointGeo2D[] pds) {
        if (p_currents.Length != pds.Length)
            throw new ArgumentException("Initial and target arrays must have same length.");

        Angle[] thetas = new Angle[p_currents.Length];
        for (int i = 0; i < p_currents.Length; ++i) {
            var from = p_currents[i];
            var to = pds[i];

            double dLat = to.Lat - from.Lat;
            double dLon = to.Lon - from.Lon;

            // 方向：atan2(Δlon, Δlat) —— 因为 lat 类似 y 轴，lon 类似 x 轴
            // 但注意：在地理中，通常 (lat, lon) 对应 (y, x)，所以：
            //   x = lon, y = lat → atan2(dx, dy) = atan2(dLon, dLat)
            double theta = Math.Atan2(dLon, dLat);

            thetas[i] = Angle.FromRadians(theta); // 弧度，范围 [-π, π]
        }
        return thetas;
    }

    /// <summary>
    /// 根据初始位置、目标位置和最大移动半径（度），计算归一化移动比例 α ∈ [0, 1]。
    /// 若实际距离 > radiusDegree，则 α = 1（已达边界）。
    /// </summary>
    static double[] GenerateAlpha(PointGeo2D[] p_currents, PointGeo2D[] pds, double[] radii) {
        if (p_currents.Length != pds.Length)
            throw new ArgumentException("Initial and target arrays must have same length.");

        double[] alphas = new double[p_currents.Length];
        for (int i = 0; i < p_currents.Length; ++i) {
            var from = p_currents[i];
            var to = pds[i];

            // 平面欧氏距离（单位：rad）
            double distance = to.DistanceL2_rad(from) * 360 / 2 / PI;

            // 归一化到 [0, 1]，上限为 1
            double alpha = distance / radii[i];
            alphas[i] = Math.Min(alpha, 1.0); // 确保 α ≤ 1
        }
        return alphas;
    }


    // -------------------------------------核心的生成代码----------------------------------------


    /// <summary>
    /// 基于方向+距离策略生成一个候选拓扑（满足约束）
    /// </summary>
    public static Vector<double> GenerateCandidateTopology(
    PointGeo2D[] p_centers,
    PointGeo2D[] p_currents,
    HeuristicParams heuristicParams,
    double[] radii, // 每个节点的最大移动半径（单位：度）
    DirectionStrategy dirStrategy,
    AlphaStrategy alphaStrategy,
    Random random,
    ConvexHullF[] forbiddenHulls, // 禁飞区等障碍物
    PointGeo2D[] pds,
    int maxRetry = 10,
    ILogger? logger = null) {
        int n = p_centers.Length;
        var candidates = new PointGeo2D[n];
        var center = ComputeCentroid(p_currents);
        ConvexHullF[] forbiddenHulls1 = forbiddenHulls;

        double[] alphas = alphaStrategy switch {
            // AlphaStrategy.Uniform => Enumerable.Repeat(SampleAlpha(random, alphaMin, alphaMax), n).ToArray(),
            AlphaStrategy.Random => Enumerable.Range(0, n).Select(_ => SampleAlpha(random, alphaMin, alphaMax)).ToArray(),
            AlphaStrategy.DegreeAdpative => SampleAlphaDegreeAdaptive(random, heuristicParams.Degrees, alphaMin, alphaMax),
            AlphaStrategy.DistanceAdaptive => SampleAlphaDistanceAdaptive(p_currents, random, center, alphaMin, alphaMax),
            AlphaStrategy.Capacitiesλ2Adaptive => SampleAlphaCapacitiesλ2Adaptive(random, heuristicParams.Capacities, heuristicParams.λ2, alphaMin, alphaMax),
            // AlphaStrategy.Voronoi => GenerateAlpha(p_currents, pds, radii),
            _ => throw new NotImplementedException()
        };
        Angle[] thetas = SampleDirections(p_currents, dirStrategy, random, perturbDelta, pds, center);
        for (int i = 0; i < n; i++) {
            var theta = thetas[i];
            // 最大允许移动距离需要计算直线和椭圆的干涉，确定最大移动向量
            double cosphi = Cos(p_centers[i].Latitude.Radians);
            var centerP2D = p_centers[i].ToPoint2D();
            var origin = p_currents[i].ToPoint2D();
            double b = radii[i] / cosphi;

            PointGeo2D proposed;
            // 当前点可能不在椭圆内（只考虑了椭圆内部的情况），此时退化为离椭圆最近的点
            if (EllipseRayIntersection.IsPointOutsideEllipse(origin, centerP2D, radii[i], b)) {
                double ellipseVal = EllipseRayIntersection.EllipseValue(origin, centerP2D, radii[i], b);
                logger?.LogWarning("current {} is outside the ellipse (value {}), radius {}, snap to nearest point on ellipse",
                    p_currents[i], ellipseVal, radii[i]);
                var nearest = EllipseRayIntersection.NearestPointOnEllipse(origin, centerP2D, radii[i], b);
                // 向内缩放一点，避免浮点误差导致刚好在边界外
                proposed = new PointGeo2D(
                    centerP2D.X + (nearest.X - centerP2D.X) * (1 - 1e-9),
                    centerP2D.Y + (nearest.Y - centerP2D.Y) * (1 - 1e-9));
            } else {
                var intersections = EllipseRayIntersection.IntersectRayEllipse(centerP2D, radii[i], b, origin, theta);
                Debug.Assert(intersections.Count >= 1);
                var delta = intersections[0] - origin;

                // 目标位置（笛卡尔近似，适用于小范围）
                proposed = new PointGeo2D(origin.X + delta.X * alphas[i], origin.Y + delta.Y * alphas[i]);
            }
#if DEBUG
            // test 不满足半径约束的情况
            if (!DeployOptimShared.IsPointInRadius(p_centers[i], proposed, radii[i], 1E-12)) {
                DeployOptimShared.IsPointInRadius(p_centers[i], proposed, radii[i], 1E-12);
                throw new ArithmeticException($"point out of radius, center: {p_centers[i]}, proposed: {proposed}, radius: {radii[i]}, index: {i}");
            }

#endif

            // 若落入禁飞区，则重试（最多 maxRetry 次），否则保留原位或随机微调
            int retry = 0;
            while (DeployOptimShared.PointInHulls(forbiddenHulls1, proposed) && retry < maxRetry) {
                // 小幅扰动方向
                double moveDist = radii[i] * alphas[i];
                theta += Angle.FromRadians((random.NextDouble() - 0.5) * Math.PI / 3); // ±30°
                var dLat = moveDist * Math.Cos(theta.Radians);
                var dLon = moveDist * Math.Sin(theta.Radians);
                proposed = new PointGeo2D(p_centers[i].Lat + dLat, p_centers[i].Lon + dLon / cosphi);
                retry++;
            }

            // 若仍无效，退回到安全位置（如原点或圆内随机点）
            bool proposedInHull = DeployOptimShared.PointInHulls(forbiddenHulls1, proposed);
            if (proposedInHull) {
                try {
                    proposed = DiskSample(p_centers[i], random, radii[i], forbiddenHulls1);
                } catch (DiskSampleHullIntersectionException exception) {
                    // 如果找不到，删除hull
                    var forbiddenHulls2 = forbiddenHulls1.ToList();
                    foreach (var hull in exception.IntersctionHulls) {
                        forbiddenHulls2.Remove(hull);
                    }
                    // 替换原有的forbiddenHulls1防止重复Log error
                    forbiddenHulls1 = forbiddenHulls2.ToArray();
                    proposed = DiskSample(p_centers[i], random, radii[i], forbiddenHulls1);
                    logger?.LogError("GenerateCandidateTopology hull intersection: {}, re disk sample", exception.Message);
                }
            }

            if (!DeployOptimShared.IsPointInRadius(p_centers[i], proposed, radii[i], 1E-12)) {
                DeployOptimShared.IsPointInRadius(p_centers[i], proposed, radii[i], 1E-12);
                throw new ArithmeticException($"point out of radius, center: {p_centers[i]}, proposed: {proposed}, radius: {radii[i]}, index: {i}");
            }

            candidates[i] = proposed;
        }

        return DeployOptimShared.Point2DsEncode(candidates);
    }

    // 可选：过滤掉不兼容的组合（例如 DegreeAdaptive 必须配合 Laplacian，但这里假设有）
    // 也可加入黑名单：
    static HashSet<(DirectionStrategy, AlphaStrategy)> blacklisted = new() {
        // 示例：若认为随机方向 + 均匀 alpha 太普通，可跳过（通常不需要）
        (DirectionStrategy.Inward,AlphaStrategy.Capacitiesλ2Adaptive),
        // (DirectionStrategy.Voronoi, AlphaStrategy.)
    };

    /// <param name="maxStrategyCombinations">18</param>
    /// <param name="p_currents">使用当前位置生成Voronoi</param>
    public static (List<Vector<double>>, List<(DirectionStrategy dir, AlphaStrategy alpha)>) GenerateDiverseTopologies(
    RectfGeo area,
    PointGeo2D[] p_centers,
    PointGeo2D[] p_currents,
    PointGeo2D[] users,
    HeuristicParams heuristicParams,
    double[] radii,
    ConvexHullF[] forbiddenHulls,
    Random random,
    int? maxStrategyCombinations = null, // 若为 null，则全组合；若指定，则最多取 N 种
    int variantsPerStrategy = 3,
    ILogger? logger = null) {


        // 生成笛卡尔积：所有 (方向, alpha) 组合
        var allCombinations = (
            from dir in allDirections
            from alpha in allAlphas
            select (dir, alpha)
        ).ToList();



        var validCombinations = allCombinations
            .Where(pair => !blacklisted.Contains(pair))
            .ToList();

        // 支持限制最大策略数（用于控制计算量）
        if (maxStrategyCombinations.HasValue) {
            validCombinations = validCombinations
                .Take(maxStrategyCombinations.Value)
                .ToList();
        }


        var results = new List<Vector<double>>();
        var combinations = new List<(DirectionStrategy dir, AlphaStrategy alpha)>();

        foreach (var (dirStrat, alphaStrat) in validCombinations) {
            for (int v = 0; v < variantsPerStrategy; v++) {
                var candidate = GenerateCandidateTopology(
                    p_centers, p_currents, heuristicParams, radii, dirStrat, alphaStrat, random, forbiddenHulls, [], logger: logger);
#if DEBUG
                if (results.Count > 0) {
                    Debug.Assert((results[^1] - candidate).All(v => v == 0) == false);
                }
#endif
                results.Add(candidate);
                combinations.Add((dirStrat, alphaStrat));
            }
        }

        return (results, combinations);
    }

    public sealed record HeuristicParams(double[] Degrees, double[] Capacities, double λ2);

    public static (List<Vector<double>>, List<(DirectionStrategy dir, AlphaStrategy alpha)>) GenerateDiversePopulation(
    RectfGeo area,
    PointGeo2D[] p_centers,
    PointGeo2D[] p_currents,
    PointGeo2D[] users,
    HeuristicParams heuristicParams,
    double radiusDegree,
     ConvexHullF[] forbiddenHulls,
    Random random,
    int populationSize,
    int variantsPerStrategy = 3,
    ILogger? logger = null) {
        int NS = allAlphas.Length * allDirections.Length;
        int N_heuristic = NS - blacklisted.Count;
        ArgumentOutOfRangeException.ThrowIfLessThan(populationSize, N_heuristic, nameof(populationSize));
        logger?.LogTrace("populationSize {}, heuristic size {},variantsPerStrategy size:{}, random size {}, AlphaStrategy size: {}, DirectionStrategy size: {}, blacklist size: {}",
            populationSize, N_heuristic, variantsPerStrategy, populationSize - N_heuristic * variantsPerStrategy,
            allAlphas.Length, allDirections.Length, blacklisted.Count);

        List<Vector<double>> pop1 = [];
        PointGeo2D[] points;
        for (int i = 0; i < populationSize - N_heuristic * variantsPerStrategy; ++i) {
            // 移除重复的hull，只log一次
            (points, forbiddenHulls) = InitializePointGeo2Ds(p_centers, random, radiusDegree, forbiddenHulls, logger);
            pop1.Add(DeployOptimShared.Point2DsEncode(points));
        }
        var (pop2, allCombinations) = GenerateDiverseTopologies(area, p_centers, p_currents, users, heuristicParams, Enumerable.Repeat(radiusDegree, p_centers.Length).ToArray(), forbiddenHulls, random, variantsPerStrategy: variantsPerStrategy, logger: logger);

        Debug.Assert(pop1.Count + pop2.Count == populationSize);

        return ([.. pop1, .. pop2], [.. Enumerable.Repeat((DirectionStrategy.Random, AlphaStrategy.Random), pop1.Count), .. allCombinations]);
    }

}

internal static class EllipseRayIntersection {
    /// <summary>
    /// 计算从 origin 出发、方向为 theta（弧度）的射线与轴对齐椭圆的交点。
    /// 椭圆方程: ((x - cx)/a)^2 + ((y - cy)/b)^2 = 1
    /// </summary>
    /// <param name="ellipseCenter">椭圆中心</param>
    /// <param name="a">椭圆 x 半轴（必须 > 0）</param>
    /// <param name="b">椭圆 y 半轴（必须 > 0）</param>
    /// <param name="origin">射线起点</param>
    /// <param name="theta">射线方向角（弧度，从 x 轴正方向逆时针）</param>
    /// <param name="tolerance">数值容差，默认 1e-12</param>
    /// <returns>交点列表（按 t 从小到大排序），只包含 t >= 0 的点</returns>
    public static List<Point2D> IntersectRayEllipse(
        Point2D ellipseCenter,
        double a,
        double b,
        Point2D origin,
        Angle theta,
        double tolerance = 1e-12) {
        if (a <= 0 || b <= 0)
            throw new ArgumentException("Ellipse semi-axes a and b must be positive.");

        // 射线方向向量（单位向量）
        double dx = Math.Cos(theta.Radians);
        double dy = Math.Sin(theta.Radians);

        // 相对坐标（将椭圆移到原点）
        double dx0 = origin.X - ellipseCenter.X;
        double dy0 = origin.Y - ellipseCenter.Y;

        // 二次方程 At^2 + Bt + C = 0 的系数
        double A = (dx * dx) / (a * a) + (dy * dy) / (b * b);
        double B = 2.0 * (dx0 * dx / (a * a) + dy0 * dy / (b * b));
        double C = (dx0 * dx0) / (a * a) + (dy0 * dy0) / (b * b) - 1.0;

        var roots = SolveQuadraticRobust(A, B, C, tolerance);

        var intersections = new List<(double t, Point2D point)>(); // 临时存 t 和点

        foreach (double t in roots) {
            if (t >= -tolerance) // 接受 t ≈ 0（如 -1e-15 视为 0）
            {
                double x = origin.X + t * dx;
                double y = origin.Y + t * dy;
                // 存 t 用于排序
                intersections.Add((t, new Point2D(x, y)));
            }
        }

        // 按 t 升序
        intersections.Sort((x, y) => x.t.CompareTo(y.t));

        // 提取 Point2D
        return intersections.Select(item => item.point).ToList();
    }

    /// <summary>
    /// 数值稳定地求解 At^2 + Bt + C = 0 的实根
    /// </summary>
    private static List<double> SolveQuadraticRobust(double A, double B, double C, double eps) {
        var roots = new List<double>();

        if (Math.Abs(A) < eps) {
            // 退化为线性：B*t + C = 0
            if (Math.Abs(B) < eps) {
                return roots; // 无解或无穷解（忽略）
            }
            roots.Add(-C / B);
            return roots;
        }

        double discriminant = B * B - 4 * A * C;

        if (discriminant < -eps) {
            return roots; // 无实根
        }

        if (discriminant < 0)
            discriminant = 0; // 视为重根

        double sqrtDisc = Math.Sqrt(discriminant);

        // Numerically stable quadratic formula
        double q;
        if (B >= 0) {
            q = -0.5 * (B + sqrtDisc);
        } else {
            q = -0.5 * (B - sqrtDisc);
        }

        double t1 = q / A;
        double t2 = (Math.Abs(q) > eps) ? C / q : double.NaN;

        roots.Add(t1);
        if (!double.IsNaN(t2) && Math.Abs(t1 - t2) > eps) {
            roots.Add(t2);
        }

        return roots;
    }

    /// <summary>
    /// 椭圆方程值：((x - cx)/a)^2 + ((y - cy)/b)^2，> 1 表示点在椭圆外。
    /// </summary>
    public static double EllipseValue(Point2D point, Point2D center, double a, double b) {
        double dx = (point.X - center.X) / a;
        double dy = (point.Y - center.Y) / b;
        return dx * dx + dy * dy;
    }

    /// <summary>
    /// 判断点是否在椭圆外（边界视为椭圆内）。
    /// </summary>
    public static bool IsPointOutsideEllipse(Point2D point, Point2D center, double a, double b, double tolerance = 1e-12) {
        return EllipseValue(point, center, a, b) > 1 + tolerance;
    }

    /// <summary>
    /// 计算椭圆上距离给定点最近的点（用于当前点位于椭圆外时的退化处理）。
    /// 椭圆: ((x - cx)/a)^2 + ((y - cy)/b)^2 = 1
    /// </summary>
    public static Point2D NearestPointOnEllipse(Point2D point, Point2D center, double a, double b) {
        double dx = point.X - center.X;
        double dy = point.Y - center.Y;

        if (Math.Abs(a - b) < 1e-12) {
            // 圆的情况：最近点在圆心连线上
            double r = Math.Sqrt(dx * dx + dy * dy);
            if (r < 1e-15) return new Point2D(center.X + a, center.Y);
            return new Point2D(center.X + a * dx / r, center.Y + a * dy / r);
        }

        // 参数化 Q(θ) = (cx + a·cosθ, cy + b·sinθ)，用牛顿法极小化 |P - Q(θ)|²
        // F(θ) = a·dx·sinθ - b·dy·cosθ + (b² - a²)·sinθ·cosθ = 0
        // F'(θ) = a·dx·cosθ + b·dy·sinθ + (b² - a²)·(cos²θ - sin²θ)
        static double F(double th, double dx, double dy, double a, double b) {
            double st = Math.Sin(th), ct = Math.Cos(th);
            return a * dx * st - b * dy * ct + (b * b - a * a) * st * ct;
        }
        static double FP(double th, double dx, double dy, double a, double b) {
            double st = Math.Sin(th), ct = Math.Cos(th);
            return a * dx * ct + b * dy * st + (b * b - a * a) * (ct * ct - st * st);
        }

        double bestDistSq = double.PositiveInfinity;
        double bestTheta = 0;
        double baseGuess = Math.Atan2(dy / b, dx / a);
        double[] guesses = { baseGuess, baseGuess + Math.PI, 0, Math.PI / 2, Math.PI, -Math.PI / 2 };
        foreach (double guess in guesses) {
            double th = guess;
            for (int it = 0; it < 30; it++) {
                double fp = FP(th, dx, dy, a, b);
                if (Math.Abs(fp) < 1e-15) break;
                double step = F(th, dx, dy, a, b) / fp;
                th -= step;
                if (Math.Abs(step) < 1e-14) break;
            }
            double qx = center.X + a * Math.Cos(th);
            double qy = center.Y + b * Math.Sin(th);
            double distSq = (qx - point.X) * (qx - point.X) + (qy - point.Y) * (qy - point.Y);
            if (distSq < bestDistSq) {
                bestDistSq = distSq;
                bestTheta = th;
            }
        }

        return new Point2D(center.X + a * Math.Cos(bestTheta), center.Y + b * Math.Sin(bestTheta));
    }
}