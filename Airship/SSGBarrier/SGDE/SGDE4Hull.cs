using Aircraft.Core.ConvexHull;
using Aircraft.Core.Optimization.DE;
using Airship.Deployment.Comm;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Spatial.Euclidean;
using Microsoft.Extensions.Logging;

namespace Airship.Deployment.SSGBarrier.SGDE;

public enum SGDEDefOprType {
    DE, Heuristic
}
public enum SGDEAttOprType {
    DE, Heuristic
}
public sealed record SGDE4HullSetting : SGDESetting {
    public SGDEDefOprType DefOprType { get; set; }
    public SGDEAttOprType AttOprType { get; set; }
}

public sealed class SGDE4Hull : SGDE {
    readonly ConvexHullD hullAttacker, hullDefender;
    readonly IComparer<Point2D> comparer;
    readonly SSGBarrierEnv2 env;
    public required double CR { get; init; }
    public required double F { get; init; }
    public double MutantRadius { get; init; } = 0.5;
    public double MutantPathScale { get; init; } = 1.5;
    readonly SGDE4HullSetting setting2;
    public SGDE4Hull(int PopSizeDefender, int PopSizeAtt4Def, ObjectiveFunctionGame objective, SSGBarrierEnv2 env, Random random, ILogger logger, SGDE4HullSetting setting, bool parallel_ = true)
    : base(env.N_Airship * 2, 2 + env.N_ControlPointOfAttacker * 2, PopSizeDefender, PopSizeAtt4Def, objective, random, logger, parallel_, setting) {
        this.hullDefender = env.DeployArea;
        this.hullAttacker = env.MissleArea;
        this.env = env;
        comparer = PointGeo2DComparerFactory.Create2(env.PointSortMode);
        setting2 = setting;
    }
    public SGDE4Hull(int PopSizeDefender, int PopSizeAtt4Def, ObjectiveFunctionGame objective, SSGBarrierEnv2 env, Random random, ILogger logger, bool parallel_ = true, double eps2 = 0.001, bool isCrowding = true) : this(PopSizeDefender, PopSizeAtt4Def, objective, env, random, logger, new() {
        Eps2 = eps2, IsCrowding = isCrowding, ProportionOfRandPBest = 1,
        DefOprType = SGDEDefOprType.Heuristic, AttOprType = SGDEAttOprType.Heuristic,
    }, parallel_) { }
    protected override Vector<double> InitializePopulationAtt() {
        return SSGShared.InitializePopulationAtt(dimAttacker, env, hullAttacker, random);
    }

    protected override Vector<double> InitializePopulationDef() {
        return SSGShared.InitializePopulationDef(dimDefender, env, hullDefender, comparer, random);
    }

    protected override void MutateAndCrossoverAtt(IReadOnlyList<Vector<double>> state, Vector<double>[] trialVectors) {
        int N = state.Count, dimension = state[0].Count;
        var population = state;
        for (int i = 0; i < trialVectors.Length; i++) {
            // DE/rand/1: v = x_r1 + F*(x_r2 - x_r3)
            int r = 0;
            if (setting2.AttOprType is SGDEAttOprType.Heuristic) { r = random.Next() % 2; }
            switch (r) {
                case 0:
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
                    break;
                case 1:
                    MutantAtt(trialVectors[i]);
                    break;
                case 2:
                    MutantAtt3(trialVectors[i]);
                    break;
                default:
                    goto case 0;

            }

            SSGShared.FixVectorAtt(ref trialVectors[i], hullAttacker, random, env, setting2.PointFixMode);
        }
    }

    protected override void MutateAndCrossoverDef(IReadOnlyList<Vector<double>> state, Vector<double>[] trialVectors) {
        int N = state.Count, dimension = state[0].Count;
        var population = state;
        for (int i = 0; i < trialVectors.Length; i++) {
            switch (setting2.DefOprType) {
                case SGDEDefOprType.DE:
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
                    break;
                case SGDEDefOprType.Heuristic:
                    trialVectors[i] = MutantDef(state[i]);
                    break;
            }

            SSGShared.FixVectorDef(ref trialVectors[i], hullDefender, comparer, random, setting2.PointFixMode);

        }
    }

    Vector<double> MutantDef(Vector<double> v) {
        var strategy2 = AirshipStrategy2.Decode2(v, env);
        int direction = random.Next() % 3;
        PopulationGenerator.DirectionStrategy directionStrategy = direction switch {
            0 => PopulationGenerator.DirectionStrategy.Inward,
            1 => PopulationGenerator.DirectionStrategy.Outward,
            _ => PopulationGenerator.DirectionStrategy.Single,
        };
        PopulationGenerator.AlphaStrategy alphaStrategy = PopulationGenerator.AlphaStrategy.Random;
        var newV = PopulationGenerator.GenerateCandidateTopology(strategy2.Deployments, strategy2.Deployments, new([], [], 0), Enumerable.Repeat(MutantRadius, strategy2.Deployments.Length).ToArray(), directionStrategy, alphaStrategy, random, [], []);
        return newV;
    }




    // rand scale
    void MutantAtt(Vector<double> trialVector) {
        AttackerStrategy attackerStrategy = AttackerStrategy.Decode2(trialVector, env);
        var path = attackerStrategy.Paths.Select(v => v.ToPoint2D()).ToArray();
        LineSegment2D line = new(path[0], path[^1]);
        for (int i = 1; i < path.Length - 1; ++i) {
            var (p1, v) = SplitPoint(line, path[i]);
            path[i] = p1 + random.NextDouble() * MutantPathScale * v;
        }
        path = path[1..^1];
        SSGShared.Encode(trialVector, path, 2);
    }
    // same scale
    void MutantAtt2(Vector<double> trialVector) {
        AttackerStrategy attackerStrategy = AttackerStrategy.Decode2(trialVector, env);
        var path = attackerStrategy.Paths.Select(v => v.ToPoint2D()).ToArray();
        LineSegment2D line = new(path[0], path[^1]);
        double r = random.NextDouble();
        for (int i = 1; i < path.Length - 1; ++i) {
            var (p1, v) = SplitPoint(line, path[i]);
            path[i] = p1 + r * MutantPathScale * v;
        }
        path = path[1..^1];
        SSGShared.Encode(trialVector, path, 2);
    }
    // select one
    void MutantAtt3(Vector<double> trialVector) {
        AttackerStrategy attackerStrategy = AttackerStrategy.Decode2(trialVector, env);
        var path = attackerStrategy.Paths.Select(v => v.ToPoint2D()).ToArray();
        LineSegment2D line = new(path[0], path[^1]);
        double r = (random.NextDouble() - 0.5) * 2;
        int i = random.Next() % env.N_ControlPointOfAttacker + 1;
        var (p1, v) = SplitPoint(line, path[i]);
        path[i] = p1 + r * MutantPathScale * v;
        path = path[1..^1];
        SSGShared.Encode(trialVector, path, 2);
    }

    static (Point2D, Vector2D) SplitPoint(LineSegment2D line, Point2D p) {
        double t = line.GetParameterT(p);
        var p1 = line.GetPointT(t);
        return (p1, p - p1);
    }
}