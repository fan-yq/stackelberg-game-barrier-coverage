using System.Collections;
using System.Diagnostics;
using System.Text.Json;
using Aircraft.Core.Code;
using Aircraft.Core.Optimization.DE;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Random;
using Microsoft.Extensions.Logging;

namespace Airship.Deployment.SSGBarrier;

public delegate double ObjectiveFunctionGame(Vector<double> defender, Vector<double> attacker);
public enum ACGAMode {
    Nash, Stackelberg
}
public enum CAMode {
    ACGA, PCGA
}

public sealed record ACGAConfig {
    public ACGAMode Mode { get; }
    public CAMode AlgoType { get; }
    public double eps { get; } // only Stackerberg has, filter x_{eps}
    public bool UsingCrowding { get; }
    public PointFixMode PointFixMode { get; set; } = PointFixMode.Resample;
    /// <param name="eps">1.Stackelberg 博弈的eps，当两个策略的效用接近时认为是混合策略</param>
    ACGAConfig(ACGAMode mode, double eps, bool useCrowding = false, CAMode cAMode = CAMode.ACGA) { Mode = mode; this.eps = eps; UsingCrowding = useCrowding; AlgoType = cAMode; }
    public static ACGAConfig Nash = new(ACGAMode.Nash, 0);
    public static ACGAConfig Stackelberg(double eps) => new(ACGAMode.Stackelberg, eps);
    public ACGAConfig UseCrowding(bool f = true) => new(Mode, eps, f, AlgoType);
    public ACGAConfig UseCAMode(CAMode cAMode) => new(Mode, eps, UsingCrowding, cAMode);
}

public abstract class ACGA {
    [DebugConfig]
    const bool recordAllMaxMinU = false;
    protected readonly int dimAttacker, dimDefender;
    // 控制最小的population数量，控制每一次population生成的数量

    protected readonly int PopSizeAtt, PopSizeDef;
    protected Matrix<double> UMatrix; // [defender-index][attacker-index]
    protected readonly ObjectiveFunctionGame objective;
    protected readonly Vector<double>[] PopAttacker, PopDefender;
    List<int> SelectAttIndices, SelectDefIndices;
    readonly Queue<Vector<double>> PopAttackerCache = [], PopDefenderCache = []; // 如果pop数量太少了，增加多样性
    protected readonly bool parallel_, isCrowding;
    protected readonly Random random;
    protected readonly ILogger logger;
    protected bool popInitialized;
    protected double Diversity, eps, eps2; // 每次迭代在queue中重新初始化，增加多样性
    public ACGAMode Mode { get; }
    readonly ACGAConfig config;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="dimDefender"></param>
    /// <param name="dimAttacker"></param>
    /// <param name="PopSizeDefender"></param>
    /// <param name="PopSizeAttacker"></param>
    /// <param name="objective"></param>
    /// <param name="random"></param>
    /// <param name="logger"></param>
    /// <param name="parallel_"></param>
    /// <param name="Diversity">每次迭代在queue中重新初始化，增加多样性</param>
    /// <param name="eps2">2. 替换原始策略时，如果误差小于eps就不替换.能够增加策略的丰富性</param>
    public ACGA(ACGAConfig config, int dimDefender, int dimAttacker, int PopSizeDefender, int PopSizeAttacker, ObjectiveFunctionGame objective, Random random, ILogger logger, bool parallel_, double Diversity = 0.05, double eps2 = 1E-3) {
        this.dimAttacker = dimAttacker;
        this.dimDefender = dimDefender;
        this.Diversity = Diversity;
        this.parallel_ = parallel_;
        this.eps = config.eps;
        this.eps2 = eps2;
        Mode = config.Mode;
        this.config = config;
        PopSizeAtt = PopSizeAttacker;
        PopSizeDef = PopSizeDefender;
        PopAttacker = new Vector<double>[PopSizeAtt];
        PopDefender = new Vector<double>[PopSizeDef];
        SelectAttIndices = Enumerable.Range(0, PopSizeAtt).ToList();
        SelectDefIndices = Enumerable.Range(0, PopSizeDef).ToList();
        UMatrix = Matrix<double>.Build.Dense(PopSizeDef, PopSizeAtt);
        this.objective = objective;
        this.random = random;
        this.logger = logger;
        popInitialized = false;
        isCrowding = config.UsingCrowding;

        logger.LogDebug("dim(att/def): {}/{}, pop size(att/def): {}/{}, mode: {}, isCrowding: {}", dimAttacker, dimDefender, PopSizeAttacker, PopSizeDefender, Mode, isCrowding);
    }

