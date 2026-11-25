using System.Diagnostics;
using System.Text.Json;
using Aircraft.Core.ConvexHull;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Spatial.Euclidean;

namespace Airship.Deployment.SSGBarrier;

public enum PointFixMode {
    Resample, Proj
}
static class SSGShared {
    internal static double Fix01(double index, int length) {
        double N = index, M = length;
        return ((N % M) + M) % M;
    }

    public static void FixPoint(ref Vector<double> mutant, ConvexHullD hull, Random random, bool isAtt, PointFixMode mode) {
        int N = mutant.Count / 2;
        for (int i = isAtt ? 1 : 0; i < N; ++i) {
            Point2D p = new(mutant[2 * i], mutant[2 * i + 1]);
            if (!hull.IsPointInHullClose(p)) {
                switch (mode) {
                    case PointFixMode.Resample:
                        p = hull.Sample(random);
                        break;
                    case PointFixMode.Proj:
                        p = hull.ProjectToHull(p);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            mutant[2 * i] = p.X;
            mutant[2 * i + 1] = p.Y;
        }
    }

    public static void FixVectorDef(ref Vector<double> trialVector, ConvexHullD hullDefender, IComparer<Point2D> comparer, Random random, PointFixMode mode) {
        FixPoint(ref trialVector, hullDefender, random, false, mode);
        var p = Decode(trialVector);
        Array.Sort(p, comparer);
        Encode(trialVector, p);
    }
    public static void FixVectorAtt(ref Vector<double> trialVector, ConvexHullD hullAttacker, Random random, SSGBarrierEnv2 env, PointFixMode mode) {
        FixPoint(ref trialVector, hullAttacker, random, true, mode);
        // fix dim 0,1
        trialVector[0] = Fix01(trialVector[0], env.Sources.Length);
        trialVector[1] = Fix01(trialVector[1], env.Sinks.Length);
        // DEShared.ClampVectorRand(ref trialVector, l, u, random, 0, 2);
        // sort
        AttackerStrategy attackerStrategy = AttackerStrategy.Decode2(trialVector, env);
        var path = attackerStrategy.Paths.Select(v => v.ToPoint2D()).ToArray();
        LineSegment2D line = new(path[0], path[^1]);
        path = path[1..^1];
        Array.Sort(path, new Point2DComparerLine(line));
        Encode(trialVector, path, 2);

#if DEBUG
        var p = Decode(trialVector);
        for (int j = 1; j < p.Length; ++j) {
            Debug.Assert(hullAttacker.IsPointInHullClose(p[j]));
            // FixVectorAtt(ref trialVector, hullAttacker, l, u, random, env);
        }
#endif
    }

    public static Point2D[] Decode(Vector<double> v) {
        int N = v.Count / 2;
        var p = new Point2D[N];
        for (int i = 0; i < N; ++i) {
            p[i] = new(v[2 * i], v[2 * i + 1]);
        }
        return p;
    }
    public static void Encode(Vector<double> v, Point2D[] p, int start = 0) {
        int N = p.Length;
        for (int i = 0; i < N; ++i) {
            v[2 * i + start] = p[i].X;
            v[2 * i + start + 1] = p[i].Y;
        }
    }

    public static Vector<double> InitializePopulationAtt(int dimAttacker, SSGBarrierEnv2 env, ConvexHullD hullAttacker, Random random) {
        var v = Vector<double>.Build.Dense(dimAttacker);
        var pa = new Point2D[env.N_ControlPointOfAttacker];

        v[0] = random.NextDouble() * env.Sources.Length;
        v[1] = random.NextDouble() * env.Sinks.Length;
        for (int j = 0; j < env.N_ControlPointOfAttacker; ++j) {
            pa[j] = hullAttacker.Sample(random);
        }
        Encode(v, pa, 2);
        FixVectorAtt(ref v, hullAttacker, random, env, PointFixMode.Resample);
        return v;
    }

    public static Vector<double> InitializePopulationDef(int dimDefender, SSGBarrierEnv2 env, ConvexHullD hullDefender, IComparer<Point2D> comparer, Random random) {
        var pd = new Point2D[env.N_Airship];
        var v = Vector<double>.Build.Dense(dimDefender);
        for (int j = 0; j < env.N_Airship; ++j) {
            pd[j] = hullDefender.Sample(random);
        }
        Encode(v, pd);
        FixVectorDef(ref v, hullDefender, comparer, random, PointFixMode.Resample);
        return v;

    }

    public static void CaculateUMatrix(Vector<double>[] popDefender, Vector<double>[] popAttacker, Matrix<double> umatrix, ObjectiveFunctionGame objective, bool parallel_) {
        if (parallel_) {
            Parallel.For(0, popDefender.Length, i => {
                for (int j = 0; j < popAttacker.Length; ++j) {
                    umatrix[i, j] = objective(popDefender[i], popAttacker[j]);
                }
            });
        } else {
            for (int i = 0; i < popDefender.Length; ++i) {
                for (int j = 0; j < popAttacker.Length; ++j) {
                    umatrix[i, j] = objective(popDefender[i], popAttacker[j]);
                }
            }
        }
    }
}