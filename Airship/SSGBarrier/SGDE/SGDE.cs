using System.Collections;
using System.Collections.Concurrent;
using System.Text.Json;
using Aircraft.Core.Code;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;

namespace Airship.Deployment.SSGBarrier.SGDE;

public record SGDESetting {
    public double Eps2 { get; set; }
    // 测试pabest的作用，1为全取，0.2为选取20%部分的pabest
    public double ProportionOfRandPBest { get; set; }
    public bool IsCrowding { get; set; } // recommand false, the improvement is little
    public bool UpdateBestAttacker { get; set; } = true;
    public PointFixMode PointFixMode { get; set; }
}
public abstract class SGDE {
    [DebugConfig]
    const bool recordAllMaxMinU = false;
    const double Diversity = 0;
    protected readonly int dimAttacker, dimDefender;
    readonly int PopSizeDef, PopSizeAttForEachDef;
    protected Matrix<double> UMatrix; // [defender-index][attacker-index]
    protected readonly ObjectiveFunctionGame objective;
    readonly Queue<Vector<double>> PopDefenderCache = [];

    protected sealed record SGDEPopulation(Vector<double> PopDefender, Vector<double>[] PopAttacker, List<int> BestAttIndices) : ISGDEPopulation {
        public IEnumerable<Vector<double>> BestAttackers => BestAttIndices.Select(i => PopAttacker[i]);
    }
    protected sealed record SGDEPopulation2(Vector<double> PopDefender, Vector<double>[] PopAttacker) : ISGDEPopulation;
    protected interface ISGDEPopulation {
        Vector<double> PopDefender { get; }
        Vector<double>[] PopAttacker { get; }
    }
    protected readonly SGDEPopulation[] populations;
    protected readonly bool parallel_, isCrowding;
    protected readonly Random random;
    protected readonly ILogger logger;
    protected bool popInitialized;
    protected double eps2;
    readonly SGDESetting sGDESetting;

    /// <param name="dimDefender"></param>
    /// <param name="dimAttacker"></param>
    /// <param name="PopSizeDefender"></param>
    /// <param name="PopSizeAttacker4EachDef"></param>
    /// <param name="objective"></param>
    /// <param name="random"></param>
    /// <param name="logger"></param>
    /// <param name="parallel_"></param>
    /// <param name="eps2">2. 替换原始策略时，如果误差小于eps就不替换.能够增加策略的丰富性</param>
    public SGDE(int dimDefender, int dimAttacker, int PopSizeDefender, int PopSizeAttacker4EachDef, ObjectiveFunctionGame objective, Random random, ILogger logger, bool parallel_, SGDESetting setting) {
        this.dimAttacker = dimAttacker;
        this.dimDefender = dimDefender;
        this.parallel_ = parallel_;
        this.eps2 = setting.Eps2;
        PopSizeAttForEachDef = PopSizeAttacker4EachDef;
        PopSizeDef = PopSizeDefender;
        populations = new SGDEPopulation[PopSizeDef];
        UMatrix = Matrix<double>.Build.Dense(PopSizeDef, PopSizeAttacker4EachDef);
        this.objective = objective;
        this.random = random;
        this.logger = logger;
        popInitialized = false;
        this.isCrowding = setting.IsCrowding;
        sGDESetting = setting;

        logger.LogDebug("{} dim(att/def): {}/{}, pop size(att/def): {}/{}, setting: {}", nameof(SGDE), dimAttacker, dimDefender, PopSizeAttacker4EachDef, PopSizeDefender, setting);
    }
    public SGDE(int dimDefender, int dimAttacker, int PopSizeDefender, int PopSizeAttacker4EachDef, ObjectiveFunctionGame objective, Random random, ILogger logger, bool parallel_, bool isCrowding, double eps2 = 1E-3) : this(dimDefender, dimAttacker, PopSizeDefender, PopSizeAttacker4EachDef, objective, random, logger, parallel_, new() { Eps2 = eps2, IsCrowding = isCrowding, ProportionOfRandPBest = 1 }) { }

