using System.Text.Json.Serialization;
using Aircraft.Core.ConvexHull;

namespace Airship.Deployment.SSGBarrier;

[JsonDerivedType(typeof(SSGBarrierEnv2))]
public interface ISSGBarrierEnv {
    public int N_Airship { get; }
    public PointGeo2D[] Sources { get; }
    public PointGeo2D[] Sinks { get; }
    public PointGeo2D[] AntiMissles { get; } // 反导位置
    public double[] SinkValues1 { get; }
    public PointGeo2DSortMode PointSortMode { get; }
    [JsonPropertyName("DeployArea1")]
    [JsonConverter(typeof(ConvexHullDConverter))]
    public ConvexHullD DeployArea { get; }
    [JsonPropertyName("MissleArea1")]
    [JsonConverter(typeof(ConvexHullDConverter))]
    public ConvexHullD MissleArea { get; }

    public Dictionary<PointGeo2D, double> SinkValues() {
        var sinkValues1 = new Dictionary<PointGeo2D, double>();
        for (int i = 0; i < Sinks.Length; ++i) {
            sinkValues1[Sinks[i]] = SinkValues1[i];
        }
        return sinkValues1;
    }
    public Dictionary<PointGeo2D, int> SinkIndices() {
        var sinkValues1 = new Dictionary<PointGeo2D, int>();
        for (int i = 0; i < Sinks.Length; ++i) {
            sinkValues1[Sinks[i]] = i;
        }
        return sinkValues1;
    }
}