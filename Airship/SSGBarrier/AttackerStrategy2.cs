using System.Collections;
using MathNet.Numerics.LinearAlgebra;

namespace Airship.Deployment.SSGBarrier;

public sealed class AttackerStrategy2 {
    public PointGeo2D[][] Paths { get; } // [attacker index, way points]

    /// <summary>
    /// 初始化攻击者策略
    /// </summary>
    /// <param name="paths">所有可能的路径集合</param>
    public AttackerStrategy2(PointGeo2D[][] paths) {
        // 排序逻辑：
        // 1. 第一优先级：路径的起点 (p[0])
        // 2. 第二优先级：如果起点相同，使用自定义比较器比较整条路径的字典序
        //    (即逐个比较路径上的点，直到分出大小)
        this.Paths = paths
            .OrderBy(p => p, new PathSequenceComparer())
            .ToArray();
    }

 

    public static int Dimension(int N, int N_Attacker) => (N + 2) * N_Attacker;




    // -------------------------------hash--------------------------------------

    public static bool operator ==(AttackerStrategy2 left, AttackerStrategy2 right) {
        if (ReferenceEquals(left, right)) return true;
        if (left.Paths.Length != right.Paths.Length) return false;
        for (int i = 0; i < left.Paths.Length; ++i) {
            if (!left.Paths[i].SequenceEqual(right.Paths[i])) {
                return false;
            }
        }
        return true;
    }

    public static bool operator !=(AttackerStrategy2 left, AttackerStrategy2 right) {
        return !(left == right);
    }

    public override bool Equals(object? obj) {
        if (obj is AttackerStrategy2 attackerStrategy) {
            return this == attackerStrategy;
        }
        return false;
    }

    public override int GetHashCode() {
        return Paths.Aggregate(0, (acc, path) => HashCode.Combine(acc, path.Aggregate(0, (acc, val) => HashCode.Combine(acc, val))));
    }


    /// <summary>
    /// 路径序列比较器：用于处理起点相同时的排序
    /// 逻辑：逐个比较路径上的点，类似字符串比较 "abc" vs "abd"
    /// </summary>
    private class PathSequenceComparer : IComparer<PointGeo2D[]> {
        private readonly PointGeo2DComparer _pointComparer = new PointGeo2DComparer();

        public int Compare(PointGeo2D[]? x, PointGeo2D[]? y) {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            int minLen = Math.Min(x.Length, y.Length);

            // 逐点比较
            for (int i = 0; i < minLen; i++) {
                int cmp = _pointComparer.Compare(x[i], y[i]);
                if (cmp != 0) {
                    return cmp; // 发现差异，直接返回结果
                }
            }

            // 如果前面的点都一样，较短的路径排在前面 (或者根据需求返回 0 表示视为相等)
            return x.Length.CompareTo(y.Length);
        }
    }

    /// <summary>
    /// 单点比较器：假设 PointGeo2D 包含 X, Y 坐标
    /// 如果 PointGeo2D 已实现 IComparable，可直接使用 Comparer<PointGeo2D>.Default
    /// </summary>
    private class PointGeo2DComparer : IComparer<PointGeo2D> {
        public int Compare(PointGeo2D x, PointGeo2D y) {
            // 先比 X，再比 Y
            int cmpX = x.Lat.CompareTo(y.Lat);
            if (cmpX != 0) return cmpX;

            return x.Lon.CompareTo(y.Lon);
        }
    }
}

