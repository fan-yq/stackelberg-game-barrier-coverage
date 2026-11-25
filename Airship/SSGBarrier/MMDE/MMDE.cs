using Aircraft.Core.Optimization.DE;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Random;
using Microsoft.Extensions.Logging;

namespace Airship.Deployment.SSGBarrier.MMDE;

public sealed record MMDEPop(Vector<double> Defender, Vector<double> Attacker, double Utility);
public abstract class MMDE {
    protected readonly int dimAttacker, dimDefender;
    double eps = 1E-10;
    protected readonly SortedList<double, MMDEPop> Populations;
    protected readonly ObjectiveFunctionGame objective;
    protected readonly int Ks, T, N; // N: population个数，Ks：bottom改善的个数，T：重新采样的个数
    readonly Vector<double>[] RegenerateCacheAtt, RegenerateCacheDef;
    readonly double[] RegenerateObjectiveCache;
    protected readonly bool parallel_;
    protected readonly ILogger logger;
    protected readonly Random random;
    protected bool popInitialized;
    public MMDE(int dimDefender, int dimAttacker, int N, int Ks, int T, ObjectiveFunctionGame objective, Random random, ILogger logger, bool parallel_) {
        this.dimAttacker = dimAttacker;
        this.dimDefender = dimDefender;
        this.parallel_ = parallel_;
        this.Ks = Ks;
        this.T = T;
        this.N = N;

        this.objective = objective;
        this.random = random;
        this.logger = logger;
        popInitialized = false;

        Comparer<double> comparer = Comparer<double>.Create((x, y) => y.CompareTo(x));
        Populations = new(comparer);
        RegenerateCacheAtt = new Vector<double>[T];
        RegenerateCacheDef = new Vector<double>[T];
        RegenerateObjectiveCache = new double[T];
        for (int i = 0; i < T; ++i) {
            RegenerateCacheDef[i] = Vector<double>.Build.Dense(dimDefender);
        }

        logger.LogDebug("dim(att/def): {}/{}, pop size: {}, Ks: {}, T: {}", dimAttacker, dimDefender, N, Ks, T);
    }
    public IList<MMDEPop> Optimize(int iterations) {
        if (!popInitialized) {
            InitializePopulations();
        }

        for (int i = 0; i < iterations; ++i) {
            BottomBosstingScheme();
            PartialRegeneration();
            if (i % 200 == 0) {
                logger.LogDebug("[{}/{}] maxmin: {:G3}", i, iterations, Populations.First().Key);
            }
            GC.Collect(0);
        }

        return Populations.Values;
    }

    void AddPopulation(MMDEPop pop1) {
        double obj = pop1.Utility;
        for (; Populations.ContainsKey(obj); obj += (obj == 0 ? eps : obj) * eps) ;
        Populations.Add(obj, pop1);
    }
    void BottomBosstingScheme() {
        for (int i = 0; i < Ks; ++i) {
            var SU = MutateAndCrossoverAtt(Populations);
            var (key, pop) = Populations.First();
            double obj = objective(pop.Defender, SU);
            if (pop.Utility > obj) {
                MMDEPop pop1 = new(pop.Defender, SU, obj);
                Populations.Remove(key);
                AddPopulation(pop1);
            }
        }
    }
    // ReaderWriterLockSlim rwLock = new();
    // void BottomBosstingScheme() {
    //     Parallel.For(0, Ks, i => {
    //         rwLock.EnterReadLock();
    //         var SU = MutateAndCrossoverAtt(Populations);
    //         var (key, pop) = Populations.First();
    //         rwLock.ExitReadLock();

    //         double obj = objective(pop.Defender, SU);
    //         if (pop.Utility > obj) {
    //             MMDEPop pop1 = new(pop.Defender, SU, obj);
    //             rwLock.EnterWriteLock();
    //             Populations.Remove(key);
    //             AddPopulation(pop1);
    //             rwLock.ExitWriteLock();
    //         }
    //     });
    // }
    // void BottomBosstingScheme() {
    //     int fraction = 4;
    //     double KsPerIter = (double)N / fraction, count = Ks / KsPerIter;
    //     var newPopulations = new ConcurrentBag<(double key, MMDEPop)>();
    //     for (int j = 0; j < count; ++j) {
    //         // take 
    //         Parallel.For(0, (int)KsPerIter, i => {
    //             var SU = MutateAndCrossoverAtt(Populations);
    //             var (key, pop) = Populations.ElementAt(i);
    //             double obj = objective(pop.Defender, SU);
    //             if (pop.Utility > obj) {
    //                 MMDEPop pop1 = new(pop.Defender, SU, obj);
    //                 newPopulations.Add((key, pop1));

    //             }
    //         });

    //         foreach (var (key, pop1) in newPopulations) {
    //             Populations.Remove(key);
    //             AddPopulation(pop1);
    //         }
    //         newPopulations.Clear();

    //     }
    // }

    void PartialRegeneration() {
        MutateAndCrossoverDef(Populations, RegenerateCacheDef);
        for (int i = 0; i < T; ++i) {
            RegenerateCacheAtt[i] = InitializePopulationAtt();
        }

        CaculateUMatrix(RegenerateCacheDef, RegenerateCacheAtt, RegenerateObjectiveCache);

        for (int i = 0; i < T; ++i) {
            Populations.RemoveAt(Populations.Count - 1);
        }
        for (int i = 0; i < T; ++i) {
            AddPopulation(new(RegenerateCacheDef[i].Clone(), RegenerateCacheAtt[i], RegenerateObjectiveCache[i]));
        }
    }

