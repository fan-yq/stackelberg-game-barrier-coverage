using Aircraft.Core.ConvexHull;
using Aircraft.Core.Optimization.DE;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Spatial.Euclidean;
using Microsoft.Extensions.Logging;

namespace Airship.Deployment.SSGBarrier;

// 有两个重要调整，对defender进行排序
// 根据convexhull检查范围
public sealed class ACDE4Hull : ACGA {
    readonly ConvexHullD hullAttacker, hullDefender;
    readonly IComparer<Point2D> comparer;
    readonly SSGBarrierEnv2 env;
    public required double CR { get; init; }
    public required double F { get; init; }
    readonly PointFixMode mode;
    public ACDE4Hull(ACGAConfig config, int PopSizeDefender, int PopSizeAttacker, ObjectiveFunctionGame objective, SSGBarrierEnv2 env, Random random, ILogger logger, bool parallel_ = true, double eps2 = 0.001)
    : base(config, env.N_Airship * 2, 2 + env.N_ControlPointOfAttacker * 2, PopSizeDefender, PopSizeAttacker, objective, random, logger, parallel_, eps2: eps2) {
        this.hullDefender = env.DeployArea;
        this.hullAttacker = env.MissleArea;
        this.env = env;
        this.mode = config.PointFixMode;
        comparer = PointGeo2DComparerFactory.Create2(env.PointSortMode);
    }

    // sort
    protected override void MutateAndCrossoverDef(IReadOnlyList<Vector<double>> state, Vector<double>[] trialVectors) {
        int N = state.Count, dimension = state[0].Count;
        var population = state;
        for (int i = 0; i < trialVectors.Length; i++) {
            // DE/rand/1: v = x_r1 + F*(x_r2 - x_r3)
            int r1 = DEShared.RandNotIn(N, [], random);
            int r2 = DEShared.RandNotIn(N, [r1], random);
            int r3 = DEShared.RandNotIn(N, [r1, r2], random);
            var mutant = population[r1] + F * (population[r2] - population[r3]);

            int randIndex = random.Next(dimension);
            for (int j = 0; j < dimension; j++) {
                if (random.NextDouble() < CR || j == randIndex)
                    trialVectors[i][j] = mutant[j];
                else
                    trialVectors[i][j] = state[i][j];
            }
            SSGShared.FixVectorDef(ref trialVectors[i], hullDefender, comparer, random, mode);
        }
    }

    protected override void MutateAndCrossoverAtt(IReadOnlyList<Vector<double>> state, Vector<double>[] trialVectors) {
        int N = state.Count, dimension = state[0].Count;
        var population = state;
        for (int i = 0; i < trialVectors.Length; i++) {
            // DE/rand/1: v = x_r1 + F*(x_r2 - x_r3)
            int r1 = DEShared.RandNotIn(N, [], random);
            int r2 = DEShared.RandNotIn(N, [r1], random);
            int r3 = DEShared.RandNotIn(N, [r1, r2], random);
            var mutant = population[r1] + F * (population[r2] - population[r3]);

            // int r4 = DEShared.RandNotIn(N, [r1, r2, r3], random);
            // int r5 = DEShared.RandNotIn(N, [r1, r2, r3, r4], random);
            // var mutant = population[r1] + F * (population[r2] - population[r3]) + F * (population[r4] - population[r5]);

            int randIndex = random.Next(dimension);
            for (int j = 0; j < dimension; j++) {
                if (random.NextDouble() < CR || j == randIndex)
                    trialVectors[i][j] = mutant[j];
                else
                    trialVectors[i][j] = state[i][j];
            }
            SSGShared.FixVectorAtt(ref trialVectors[i], hullAttacker, random, env, mode);

        }
    }

    internal static double Fix01(double index, int length) {
        double N = index, M = length;
        return ((N % M) + M) % M;
    }


    protected override Vector<double> InitializePopulationAtt() {
        return SSGShared.InitializePopulationAtt(dimAttacker, env, hullAttacker, random);
    }

    protected override Vector<double> InitializePopulationDef() {
        return SSGShared.InitializePopulationDef(dimDefender, env, hullDefender, comparer, random);
    }
}