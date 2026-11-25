using System.Diagnostics;
using System.Text.Json.Serialization;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Spatial.Euclidean;

namespace Airship.Deployment.SSGBarrier;

public sealed record AirshipMixedStrategy(PointGeo2D[] Centers, double[] Allocations, int TotalAllocation) {
    [JsonIgnore] public Point2D[] CentersP2D = Centers.Select(c => c.ToPoint2D()).ToArray();
    /// <summary>
    /// 归一化处理
    /// 1. X = X/X.Sum()*K
    /// 2. 找到超出upper的部分，对剩余元素重归一化
    /// </summary>
    /// <param name="X">未归一化的向量</param>
    /// <param name="K">归一化后的和</param>
    /// <param name="upper">单个元素最大的值</param>
    /// <returns></returns>
    public static AirshipMixedStrategy Create(PointGeo2D[] Centers, Vector<double> X, int K, double upper) {
        int count = X.Count;
        double[] result = new double[count];

        // 1. 初步归一化: X = X / X.Sum() * K
        double sumX = X.Sum();

        // 防止除以0
        if (sumX == 0) {
            // 如果原向量和为0，通常意味着全为0，这里可以选择直接返回全0或均匀分布
            // 这里按均匀分布处理，或者保持全0，视具体业务而定。
            // 假设保持全0：
            return new AirshipMixedStrategy(Centers, new double[count], K);
        }

        // 计算初步缩放后的值
        double scaleFactor = K / sumX;
        for (int i = 0; i < count; i++) {
            result[i] = X[i] * scaleFactor;
        }

        // 2. 找到超出 upper 的部分，对剩余元素重归一化
        // 这是一个迭代过程，因为截断一部分后，剩余部分的总和变了，可能导致原本没超标的现在超标了（虽然概率小，但逻辑上需要循环直到稳定）
        // 不过对于大多数情况，一次处理即可。为了严谨，我们使用迭代法。

        bool changed;
        bool[] isCapped = new bool[count];

        do {
            changed = false;
            double currentSum = 0;
            int activeCount = 0; // 未被锁定的元素数量

            // 计算当前未被锁定的元素的总和
            for (int i = 0; i < count; i++) {
                if (isCapped[i]) {
                    currentSum += result[i]; // 锁定的元素值固定为 upper
                } else {
                    // 检查是否超标
                    if (result[i] > upper) {
                        result[i] = upper;
                        isCapped[i] = true; // 锁定该位置
                        changed = true;     // 标记发生了变动
                        currentSum += result[i];
                    } else {
                        activeCount++;
                    }
                }
            }

            // 如果有元素被锁定，需要重新分配剩余的和
            if (changed && activeCount > 0) {
                // 剩余需要的总和
                double remainingTarget = K - currentSum;

                // 计算未被锁定的原始向量的部分和（用于保持相对比例）
                double activeOriginalSum = 0;
                for (int i = 0; i < count; i++) {
                    if (!isCapped[i]) {
                        activeOriginalSum += X[i];
                    }
                }

                if (activeOriginalSum > 0) {
                    // 重新计算缩放因子
                    double newScale = remainingTarget / activeOriginalSum;

                    for (int i = 0; i < count; i++) {
                        if (!isCapped[i]) {
                            result[i] = X[i] * newScale;
                            Debug.Assert(result[i] >= 0);
                            // 注意：这里不立即检查是否超标，留给下一轮循环检查
                        }
                    }
                }
            }

        } while (changed);

        return new AirshipMixedStrategy(Centers, result, K);
    }
}