    public ACGAResult Optimize(int iterations) {
        if (!popInitialized) {
            InitializePopulations();
        }

        // 新的总群umatrix缓存
        Matrix<double> umatrix = Matrix<double>.Build.Dense(PopSizeDef, PopSizeAttForEachDef);
        Vector<double>[] trialVectorsDef = new Vector<double>[PopSizeDef];
        Vector<double>[] trialVectorsAtt = new Vector<double>[PopSizeDef * PopSizeAttForEachDef];
        var V = Vector<double>.Build;

        // internal variables
        List<double> mmU = [];
        List<int> changeAttC = [], changeDefC = [];

        for (int i = 0; i < PopSizeDef * PopSizeAttForEachDef; ++i) {
            trialVectorsAtt[i] = V.Dense(dimAttacker);
        }
        for (int i = 0; i < PopSizeDef; ++i) {
            trialVectorsDef[i] = V.Dense(dimDefender);
        }

        // bool defChanged = true, attChanged = true;
        int attCount = 0, defCount = 0;
        for (int i = 0; i < iterations; ++i) {
            int changedefCount = IterDefender(populations, umatrix, trialVectorsDef);
            int changeattCount = IterAttacker(populations, umatrix, trialVectorsAtt);
            defCount += changedefCount;
            attCount += changeattCount;
            if (i % 50 == 0) {
                double totalAttCount = 50 * PopSizeAttForEachDef * PopSizeDef, totalDefCount = 50 * PopSizeDef;
                logger.LogDebug("[{}/{}], maxmin: {:G3} change fraction(att/def): {:G2}/{:G2}", i, iterations, ACGAResult.MaxMinU(UMatrix, Enumerable.Range(0, PopSizeDef).ToList()), attCount / totalAttCount, defCount / totalDefCount);
                attCount = 0; defCount = 0;
            }

#pragma warning disable CS0162,CS8625, CS8604
            if (recordAllMaxMinU) {
                mmU.Add(ACGAResult.MaxMinU(UMatrix, Enumerable.Range(0, PopSizeDef).ToList()));
                changeAttC.Add(changeattCount);
                changeDefC.Add(changedefCount);
            }

            GC.Collect(0);
        }
        var PopDefender = new PopDefWrap(populations, []).ToArray();
        var PopAttacker = new PopAttWrap(populations, PopSizeAttForEachDef).ToArray();
        var umatrix1 = Matrix<double>.Build.Dense(PopSizeDef, PopSizeDef * PopSizeAttForEachDef);
        SSGShared.CaculateUMatrix(PopDefender, PopAttacker, umatrix1, objective, parallel_);

        if (recordAllMaxMinU) {
            string basePath = @"C:\Users\fyq\Documents\dev\aircraft\data\SSG\internal";
            CAInternalInfo r = new() {
                Note = $"SGDE {sGDESetting}",
                PopSizeDef = PopSizeDef,
                PopSizeAttForEachDef = PopSizeAttForEachDef,
                PopSizeAtt = PopSizeAttForEachDef * PopSizeDef,
                CreateAt = DateTime.Now,
                MMU = mmU,
                ChangeAttCount = changeAttC,
                ChangeDefCount = changeDefC,
            };
            File.WriteAllBytes(Path.Join(basePath, $"SGDE-{Guid.NewGuid()}.json"), JsonSerializer.SerializeToUtf8Bytes(r));
        }

        return new(PopDefender, PopAttacker, umatrix1, Enumerable.Range(0, PopSizeDef).ToList(), Enumerable.Range(0, PopSizeDef * PopSizeAttForEachDef).ToList(), ACGAMode.Stackelberg);
    }

    record struct MutantIndex(int IDef, int IAttFrom, Vector<double> BestAtt, double U); // replace [IDef][IAttFrom] to [IAttToDef][IAttToAtt]

