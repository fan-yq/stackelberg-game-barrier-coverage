using System.Text.Json.Serialization;
using Aircraft.Core.ConvexHull;

namespace Airship.Deployment.SSGBarrier;

// 定义博弈区域
// 目标的分布和价值
// 离散网格设置，后续可能会改为连续网格等
public sealed class SSGBarrierEnv2 : ISSGBarrierEnv {
    [JsonPropertyName("DeployArea1")]
    [JsonConverter(typeof(ConvexHullDConverter))]
    public ConvexHullD DeployArea { get; }
    [JsonPropertyName("MissleArea1")]
    [JsonConverter(typeof(ConvexHullDConverter))]
    public ConvexHullD MissleArea { get; }
    public int N_Airship { get; }
    public int N_ControlPointOfAttacker { get; }
    public PointGeo2D[] Sources { get; }
    public PointGeo2D[] Sinks { get; }
    public PointGeo2D[] AntiMissles { get; } // 反导位置
    public double[] SinkValues1 { get; }
    public PointGeo2DSortMode PointSortMode { get; }
    [JsonConstructor]
    public SSGBarrierEnv2(
        ConvexHullD deployArea,
        ConvexHullD missleArea,
        int n_Airship,
        int n_ControlPointOfAttacker,
        PointGeo2D[] sources,
        PointGeo2D[] sinks,
        PointGeo2D[] antiMissles,
        double[] sinkValues1,
        PointGeo2DSortMode pointSortMode = PointGeo2DSortMode.Lat) {
        DeployArea = deployArea;
        MissleArea = missleArea;
        N_Airship = n_Airship;
        N_ControlPointOfAttacker = n_ControlPointOfAttacker;
        Sources = sources.ToArray();
        Sinks = sinks.ToArray();
        AntiMissles = antiMissles.ToArray();
        SinkValues1 = sinkValues1;
        PointSortMode = pointSortMode;
    }
}