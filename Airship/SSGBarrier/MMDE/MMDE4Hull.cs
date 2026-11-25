using Aircraft.Core.ConvexHull;
using Aircraft.Core.Optimization.DE;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Spatial.Euclidean;
using Microsoft.Extensions.Logging;

namespace Airship.Deployment.SSGBarrier.MMDE;

public sealed class MMDE4Hull : MMDE {
    readonly ConvexHullD hullAttacker, hullDefender;
    readonly IComparer<Point2D> comparer;
    readonly SSGBarrierEnv2 env;
    public required double CR { get; init; }
    public required double F { get; init; }
    public required PointFixMode PointFixMode { get; init; }
    public MMDE4Hull(int N, int Ks, int T, ObjectiveFunctionGame objective, SSGBarrierEnv2 env, Random random, ILogger logger, bool parallel_ = true)
    : base(env.N_Airship * 2, 2 + env.N_ControlPointOfAttacker * 2, N, Ks, T, objective, random, logger, parallel_) {
        this.hullDefender = env.DeployArea;
        this.hullAttacker = env.MissleArea;
        this.env = env;
        comparer = PointGeo2DComparerFactory.Create2(env.PointSortMode);
    }
    protected override Vector<double> InitializePopulationAtt() {
        return SSGShared.InitializePopulationAtt(dimAttacker, env, hullAttacker, random);
    }

    protected override Vector<double> InitializePopulationDef() {
        return SSGShared.InitializePopulationDef(dimDefender, env, hullDefender, comparer, random);
    }

    protected override Vector<double> MutateAndCrossoverAtt(SortedList<double, MMDEPop> state) {
        int N = state.Count, dimension = dimAttacker;
        var population = state;
        // DE/rand/1: v = x_r1 + F*(x_r2 - x_r3)
        int r1 = DEShared.RandNotIn(N, [], random);
        int r2 = DEShared.RandNotIn(N, [r1], random);
        int r3 = DEShared.RandNotIn(N, [r1, r2], random);
        var mutant = population.ElementAt(r1).Value.Attacker + F * (population.ElementAt(r2).Value.Attacker - population.ElementAt(r3).Value.Attacker);

        int randIndex = random.Next(dimension);
        for (int j = 0; j < dimension; j++) {
            if (random.NextDouble() < CR || j == randIndex)
                mutant[j] = mutant[j];
            else
                mutant[j] = population.ElementAt(j).Value.Attacker[j];
        }
        SSGShared.FixVectorAtt(ref mutant, hullAttacker, random, env, PointFixMode);
        return mutant;
    }

    protected override void MutateAndCrossoverDef(SortedList<double, MMDEPop> state, Vector<double>[] trialVectors) {
        int N = state.Count, dimension = dimDefender;
        var population = state;
        for (int i = 0; i < trialVectors.Length; i++) {
            // DE/rand/1: v = x_r1 + F*(x_r2 - x_r3)
            int r1 = DEShared.RandNotIn(N, [], random);
            int r2 = DEShared.RandNotIn(N, [r1], random);
            int r3 = DEShared.RandNotIn(N, [r1, r2], random);
            var mutant = population.ElementAt(r1).Value.Defender + F * (population.ElementAt(r2).Value.Defender - population.ElementAt(r3).Value.Defender);

            int randIndex = random.Next(dimension);
            for (int j = 0; j < dimension; j++) {
                if (random.NextDouble() < CR || j == randIndex)
                    trialVectors[i][j] = mutant[j];
                else
                    trialVectors[i][j] = population.ElementAt(i).Value.Defender[j];
            }
            SSGShared.FixVectorDef(ref trialVectors[i], hullDefender, comparer, random, PointFixMode);
        }
    }
}