    public ACGAResult Optimize(int iterations) {
        if (!popInitialized) {
            InitializePopulations();
        }
        var V = Vector<double>.Build;
        var trialVectorsAtt = new Vector<double>[PopSizeAtt];
        var trialVectorsDef = new Vector<double>[PopSizeDef];
        for (int i = 0; i < PopSizeAtt; ++i) {
            trialVectorsAtt[i] = V.Dense(dimAttacker);
        }
        for (int i = 0; i < PopSizeDef; ++i) {
            trialVectorsDef[i] = V.Dense(dimDefender);
        }

        // 新的总群umatrix缓存
        Matrix<double> umatrix = Matrix<double>.Build.Dense(PopDefender.Length, PopAttacker.Length);
        Matrix<double> umatrix2 = Matrix<double>.Build.Dense(PopDefender.Length, PopAttacker.Length);

        // internal variables
        List<double> mmU = [];
        List<int> changeAttC = [], changeDefC = [];
        // bool defChanged = true, attChanged = true;
        int attCount = 0, defCount = 0;
        for (int i = 0; i < iterations; ++i) {
            // if (attChanged) { defChanged = IterDefender(trialVectorsDef, umatrix); defCount++; }
            // if (defChanged) { attChanged = IterAttacker(trialVectorsAtt, umatrix); attCount++; }
            // if (!attChanged && !defChanged) {
            //     logger.LogDebug("[{}/{}] attacker and defender did not change", i, iterations);
            //     attChanged = true; defChanged = true;
            // }

            int changedefCount, changeattCount;

            if (config.AlgoType is CAMode.ACGA) {
                IterDefender1(trialVectorsDef, umatrix);
                changedefCount = IterDefender2(trialVectorsDef, umatrix);

                IterAttacker1(trialVectorsAtt, umatrix);
                changeattCount = IterAttacker2(trialVectorsAtt, umatrix);
            } else if (config.AlgoType is CAMode.PCGA) {
                IterDefender1(trialVectorsDef, umatrix);
                IterAttacker1(trialVectorsAtt, umatrix2);
                changedefCount = IterDefender2(trialVectorsDef, umatrix);
                changeattCount = IterAttacker2(trialVectorsAtt, umatrix2);
            } else {
                throw new NotImplementedException();
            }
            attCount += changeattCount;
            defCount += changedefCount;

            if (i % 50 == 0) {
                double totalAttCount = 50 * PopSizeAtt, totalDefCount = 50 * PopSizeDef;
                logger.LogDebug("[{}/{}] maxmin: {:G3} minmax: {:G3}, change fraction(att/def): {:G2}/{:G2}", i, iterations, ACGAResult.MaxMinU(UMatrix, SelectDefIndices), ACGAResult.MinMaxU(UMatrix, SelectAttIndices), attCount / totalAttCount, defCount / totalDefCount);
                attCount = 0; defCount = 0;
            }

#pragma warning disable CS0162, CS8625, CS8604
            if (recordAllMaxMinU) {
                mmU.Add(ACGAResult.MaxMinU(UMatrix, Enumerable.Range(0, PopSizeAtt).ToList()));
                changeAttC.Add(changeattCount);
                changeDefC.Add(changedefCount);
            }

            GC.Collect(0);
        }

        if (recordAllMaxMinU) {
            string basePath = @"C:\Users\fyq\Documents\dev\aircraft\data\SSG\internal";
            CAInternalInfo r = new() {
                Note = $"ACGA {config}",
                PopSizeDef = PopSizeDef,
                PopSizeAttForEachDef = 0,
                PopSizeAtt = PopSizeAtt,
                CreateAt = DateTime.Now,
                MMU = mmU,
                ChangeAttCount = changeAttC,
                ChangeDefCount = changeDefC,
            };
            File.WriteAllBytes(Path.Join(basePath, $"ACGA-{Guid.NewGuid()}.json"), JsonSerializer.SerializeToUtf8Bytes(r));
        }

        return new(PopDefender, PopAttacker, UMatrix, SelectDefIndices, SelectAttIndices, Mode);
    }