    // 交叉生成种群
    /// <param name="trialVectors">defender trialVectors cache</param>
    /// <returns>changed count</returns>
    int IterDefender(SGDEPopulation[] pop, Matrix<double> umatrix, Vector<double>[] trialVectors) {
        int changed = 0;
        // generate trial vector
        MutateAndCrossoverDef(new PopDefWrap(pop, PopDefenderCache), trialVectors);
        SGDEPopulation2[] newPopulation = new SGDEPopulation2[PopSizeDef];
        for (int i = 0; i < PopSizeDef; ++i) {
            newPopulation[i] = new(trialVectors[i], pop[i].PopAttacker);
        }


        // var oldRowMins = Enumerable.Range(0, PopSizeDef).Select(i => umatrix.Row(i).Min()).ToArray();
        ConcurrentBag<MutantIndex> mutantIndices = [];
        var bestPop = populations.SelectMany(p => p.BestAttackers).ToArray();
        var randBestPop = bestPop;
        if (sGDESetting.ProportionOfRandPBest != 1) {
            var randBestPopIndices = GetByIndex(0, bestPop.Length, (int)(sGDESetting.ProportionOfRandPBest * bestPop.Length));
            randBestPop = randBestPopIndices.Select(i => bestPop[i]).ToArray(); // 测试
        }


        Parallel.For(0, PopSizeDef, i => {
            for (int j = 0; j < PopSizeAttForEachDef; ++j) {
                umatrix[i, j] = objective(newPopulation[i].PopDefender, newPopulation[i].PopAttacker[j]);
            }

            var trialVecObj = umatrix.Row(i);

            // 计算unit内部的最小值，和最优和最差个体index
            double minTrialObj = double.MaxValue, minDefenderObj_j = double.MaxValue;
            int iAttFrom = -1;
            double maxAttFrom = double.MinValue;
            Vector<double> bestAttPop = Vector<double>.Build.Dense([]);
            for (int j = 0; j < PopSizeAttForEachDef; ++j) {
                if (minTrialObj > trialVecObj[j]) {
                    minTrialObj = trialVecObj[j];
                    bestAttPop = populations[i].PopAttacker[j];
                }
                minDefenderObj_j = Min(minDefenderObj_j, UMatrix[i, j]);

                if (maxAttFrom < umatrix[i, j]) {
                    maxAttFrom = umatrix[i, j];
                    iAttFrom = j;
                }
            }


            // min vec
            if (minTrialObj > minDefenderObj_j) {
                if (sGDESetting.ProportionOfRandPBest > 0) {
                    var trailObj2 = CaculateUVector(new SGDEPopulation2(trialVectors[i], randBestPop));
                    var minDefObj2 = CaculateUVector(new SGDEPopulation2(populations[i].PopDefender, randBestPop));
                    for (int j = 0; j < randBestPop.Length; ++j) {
                        if (trailObj2[j] < minTrialObj) {
                            minTrialObj = trailObj2[j];
                            bestAttPop = randBestPop[j];
                        }
                    }
                    minDefenderObj_j = Min(minDefenderObj_j, minDefObj2.Min());
                }
                if (minTrialObj > minDefenderObj_j) {
                    mutantIndices.Add(new(i, iAttFrom, bestAttPop, minTrialObj));
                }
            }
        });


        foreach (var mi in mutantIndices) {
            int i = mi.IDef;
            var trialVecObj = umatrix.Row(i);
            pop[i] = new(trialVectors[i].Clone(), pop[i].PopAttacker, []);
            UMatrix.SetRow(i, trialVecObj);

            if (sGDESetting.UpdateBestAttacker) {
                pop[i].PopAttacker[mi.IAttFrom] = mi.BestAtt;
                UMatrix[i, mi.IAttFrom] = mi.U;
            }
            changed++;
        }
        CaculateBestAtt(populations, UMatrix);

        for (int i = 0; i < PopSizeDef * Diversity; ++i) {
            PopDefenderCache.Enqueue(InitializePopulationDef());
        }
        // 保证queue大小小于MinPopSizeDefender
        int CapCache = PopDefenderCache.Count;
        for (int i = 0; i < CapCache - PopSizeDef; ++i) {
            PopDefenderCache.Dequeue();
        }

        return changed;
    }

    /// <param name="trialVectors">attacker trialVectors cache</param>
    int IterAttacker(SGDEPopulation[] pops, Matrix<double> umatrix, Vector<double>[] trialVectors) {
        int changed = 0;

        // generate trial vector
        MutateAndCrossoverAtt(new PopAttWrap(pops, PopSizeAttForEachDef), trialVectors);
        SGDEPopulation2[] newPopulation = new SGDEPopulation2[PopSizeDef];
        for (int i = 0; i < PopSizeDef; ++i) {
            newPopulation[i] = new(pops[i].PopDefender, trialVectors[(i * PopSizeAttForEachDef)..((i + 1) * PopSizeAttForEachDef)]);
        }
        // evaluate matrix
        CaculateUMatrix(newPopulation, umatrix);

        for (int i = 0; i < PopSizeDef; ++i) {
            for (int j = 0; j < PopSizeAttForEachDef; ++j) {
                int indexAtt = j;
                if (isCrowding) {
                    // select the closed pop
                    indexAtt = Enumerable.Range(0, PopSizeAttForEachDef).MinBy(k => (pops[i].PopAttacker[k] - newPopulation[i].PopAttacker[j]).L2Norm());
                }
                double trialObj = umatrix[i, j], popObj = UMatrix[i, indexAtt];
                if (trialObj < popObj - eps2) {
                    pops[i].PopAttacker[indexAtt] = newPopulation[i].PopAttacker[j];
                    UMatrix[i, indexAtt] = trialObj;
                    changed++;
                }
            }
        }
        CaculateBestAtt(populations, UMatrix);

        return changed;
    }


