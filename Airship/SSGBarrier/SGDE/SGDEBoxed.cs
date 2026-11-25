using Aircraft.Core.Optimization.DE;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Random;
using Microsoft.Extensions.Logging;

namespace Airship.Deployment.SSGBarrier.SGDE;

public enum SGDEBoxedType {
    DERand, DEClamp, Reflection
}

public sealed record SGDEBoxedSetting : SGDESetting {
    public SGDEBoxedType OprType { get; set; }
}

// copy from ACGABoxed
public sealed class SGDEBoxed : SGDE {
    readonly Vector<double> lowerAttacker, upperAttacker, lowerDefender, upperDefender;
    readonly SGDEBoxedSetting setting2;
    public SGDEBoxed(int dimDefender, int dimAttacker, int PopSizeDefender, int PopSizeAttacker4EachDef, ObjectiveFunctionGame objective, double[] lowerDefender, double[] upperDefender, double[] lowerAttacker, double[] upperAttacker, Random random, ILogger logger, bool parallel_, SGDEBoxedSetting setting) : base(dimDefender, dimAttacker, PopSizeDefender, PopSizeAttacker4EachDef, objective, random, logger, parallel_, setting) {
        var V = Vector<double>.Build;
        this.lowerAttacker = V.Dense(lowerAttacker);
        this.upperAttacker = V.Dense(upperAttacker);
        this.lowerDefender = V.Dense(lowerDefender);
        this.upperDefender = V.Dense(upperDefender);
        this.setting2 = setting;
    }

    public double CR { get; init; }
    public double F { get; init; }

    void MutateAndCrossover(IReadOnlyList<Vector<double>> state, Vector<double>[] trialVectors, Vector<double> l, Vector<double> u) {
        int N = state.Count, dimension = state[0].Count;
        var population = state;
        for (int i = 0; i < trialVectors.Length; i++) {
            // DE/rand/1: v = x_r1 + F*(x_r2 - x_r3)
            int r1 = DEShared.RandNotIn(N, [], random);
            int r2 = DEShared.RandNotIn(N, [r1], random);
            int r3 = DEShared.RandNotIn(N, [r1, r2], random);
            var mutant = population[r1] + F * (population[r2] - population[r3]);

            switch (setting2.OprType) {
                case SGDEBoxedType.DERand:
                    DEShared.ClampVectorRand(ref mutant, l, u, random);
                    break;
                case SGDEBoxedType.DEClamp:
                    DEShared.ClampVector(ref mutant, l, u);
                    break;
                case SGDEBoxedType.Reflection:
                    DEShared.ClampVectorReflection(ref mutant, l, u);
                    break;
                default:
                    goto case SGDEBoxedType.DERand;
            }


            int randIndex = random.Next(dimension);
            for (int j = 0; j < dimension; j++) {
                if (random.NextDouble() < CR || j == randIndex)
                    trialVectors[i][j] = mutant[j];
                else
                    trialVectors[i][j] = state[i][j];
            }
        }
    }
    protected override void MutateAndCrossoverAtt(IReadOnlyList<Vector<double>> pop, Vector<double>[] trialVectors) {
        MutateAndCrossover(pop, trialVectors, lowerAttacker, upperAttacker);
    }

    protected override void MutateAndCrossoverDef(IReadOnlyList<Vector<double>> pop, Vector<double>[] trialVectors) {
        MutateAndCrossover(pop, trialVectors, lowerDefender, upperDefender);
    }

    protected override Vector<double> InitializePopulationAtt() {
        var pop = Vector<double>.Build.Dense(random.NextDoubles(dimAttacker)).PointwiseMultiply(upperAttacker - lowerAttacker) + lowerAttacker;
        return pop;
    }

    protected override Vector<double> InitializePopulationDef() {
        var pop = Vector<double>.Build.Dense(random.NextDoubles(dimDefender)).PointwiseMultiply(upperDefender - lowerDefender) + lowerDefender;
        return pop;
    }
}