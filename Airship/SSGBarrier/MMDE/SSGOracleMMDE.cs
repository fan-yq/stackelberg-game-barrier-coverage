using System.Diagnostics;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;

namespace Airship.Deployment.SSGBarrier.MMDE;

public sealed class SSGOracleMMDE {
    MMDE4Hull aCGA4Hull;
    SSGBarrierEnv2 env;
    SSGBarrierSetting setting;
    ObjectiveFunctionGame utility;
    bool parallel_;
    public SSGOracleMMDE(Random random, SSGBarrierEnv2 env, SSGBarrierSetting setting, PointFixMode mode, ILoggerFactory loggerFactory) {
        int N_PopAttacker = env.N_Airship + 1;
        this.env = env;
        this.setting = setting;
        utility = BuildUtility(env, setting);
        parallel_ = true;
#if DEBUG && DETAIL
        parallel_ = false;
#endif
        int N = Max(300, 100 * env.N_Airship);
        aCGA4Hull = new(N, 2 * N, Max((int)(0.1 * N), 1), utility, env, random, loggerFactory.CreateLogger<MMDE4Hull>(), parallel_) {
            F = SSGConfig.F,
            CR = SSGConfig.R,
            PointFixMode = mode,
        };
    }

    public SSGResult2 Resolve(int iterations) {
        var res1 = aCGA4Hull.Optimize(iterations);
        var res = MMDE.MMDEPopToACGAResult(res1, utility, parallel_);

        return new() {
            AttackerStrategies = res.PopAttacker.Select(p => AttackerStrategy.Decode2(p, env)).ToArray(),
            AirshipMixedStrategy = AirshipStrategy2.Decode2(res.PopDefender[0], env).ToMixdStrategy(),
            AirshipStrategies = res.PopDefender.Select(p => AirshipStrategy2.Decode2(p, env)).ToArray(),
            Env = env,
            Setting = setting,
            MaxMinU = res.MaxMinU(),
            MinMaxU = res.MinMaxU(),
            UMatrix = res.UMatrix.ToRowArrays()
        };
    }

    ObjectiveFunctionGame BuildUtility(SSGBarrierEnv2 env, SSGBarrierSetting setting) {
        SSGBarrierUtility utility = new(env, setting);
        double objective(Vector<double> vdef, Vector<double> vatt) {
            AttackerStrategy attackerStrategy = AttackerStrategy.Decode2(vatt, env);
            AirshipStrategy2 airshipStrategy = AirshipStrategy2.Decode2(vdef, env);
            var (ua, ud) = utility.U(airshipStrategy.ToMixdStrategy(), attackerStrategy);
            return ud;
        }

        return objective;
    }
}