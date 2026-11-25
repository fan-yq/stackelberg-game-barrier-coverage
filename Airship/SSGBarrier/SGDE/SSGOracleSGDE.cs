using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;

namespace Airship.Deployment.SSGBarrier.SGDE;

public sealed class SSGOracleSGDE {
    SGDE4Hull SGDE4Hull;
    SSGBarrierEnv2 env;
    SSGBarrierSetting setting;
    public SSGOracleSGDE(int PopSizeDefender, int PopSizeAttacker, Random random, SSGBarrierEnv2 env, SSGBarrierSetting setting, ILoggerFactory loggerFactory, SGDE4HullSetting setting1) {
        int N_PopAttacker = env.N_Airship + 1;
        this.env = env;
        this.setting = setting;
        var utility = BuildUtility(env, setting);
        bool parallel_ = true;
#if DEBUG && DETAIL
        parallel_ = false;
#endif
        SGDE4Hull = new(PopSizeDefender, PopSizeAttacker, utility, env, random, loggerFactory.CreateLogger<SGDE4Hull>(), setting1, parallel_) {
            F = 0.5,
            CR = 0.9
        };
    }

    public SSGResult2 Resolve(int iterations) {
        var res = SGDE4Hull.Optimize(iterations);

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
    // only for test

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