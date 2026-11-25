using MathNet.Numerics.LinearAlgebra;

namespace Aircraft.Core.Optimization.DE;

public static class DEShared {
    public static void ClampVector(ref Vector<double> mutant, Vector<double> lowerBounds, Vector<double> upperBounds) {
        int Dimension = mutant.Count;
        for (int j = 0; j < Dimension; j++) {
            mutant[j] = Clamp(mutant[j], lowerBounds[j], upperBounds[j]);
        }
    }

    public static void ClampVectorRand(ref Vector<double> mutant, Vector<double> lowerBounds, Vector<double> upperBounds, Random random) {
        int Dimension = mutant.Count;
        for (int j = 0; j < Dimension; j++) {
            if (mutant[j] < lowerBounds[j] || mutant[j] > upperBounds[j]) {
                mutant[j] = random.NextDouble() * (upperBounds[j] - lowerBounds[j]) + lowerBounds[j];
            }
        }
    }
    public static void ClampVectorRand(ref Vector<double> mutant, Vector<double> lowerBounds, Vector<double> upperBounds, Random random, int start, int end) {
        for (int j = start; j < end; j++) {
            if (mutant[j] < lowerBounds[j] || mutant[j] > upperBounds[j]) {
                mutant[j] = random.NextDouble() * (upperBounds[j] - lowerBounds[j]) + lowerBounds[j];
            }
        }
    }

    public static void ClampVectorReflection(ref Vector<double> mutant, Vector<double> lowerBounds, Vector<double> upperBounds) {
        int dim = mutant.Count;

        for (int i = 0; i < dim; i++) {
            double lo = lowerBounds[i];
            double hi = upperBounds[i];
            double range = hi - lo;

            // 防止非法区间
            if (range <= 0) {
                mutant[i] = lo;
                continue;
            }

            double value = mutant[i];

            // 若在边界内，直接跳过
            if (value >= lo && value <= hi)
                continue;

            // ===== 反射策略 =====
            // 将越界值通过镜像反射回 [lo, hi] 区间
            // 数学上：先平移到以 lo 为原点的坐标，再对 range 取模，处理多次反射
            double shifted = value - lo;

            // 使用取模实现多次反射（即使越界多个区间长度也能正确映射）
            // C# 的 % 对负数返回负值，需要修正到 [0, range)
            double mod = shifted % (2 * range);
            if (mod < 0) mod += 2 * range;

            if (mod <= range) {
                // 正向区间：lo + mod
                mutant[i] = lo + mod;
            } else {
                // 反向反射区间：[hi - (mod - range)] = lo + (2*range - mod)
                mutant[i] = lo + (2 * range - mod);
            }

            // 数值安全兜底：浮点误差可能造成微小越界
            if (mutant[i] < lo) mutant[i] = lo;
            if (mutant[i] > hi) mutant[i] = hi;
        }
    }

    public static int RandNotIn(int maxValue, int[] notIn, Random random) {
        while (true) {
            int randIndex = random.Next(maxValue);
            if (!notIn.Contains(randIndex)) {
                return randIndex;
            }
        }
    }
}