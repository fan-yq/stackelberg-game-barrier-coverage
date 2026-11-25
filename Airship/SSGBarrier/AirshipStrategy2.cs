using System.Text.Json.Serialization;
using MathNet.Numerics.LinearAlgebra;

namespace Airship.Deployment.SSGBarrier;


public sealed class AirshipStrategy2 {
    public PointGeo2D[] Deployments { get; }
    public AirshipStrategy2(PointGeo2D[] deployments, PointGeo2DSortMode mode) {
        var comparer = PointGeo2DComparerFactory.Create(mode);
        Deployments = deployments.OrderBy(p => p, comparer).ToArray();
    }

    /// <summary>
    /// only for serialization
    /// </summary>
    [JsonConstructor]
    public AirshipStrategy2(PointGeo2D[] deployments) {
        Deployments = deployments;
    }

    /// <summary>
    /// 前面2M是起点和终点编码
    /// 后面NM是路径点编码
    /// </summary>
    /// <param name="x"></param>
    /// <param name="env"></param>
    public static AirshipStrategy2 Decode2(Vector<double> x, SSGBarrierEnv2 env) {
        PointGeo2D[] deployments = new PointGeo2D[env.N_Airship];
        int NA = env.N_Airship;
        for (int j = 0; j < NA; ++j) {
            double lat = x[j * 2], lon = x[j * 2 + 1];
            deployments[j] = new(lat, lon);
        }

        return new(deployments, env.PointSortMode);
    }


    public AirshipMixedStrategy ToMixdStrategy() {
        return new(Deployments, Enumerable.Repeat(1.0, Deployments.Length).ToArray(), Deployments.Length);
    }

}