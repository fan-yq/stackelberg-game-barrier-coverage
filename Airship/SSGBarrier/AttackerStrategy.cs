using System.Collections;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Spatial.Euclidean;

namespace Airship.Deployment.SSGBarrier;

public sealed class AttackerStrategy {
    public PointGeo2D[] Paths { get; } // [attacker index, way points]

    /// <summary>
    /// 初始化攻击者策略
    /// </summary>
    /// <param name="paths">所有可能的路径集合</param>
    public AttackerStrategy(PointGeo2D[] paths) {
        // 排序逻辑：
        // 1. 第一优先级：路径的起点 (p[0])
        // 2. 第二优先级：如果起点相同，使用自定义比较器比较整条路径的字典序
        //    (即逐个比较路径上的点，直到分出大小)
        this.Paths = paths;
    }

    public IEnumerable<LineSegment2D> Lines() {
        for (int i = 0; i < Paths.Length - 1; ++i) {
            if (Paths[i] != Paths[i + 1]) {
                yield return new(Paths[i].ToPoint2D(), Paths[i + 1].ToPoint2D());
            }
        }
    }



    /// <summary>
    /// 前面2是起点和终点编码
    /// 后面N是路径点
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static AttackerStrategy Decode2(Vector<double> x, SSGBarrierEnv2 env) {
        int N = env.N_ControlPointOfAttacker;
        PointGeo2D[] path = new PointGeo2D[N + 2];
        int idx_source = (int)x[0], idx_sink = (int)x[1];
        path[0] = env.Sources[idx_source];
        path[^1] = env.Sinks[idx_sink];
        for (int j = 0; j < N; ++j) {
            path[j + 1] = new(x[2 + 2 * j], x[3 + 2 * j]);
        }

        return new(path);
    }



    // -------------------------------hash--------------------------------------

    public static bool operator ==(AttackerStrategy left, AttackerStrategy right) {
        if (ReferenceEquals(left, right)) return true;
        if (left.Paths.Length != right.Paths.Length) return false;
        if (!left.Paths.SequenceEqual(right.Paths)) {
            return false;
        }
        return true;
    }

    public static bool operator !=(AttackerStrategy left, AttackerStrategy right) {
        return !(left == right);
    }

    public override bool Equals(object? obj) {
        if (obj is AttackerStrategy attackerStrategy) {
            return this == attackerStrategy;
        }
        return false;
    }

    public override int GetHashCode() {
        return Paths.Aggregate(0, (acc, path) => HashCode.Combine(acc, path.GetHashCode()));
    }
}


public sealed record AttackerStrategySet<T>(HashSet<T> Set) : IReadOnlyList<T> {
    readonly List<T> List = Set.ToList();
    public T this[int index] => List[index];

    public int Count => Set.Count;

    public IEnumerator<T> GetEnumerator() {
        return List.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    public bool Add(T strategy) {
        if (Set.Add(strategy)) {
            List.Add(strategy);
            return true;
        } else {
            return false;
        }
    }

}