    // 交叉生成种群
    /// <param name="trialVectors">defender trialVectors cache</param>
    /// <returns>changed count</returns>
    void IterDefender1(Vector<double>[] trialVectors, Matrix<double> umatrix) {
        // generate trial vector
        MutateAndCrossoverDef(new MergePopAndCache(PopDefender, PopDefenderCache), trialVectors);
        // evaluate matrix
        CaculateUMatrix(trialVectors, PopAttacker, umatrix);
    }
    /// <summary>
    /// 选择
    /// </summary>
    int IterDefender2(Vector<double>[] trialVectors, Matrix<double> umatrix) {
        int changed = 0;
        // if (Mode is ACGAMode.Stackelberg) {
        //     SelectAttIndices.Clear();
        //     double[] maxUs = Enumerable.Repeat(0, PopAttacker.Length).Select(j => umatrix.Column(j).Max()).ToArray();
        //     double minMaxU = maxUs.Min();
        //     for (int i = 0; i < PopAttacker.Length; ++i) {
        //         // 如果比现有的稍大，则直接加入
        //         if (maxUs[i] - minMaxU < eps && minMaxU <= maxUs[i]) {
        //             SelectAttIndices.Add(i);
        //         }
        //     }
        // }

        // update ideal point
        // if is major replace old 
        // 如果优超原种群，则取代，如果被优超，则跳过，都不是则append
        for (int i = 0; i < trialVectors.Length; ++i) {
            var trialVecObj = umatrix.Row(i);
            int indexPop = i;
            if (isCrowding) {
                // select the closed pop
                indexPop = Enumerable.Range(0, trialVectors.Length).MinBy(j => (PopDefender[j] - trialVectors[i]).L2Norm());
            }

            double minTrialObj, minDefenderObj_j;
            if (Mode is ACGAMode.Nash) {
                // 需要考虑trial在column max上是否优于原种群
                minTrialObj = trialVecObj.Min();
                minDefenderObj_j = UMatrix.Row(indexPop).Min();
            } else if (Mode is ACGAMode.Stackelberg) {
                // 只需要考虑trial在bestDefenderIndex上是否优于原种群
                minTrialObj = SelectAttIndices.Min(j => trialVecObj[j]);
                minDefenderObj_j = SelectAttIndices.Min(j => UMatrix[indexPop, j]);
            } else {
                throw new UnreachableException();
            }

            // min vec
            if (minTrialObj > minDefenderObj_j + eps2) {
                // if (SuperiorJudge(UMatrix.Row(i), trialVecObj, SelectAttIndices) == -1) {
                PopDefenderCache.Enqueue(PopDefender[indexPop]);
                PopDefender[indexPop] = trialVectors[i].Clone();
                UMatrix.SetRow(indexPop, trialVecObj);
                changed++;
                // break;
            }

        }

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
    void IterAttacker1(Vector<double>[] trialVectors, Matrix<double> umatrix) {
        // generate trial vector
        MutateAndCrossoverAtt(new MergePopAndCache(PopAttacker, PopAttackerCache), trialVectors);
        // evaluate matrix
        CaculateUMatrix(PopDefender, trialVectors, umatrix);
    }
    int IterAttacker2(Vector<double>[] trialVectors, Matrix<double> umatrix) {
        int changed = 0;


        if (Mode is ACGAMode.Stackelberg) {
            SelectDefIndices.Clear();
            double[] minUs = Enumerable.Repeat(0, PopDefender.Length).Select(i => umatrix.Row(i).Min()).ToArray();
            double maxMinU = minUs.Max();
            for (int i = 0; i < PopDefender.Length; ++i) {
                // 如果比现有的稍小，则直接加入
                if (maxMinU - minUs[i] <= eps && maxMinU >= minUs[i]) {
                    SelectDefIndices.Add(i);
                }
            }
        }

        for (int j = 0; j < trialVectors.Length; ++j) {
            var trialVecObj = umatrix.Column(j);
            int indexPop = j;
            if (isCrowding) {
                // select the closed pop
                indexPop = Enumerable.Range(0, trialVectors.Length).MinBy(i => (PopAttacker[i] - trialVectors[j]).L2Norm());
            }

            double maxTrialObj, maxattackerObj;
            if (Mode is ACGAMode.Nash) {
                // 需要考虑trial在column max上是否优于原种群
                maxTrialObj = trialVecObj.Max();
                maxattackerObj = UMatrix.Column(indexPop).Max();
            } else if (Mode is ACGAMode.Stackelberg) {
                // 只需要考虑trial在bestDefenderIndex上是否优于原种群
                maxTrialObj = SelectDefIndices.Max(i => trialVecObj[i]);
                maxattackerObj = SelectDefIndices.Max(i => UMatrix[i, indexPop]);
            } else {
                throw new UnreachableException();
            }

            if (maxTrialObj < maxattackerObj - eps2) {
                // if (SuperiorJudge(UMatrix.Column(j), trialVecObj, SelectDefIndices) == 1) {
                PopAttackerCache.Enqueue(PopAttacker[indexPop]);
                PopAttacker[indexPop] = trialVectors[j].Clone();
                UMatrix.SetColumn(indexPop, trialVecObj);
                changed++;
            }

        }

        for (int i = 0; i < PopSizeAtt * Diversity; ++i) {
            PopAttackerCache.Enqueue(InitializePopulationAtt());
        }
        // 保证queue大小小于MinPopSizeDefender
        int CapCache = PopAttackerCache.Count;
        for (int i = 0; i < CapCache - PopSizeAtt; ++i) {
            PopAttackerCache.Dequeue();
        }
        return changed;
    }


    void CaculateUMatrix(Vector<double>[] popDefender, Vector<double>[] popAttacker, Matrix<double> umatrix) {
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

    protected virtual ACGAPopulation InitializePopulations() {
        for (int i = 0; i < PopSizeAtt; ++i) {
            PopAttacker[i] = InitializePopulationAtt();
        }

        for (int i = 0; i < PopSizeDef; ++i) {
            PopDefender[i] = InitializePopulationDef();
        }
        CaculateUMatrix(PopDefender, PopAttacker, UMatrix);
        popInitialized = true;
        return new(PopDefender, PopAttacker, UMatrix);
    }
    protected abstract Vector<double> InitializePopulationAtt();
    protected abstract Vector<double> InitializePopulationDef();

    protected abstract void MutateAndCrossoverAtt(IReadOnlyList<Vector<double>> pop, Vector<double>[] trialVectors);
    protected abstract void MutateAndCrossoverDef(IReadOnlyList<Vector<double>> pop, Vector<double>[] trialVectors);

    public sealed record ACGAPopulation(Vector<double>[] PopulationDef, Vector<double>[] PopulationAtt, Matrix<double>? Umatrix);
    public ACGAPopulation GetPopulation() => new(PopDefender, PopAttacker, UMatrix);
    public void SetPopulation(ACGAPopulation aCEAPopulation) {
        var (pd, pa, u) = aCEAPopulation;
        Array.Copy(pd, PopDefender, pd.Length);
        Array.Copy(pa, PopAttacker, pa.Length);
        if (u is not null) {
            UMatrix = u;
        } else {
            CaculateUMatrix(PopDefender, PopAttacker, UMatrix);
        }
        popInitialized = true;
    }

    /// <summary>
    /// 判断优超，如果u1全大于等于u2，返回1
    /// 如果u2全大于等于u1，返回-1
    /// 如果相等返回2
    /// 否则返回0
    /// </summary>
    static int SuperiorJudge(IList<double> u1, IList<double> u2, List<int> select) {
        bool allGreaterThan = true, allLessThan = true;
        Debug.Assert(u1.Count == u2.Count);
        foreach (var i in select) {
            allGreaterThan &= u1[i] >= u2[i];
            allLessThan &= u1[i] <= u2[i];
        }

        if (allGreaterThan && allLessThan) { return 2; }
        if (allGreaterThan) { return 1; }
        if (allLessThan) { return -1; }
        return 0;
    }

    sealed class MergePopAndCache(IReadOnlyList<Vector<double>> pop1, Queue<Vector<double>> pop2) : IReadOnlyList<Vector<double>> {
        public Vector<double> this[int index] => index >= pop1.Count ? pop2.ElementAt(index - pop1.Count) : pop1[index];
        public int Count => pop1.Count + pop2.Count;
        public IEnumerator<Vector<double>> GetEnumerator() {
            return pop1.Concat(pop2).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}

public sealed class ACGABoxed : ACGA {
    readonly Vector<double> lowerAttacker, upperAttacker, lowerDefender, upperDefender;
    public double CR { get; init; }
    public double F { get; init; }
    public ACGABoxed(ACGAConfig config, int dimDefender, int dimAttacker, int PopSizeDefender, int PopSizeAttacker, ObjectiveFunctionGame objective, Vector<double> lowerDefender, Vector<double> upperDefender, Vector<double> lowerAttacker, Vector<double> upperAttacker, Random random, ILogger logger, bool parallel_ = true) : base(config, dimDefender, dimAttacker, PopSizeDefender, PopSizeAttacker, objective, random, logger, parallel_) {
        this.lowerAttacker = lowerAttacker;
        this.upperAttacker = upperAttacker;
        this.lowerDefender = lowerDefender;
        this.upperDefender = upperDefender;
    }

    public ACGABoxed(ACGAConfig config, int dimDefender, int dimAttacker, int PopSizeDefender, int PopSizeAttacker, ObjectiveFunctionGame objective, double[] lowerDefender, double[] upperDefender, double[] lowerAttacker, double[] upperAttacker, Random random, ILogger logger, bool parallel_ = true) : base(config, dimDefender, dimAttacker, PopSizeDefender, PopSizeAttacker, objective, random, logger, parallel_) {
        var V = Vector<double>.Build;
        this.lowerAttacker = V.Dense(lowerAttacker);
        this.upperAttacker = V.Dense(upperAttacker);
        this.lowerDefender = V.Dense(lowerDefender);
        this.upperDefender = V.Dense(upperDefender);
    }

    public ACGABoxed(int dimDefender, int dimAttacker, int PopSizeDefender, int PopSizeAttacker, ObjectiveFunctionGame objective, double[] lowerDefender, double[] upperDefender, double[] lowerAttacker, double[] upperAttacker, Random random, ILogger logger, bool parallel_ = true)
    : this(ACGAConfig.Nash, dimDefender, dimAttacker, PopSizeDefender, PopSizeAttacker, objective, lowerDefender, upperDefender, lowerAttacker, upperAttacker, random, logger, parallel_) { }
    public ACGABoxed(int dimDefender, int dimAttacker, int PopSizeDefender, int PopSizeAttacker, ObjectiveFunctionGame objective, Vector<double> lowerDefender, Vector<double> upperDefender, Vector<double> lowerAttacker, Vector<double> upperAttacker, Random random, ILogger logger, bool parallel_ = true)
    : this(ACGAConfig.Nash, dimDefender, dimAttacker, PopSizeDefender, PopSizeAttacker, objective, lowerDefender, upperDefender, lowerAttacker, upperAttacker, random, logger, parallel_) { }

    void MutateAndCrossover(IReadOnlyList<Vector<double>> state, Vector<double>[] trialVectors, Vector<double> l, Vector<double> u) {
        int N = state.Count, dimension = state[0].Count;
        var population = state;
        for (int i = 0; i < trialVectors.Length; i++) {
            // DE/rand/1: v = x_r1 + F*(x_r2 - x_r3)
            int r1 = DEShared.RandNotIn(N, [], random);
            int r2 = DEShared.RandNotIn(N, [r1], random);
            int r3 = DEShared.RandNotIn(N, [r1, r2], random);
            var mutant = population[r1] + F * (population[r2] - population[r3]);
            DEShared.ClampVectorReflection(ref mutant, l, u);
            // DEShared.ClampVector(ref mutant, l, u);

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