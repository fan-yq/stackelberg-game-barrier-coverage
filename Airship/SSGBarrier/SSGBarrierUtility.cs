using System.Collections.Concurrent;
using System.Diagnostics;
using Aircraft.Core.ConvexHull;
using MathNet.Numerics.Integration;
using MathNet.Spatial.Euclidean;

namespace Airship.Deployment.SSGBarrier;

public sealed class SSGBarrierUtility(ISSGBarrierEnv gameEnv, SSGBarrierSetting setting) {
    readonly LineSegCircleGeometry geometry = new();
    readonly Dictionary<PointGeo2D, double> SinkValues = gameEnv.SinkValues();
    readonly Dictionary<PointGeo2D, int> SinkIndices = gameEnv.SinkIndices();

    public (double Ua, double Ud) U(AirshipMixedStrategy strategy, AttackerStrategy attackerStrategy) {
        var path = attackerStrategy.Paths;
        var (S_sum, d) = ProbOfAttack(strategy, attackerStrategy);
        double dmin = PointGeo2D.SphereAngle(attackerStrategy.Paths[0], attackerStrategy.Paths[^1]);
        Debug.Assert(d >= dmin);

        double Ua = SinkValues[path[^1]] * S_sum - setting.LambdaPath * (d - dmin) / dmin;
        return (Ua, -Ua);
    }

    public (double S_sum, double d) ProbOfAttack(AirshipMixedStrategy strategy, AttackerStrategy attackerStrategy) {
        var path = attackerStrategy.Paths;
        // 计算距离，如果大于d max 则直接返回0
        double d = 0;
        for (int i = 0; i < path.Length - 1; ++i) {
            d += PointGeo2D.SphereAngle(path[i], path[i + 1]);
        }

        if (d > setting.dMaxAttacker) {
            return (0, d);
        }


        var lineSeg = attackerStrategy.Lines();

        // integrate prob
        double S_sum_ln = lineSeg.Sum(line => CaculateLineSegCircle(strategy, line, attackerStrategy.Paths[^1]));
        double Psurv = Exp(setting.Kappa * S_sum_ln);
        // double P_CDP = 1 - S_sum; // 在整条路径上至少被发现并打击一次的概率
        Debug.Assert(0 <= Psurv && Psurv <= 1);
        return (Psurv, d);
    }

    /// <summary>
    /// 多个导弹打击同一个目标不满足可加性，1-(1-pi)(1-pj)
    /// </summary>
    public (double Ua, double Ud) U2(AirshipMixedStrategy airshipStrategy, AttackerStrategy2 attackerStrategy) {
        double Ua = 0;
        int M = attackerStrategy.Paths.Length;
        double[] p = Enumerable.Repeat(1.0, gameEnv.Sinks.Length).ToArray(); // 打击失败的概率
        for (int i = 0; i < M; ++i) {
            var (S_sum, d) = ProbOfAttack(airshipStrategy, new(attackerStrategy.Paths[i]));
            double sinkValue = SinkValues[attackerStrategy.Paths[i][^1]];
            p[SinkIndices[attackerStrategy.Paths[i][^1]]] *= 1 - S_sum;
        }

        for (int i = 0; i < gameEnv.Sinks.Length; ++i) {
            Ua += p[i] * gameEnv.SinkValues1[i];
        }

        return (Ua, -Ua);
    }

    /// <returns>某一段的生存概率ln</returns>
    public double CaculateLineSegCircle(AirshipMixedStrategy strategy, LineSegment2D line, PointGeo2D target) {
        // simpson integration, four points
        double lineLength = line.Length;
        int n = 2;
        n = Max(n, (int)lineLength);
        if (int.IsOddInteger(n)) { n++; }
        return CaculateLineSegCircle(strategy, line, target, n);
    }
    public double CaculateLineSegCircle(AirshipMixedStrategy strategy, LineSegment2D line, PointGeo2D target, int n) {
        // simpson integration, four points
        double lineLength = line.Length;
        double integrate_part_norm = SimpsonRule.IntegrateComposite(ds => {
            var p = line.GetPointT(ds);
            // double da = AntiMissileDistanceMin(new(p), gameEnv.AntiMissles);
            double da = PointGeo2D.SphereAngle(new(p), target);
            double prob = DetectProbSum(new(p), strategy) * setting.AntiMissleProb(da);
            return prob;
        }, 0, 1, n);
        double ln_S = -integrate_part_norm * line.Length;
        // Debug.Assert(0 <= S && S <= 1);
        return ln_S;
    }
    // 多个飞艇探测
    double DetectProbSum(PointGeo2D p, AirshipMixedStrategy strategy) {
        double p_undetected = 1;
        int L = strategy.Centers.Length;
        for (int i = 0; i < L; ++i) {
            double d = PointGeo2D.SphereAngle(p, strategy.Centers[i]);
            p_undetected *= 1 - setting.DetectProb(d) * strategy.Allocations[i];
        }
        return 1 - p_undetected;
    }

    // Model 2最大的探测概率
    // double DetectProbSum(PointGeo2D p, AirshipMixedStrategy strategy) {
    //     double dmin = strategy.Centers.Min(c => PointGeo2D.SphereAngle(p, c));
    //     return setting.DetectProb(dmin);
    // }

    // 找到最近的反导位置
    double AntiMissileDistanceMin(PointGeo2D p, PointGeo2D[] AntiMissles) {
        return AntiMissles.Min(a => PointGeo2D.SphereAngle(a, p));
    }

    /// <summary>
    /// 计算生存概率随着路径的prob变化
    /// </summary>
    /// <param name="s">[0,1] 路径的比例/param>
    /// <returns>比如s=0.6,从起点到0.6的prob</returns>
    public double ProbSurvWithS(AirshipMixedStrategy strategy, AttackerStrategy attackerStrategy, double s) {
        Debug.Assert(s >= 0 && s <= 1);
        if (s <= 0) return 1.0;
        if (s >= 1) s = 1.0;

        var path = attackerStrategy.Paths;
        var lineSeg = attackerStrategy.Lines().ToArray();

        // 段长度（过滤零长度段）
        var ds = new List<double>();
        for (int i = 0; i < lineSeg.Length; ++i) {
            var d = PointGeo2D.SphereAngle(new(lineSeg[i].StartPoint), new(lineSeg[i].EndPoint));
            if (d <= 0) continue;
            ds.Add(d);
        }
        double dsSum = ds.Sum();

        double Psurv_ln = 0;
        double acc = 0;
        for (int i = 0; i < ds.Count; ++i) {
            double frac = ds[i] / dsSum;
            double next = acc + frac;
            if (s >= next) {
                Psurv_ln += CaculateLineSegCircle(strategy, lineSeg[i], path[^1], 100);
                acc = next;
            } else {
                double t = (s - acc) / frac;
                if (t > 0) {
                    var mid = lineSeg[i].GetPointT(t);
                    Psurv_ln += CaculateLineSegCircle(strategy, new LineSegment2D(lineSeg[i].StartPoint, mid), path[^1], 100);
                }
                break;
            }
        }
        return Exp(setting.Kappa * Psurv_ln);
    }
}

