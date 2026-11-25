using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;

namespace Airship.Deployment.SSGBarrier;

public sealed class SSGOracleACDE {
    ACDE4Hull aCGA4Hull;
    SSGBarrierEnv2 env;
    SSGBarrierSetting setting;
    public SSGOracleACDE(int PopSizeDefender, int PopSizeAttacker, Random random, SSGBarrierEnv2 env, SSGBarrierSetting setting, ACGAConfig setting1, ILoggerFactory loggerFactory, double eps2) {
        int N_PopAttacker = env.N_Airship + 1;
        this.env = env;
        this.setting = setting;
        var utility = BuildUtility(env, setting);
        bool parallel_ = true;
#if DEBUG && DETAIL
        parallel_ = false;
#endif
        aCGA4Hull = new(setting1, PopSizeDefender, PopSizeAttacker, utility, env, random, loggerFactory.CreateLogger<ACDE4Hull>(), parallel_, eps2) {
            F = 0.5,
            CR = 0.9
        };
    }

    public SSGResult2 Resolve(int iterations, ACGA.ACGAPopulation? population = null) {
        if (population is not null) {
            aCGA4Hull.SetPopulation(population);
        }
        var res = aCGA4Hull.Optimize(iterations);

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
    public ACGA.ACGAPopulation GetPopulation() => aCGA4Hull.GetPopulation();

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