    void CaculateUMatrix<T>(T[] population, Matrix<double> umatrix) where T : ISGDEPopulation {
        int m = population[0].PopAttacker.Length;
        if (parallel_) {
            Parallel.For(0, population.Length, i => {
                for (int j = 0; j < m; ++j) {
                    umatrix[i, j] = objective(population[i].PopDefender, population[i].PopAttacker[j]);
                }
            });
        } else {
            for (int i = 0; i < population.Length; ++i) {
                for (int j = 0; j < m; ++j) {
                    umatrix[i, j] = objective(population[i].PopDefender, population[i].PopAttacker[j]);
                }
            }
        }
    }
    double[] CaculateUVector<T>(T population) where T : ISGDEPopulation {
        int m = population.PopAttacker.Length;
        double[] res = new double[m];
        for (int j = 0; j < m; ++j) {
            res[j] = objective(population.PopDefender, population.PopAttacker[j]);
        }
        return res;
    }

    protected virtual void InitializePopulations() {
        for (int i = 0; i < PopSizeDef; ++i) {
            var popAtt = new Vector<double>[PopSizeAttForEachDef];
            for (int j = 0; j < PopSizeAttForEachDef; ++j) {
                popAtt[j] = InitializePopulationAtt();
            }
            populations[i] = new(InitializePopulationDef(), popAtt, []);
        }
        CaculateUMatrix(populations, UMatrix);
        CaculateBestAtt(populations, UMatrix);
        popInitialized = true;
    }

    void CaculateBestAtt(SGDEPopulation[] pop, Matrix<double> umatrix) {
        for (int i = 0; i < PopSizeDef; ++i) {
            // row min
            double rowMin = umatrix.Row(i).Min();
            pop[i].BestAttIndices.Clear();
            for (int j = 0; j < PopSizeAttForEachDef; ++j) {
                if (umatrix[i, j] <= rowMin + eps2) {
                    pop[i].BestAttIndices.Add(j);
                }
            }
        }
    }
    protected abstract Vector<double> InitializePopulationAtt();
    protected abstract Vector<double> InitializePopulationDef();

    protected abstract void MutateAndCrossoverAtt(IReadOnlyList<Vector<double>> pop, Vector<double>[] trialVectors);
    protected abstract void MutateAndCrossoverDef(IReadOnlyList<Vector<double>> pop, Vector<double>[] trialVectors);
    sealed class PopAttWrap(SGDEPopulation[] populations, int PopSizeAttForEachDef) : IReadOnlyList<Vector<double>> {
        public Vector<double> this[int index] => populations[index / PopSizeAttForEachDef].PopAttacker[index % PopSizeAttForEachDef];
        public int Count => populations.Length * PopSizeAttForEachDef;
        public IEnumerator<Vector<double>> GetEnumerator() {
            for (int i = 0; i < Count; ++i) {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }

    sealed class PopDefWrap(SGDEPopulation[] populations, Queue<Vector<double>> pop2) : IReadOnlyList<Vector<double>> {
        public Vector<double> this[int index] => index >= populations.Length ? pop2.ElementAt(index - populations.Length) : populations[index].PopDefender;
        public int Count => populations.Length + pop2.Count;
        public IEnumerator<Vector<double>> GetEnumerator() {
            for (int i = 0; i < Count; ++i) {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }

    public List<int> GetByIndex(int min, int max, int count) {
        int[] pool = Enumerable.Range(min, max - min).ToArray();
        List<int> result = new List<int>(count);
        int currentMax = pool.Length;
        for (int i = 0; i < count; i++) {
            int index = random.Next(0, currentMax);
            result.Add(pool[index]);
            pool[index] = pool[--currentMax];
        }
        return result;
    }

}