    void CaculateUMatrix(Vector<double>[] popDefender, Vector<double>[] popAttacker, IList<double> umatrix) {
        if (parallel_) {
            Parallel.For(0, popDefender.Length, i => {
                umatrix[i] = objective(popDefender[i], popAttacker[i]);
            });
        } else {
            for (int i = 0; i < popDefender.Length; ++i) {
                umatrix[i] = objective(popDefender[i], popAttacker[i]);
            }
        }
    }

    protected abstract Vector<double> MutateAndCrossoverAtt(SortedList<double, MMDEPop> pop);
    protected abstract void MutateAndCrossoverDef(SortedList<double, MMDEPop> pop, Vector<double>[] trialVectors);
    protected abstract Vector<double> InitializePopulationAtt();
    protected abstract Vector<double> InitializePopulationDef();
    protected virtual void InitializePopulations() {
        Vector<double>[] PopAttacker = new Vector<double>[N];
        Vector<double>[] PopDefender = new Vector<double>[N];
        double[] u = new double[N];
        for (int i = 0; i < N; ++i) {
            PopAttacker[i] = InitializePopulationAtt();
        }

        for (int i = 0; i < N; ++i) {
            PopDefender[i] = InitializePopulationDef();
        }
        CaculateUMatrix(PopDefender, PopAttacker, u);

        for (int i = 0; i < N; ++i) {
            AddPopulation(new(PopDefender[i], PopAttacker[i], u[i]));
        }
        popInitialized = true;
    }

    public static ACGAResult MMDEPopToACGAResult(IList<MMDEPop> pops, ObjectiveFunctionGame objective, bool parallel_) {
        int N = pops.Count;

        Vector<double>[] popAtt = new Vector<double>[N];
        Vector<double>[] popDef = new Vector<double>[N];

        for (int i = 0; i < N; ++i) {
            popAtt[i] = pops[i].Attacker;
            popDef[i] = pops[i].Defender;
        }

        Matrix<double> umatrix = Matrix<double>.Build.Dense(N, N);

        SSGShared.CaculateUMatrix(popDef, popAtt, umatrix, objective, parallel_);
        var l = Enumerable.Range(0, N).ToList();
        return new(popDef, popAtt, umatrix, l, l, ACGAMode.Stackelberg);
    }
}

public sealed class MMDEBoxed : MMDE {
    readonly Vector<double> lowerAttacker, upperAttacker, lowerDefender, upperDefender;

    public MMDEBoxed(int dimDefender, int dimAttacker, int N, int Ks, int T, ObjectiveFunctionGame objective, double[] lowerDefender, double[] upperDefender, double[] lowerAttacker, double[] upperAttacker, Random random, ILogger logger, bool parallel_) : base(dimDefender, dimAttacker, N, Ks, T, objective, random, logger, parallel_) {
        var V = Vector<double>.Build;
        this.lowerAttacker = V.Dense(lowerAttacker);
        this.upperAttacker = V.Dense(upperAttacker);
        this.lowerDefender = V.Dense(lowerDefender);
        this.upperDefender = V.Dense(upperDefender);
    }

    public double CR { get; init; }
    public double F { get; init; }
    protected override Vector<double> InitializePopulationAtt() {
        var pop = Vector<double>.Build.Dense(random.NextDoubles(dimAttacker)).PointwiseMultiply(upperAttacker - lowerAttacker) + lowerAttacker;
        return pop;
    }

    protected override Vector<double> InitializePopulationDef() {
        var pop = Vector<double>.Build.Dense(random.NextDoubles(dimDefender)).PointwiseMultiply(upperDefender - lowerDefender) + lowerDefender;
        return pop;
    }

    protected override Vector<double> MutateAndCrossoverAtt(SortedList<double, MMDEPop> pop) {
        var population = pop;
        int dimension = pop.ElementAt(0).Value.Attacker.Count;
        Vector<double> trialVector = Vector<double>.Build.Dense(dimension);
        // DE/rand/1: v = x_r1 + F*(x_r2 - x_r3)
        int r1 = DEShared.RandNotIn(N, [], random);
        int r2 = DEShared.RandNotIn(N, [r1], random);
        int r3 = DEShared.RandNotIn(N, [r1, r2], random);
        var mutant = population.ElementAt(r1).Value.Attacker + F * (population.ElementAt(r2).Value.Attacker - population.ElementAt(r3).Value.Attacker);
        DEShared.ClampVectorReflection(ref mutant, lowerAttacker, upperAttacker);

        int randIndex = random.Next(dimension);
        for (int j = 0; j < dimension; j++) {
            if (random.NextDouble() < CR || j == randIndex)
                trialVector[j] = mutant[j];
            else
                trialVector[j] = population.ElementAt(j).Value.Attacker[j];
        }

        return trialVector;
    }

    protected override void MutateAndCrossoverDef(SortedList<double, MMDEPop> pop, Vector<double>[] trialVectors) {
        int N = pop.Count;
        int dimension = pop.ElementAt(0).Value.Defender.Count;

        var population = pop;
        for (int i = 0; i < trialVectors.Length; i++) {
            // DE/rand/1: v = x_r1 + F*(x_r2 - x_r3)
            int r1 = DEShared.RandNotIn(N, [], random);
            int r2 = DEShared.RandNotIn(N, [r1], random);
            int r3 = DEShared.RandNotIn(N, [r1, r2], random);
            var mutant = population.ElementAt(r1).Value.Defender + F * (population.ElementAt(r2).Value.Defender - population.ElementAt(r3).Value.Defender);
            DEShared.ClampVectorReflection(ref mutant, lowerDefender, upperDefender);

            int randIndex = random.Next(dimension);
            for (int j = 0; j < dimension; j++) {
                if (random.NextDouble() < CR || j == randIndex)
                    trialVectors[i][j] = mutant[j];
                else
                    trialVectors[i][j] = population.ElementAt(i).Value.Defender[j];
            }
        }
    }
}