using System.Collections.Concurrent;
using System.Text.Json;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;

namespace Airship.Deployment.SSGBarrier;

// 互相注入种群，计算相应的maxmin值
public sealed class SSGCrossCompare {

    public static SSGResultG<SSGBarrierEnv2>[] LoadData2(string[] files) {
        // load
        List<SSGResultG<SSGBarrierEnv2>> res = new(files.Length);
        foreach (var file in files) {
            var r = JsonSerializer.Deserialize<SSGResultG<SSGBarrierEnv2>>(File.ReadAllBytes(file));
            if (r is not null) {
                res.Add(r);
            }
        }
        return res.ToArray();
    }
    /// <summary>
    /// 先选出eps-best的defender，然后互相比较table元素ij, i-> file i Defender, j -> file j Attacker
    /// </summary>
    public static async Task<SSGCrossCompareResult> Evaluate(string[] files, bool OnlyBestDefender, double eps) {
        var res = LoadData2(files);
        var setting = res[0].Setting;
        var env = res[0].Env;
        SSGBarrierUtility utility = new(env, setting);
        int N = files.Length;
        var mmm = Matrix<double>.Build.Dense(N, N);


        await Parallel.ForAsync(0, N, async (i, token) => {
            for (int j = 0; j < N; ++j) {
                var defenders = res[i].AirshipStrategies;
                if (OnlyBestDefender) {
                    var bestDefIndices = ACGAResult.EpsBestDefenderIndices(eps, Matrix<double>.Build.DenseOfRowArrays(res[i].UMatrix));
                    defenders = bestDefIndices.Select(k => defenders[k]).ToArray();
                }
                var m = await CaculateMaxMinUd(defenders, res[j].AttackerStrategies, utility);
                mmm[i, j] = m;
            }
        });

        return new() {
            Files = files,
            CaseName = res[0].CaseName,
            MaxMinUs = mmm.ToRowArrays(),
            OnlyBest = OnlyBestDefender,
            Eps = eps
        };
    }

    static async Task<double> CaculateMaxMinUd(AirshipStrategy2[] popDefender, AttackerStrategy[] popAttacker, SSGBarrierUtility utility) {
        var umatrix = Matrix<double>.Build.Dense(popDefender.Length, popAttacker.Length);
        ValueTask f(int i, CancellationToken token) {
            for (int j = 0; j < popAttacker.Length; ++j) {
                AttackerStrategy attackerStrategy = popAttacker[j];
                AirshipStrategy2 airshipStrategy = popDefender[i];
                var (ua, ud) = utility.U(airshipStrategy.ToMixdStrategy(), attackerStrategy);
                umatrix[i, j] = ud;
            }
            return ValueTask.CompletedTask;

        }
        await Parallel.ForAsync(0, popDefender.Length, f);

        double maxMinU = ACGAResult.MaxMinU(umatrix, Enumerable.Range(0, popDefender.Length).ToList());
        return maxMinU;
    }

    // 第二种比较方法: fix eps-best defender, evaluted the optimal attacker, compare max min U.
    // mode1: selectEpsBest=true, eps-best defenders has good result
    // mode2: selectEpsBest=false, best defender is good
    // public static IEnumerable<FixedEpsBestDefenderResult> ComparedWithFixedEpsBestDefender(string[] files, int iterations, double eps, int randomSeed, ILoggerFactory factory, bool selectEpsBest = true) {
    //     foreach (var file in files) {
    //         var r = LoadData2([file])[0];
    //         var setting = r.Setting;
    //         var env = r.Env;

    //         int PopSizeAttacker = 20 * (env.N_Airship + 1);
    //         var bestDefIndices = ACGAResult.EpsBestDefenderIndices(eps, Matrix<double>.Build.DenseOfRowArrays(r.UMatrix));
    //         var defenders = bestDefIndices.Select(k => r.AirshipStrategies[k]).ToArray();
    //         SSGPredefinedDeployment sSGPredefinedDeployment = new(PopSizeAttacker, PointFixMode.Proj, new(randomSeed), env, setting, factory);

    //         ConcurrentBag<double> maxMinUd = [];

    //         foreach (var defender in defenders) {
    //             // Parallel.ForEach(defenders, defender => {
    //             SSGResult2 optimalAtt = sSGPredefinedDeployment.Predefined(iterations, defender.Deployments);
    //             maxMinUd.Add(optimalAtt.MaxMinU);
    //             // });
    //         }

    //         yield return new(file, r.MaxMinU, selectEpsBest ? maxMinUd.Max() : maxMinUd.Min());
    //     }
    // }

    public sealed record FixedEpsBestDefenderResult(string File, double MaxMinUd, double FixedEpsBestMinUd);
}

public sealed record SSGCrossCompareResult {
    public required string[] Files { get; set; }
    public string? CaseName { get; set; }
    public required double[][] MaxMinUs { get; set; } // [defender-index][attacker-index]
    public bool OnlyBest { get; set; }
    public double Eps { get; set; }
    public DateTime CreateAt { get; set; } = DateTime.Now;
}