using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Airship.Deployment.SSGBarrier;

// 设置探测能力，步数等参数
public sealed class SSGBarrierSetting {
    public double Radius { get; } // 覆盖范围 unused
    public double K_detect { get; }
    public double K_anti { get; }
    public double K_anti_max { get; }
    public double d_anti { get; }
    public double dMaxAttacker { get; }
    public double LambdaPath { get; } // 导弹飞行长度的惩罚项，越长损失越大
    public double Kappa { get; }

    /// <summary>
    /// 初始化 PathGameSetting 的新实例
    /// </summary>
    /// <param name="radius">覆盖范围rad</param>
    /// <param name="k_Detect">探测系数1/rad^2 (K_detect)</param>
    /// <param name="k_Anti">反导系数1/rad (K_anti)</param>
    /// <param name="d_Anti">反导距离参数rad (d_anti)</param>
    /// <param name="dMaxAttacker">导弹最大飞行距离rad</param>
    [JsonConstructor]
    public SSGBarrierSetting(double radius, double k_Detect, double k_Anti, double d_Anti, double K_Anti_max, double dMaxAttacker, double lambdaPath = 0, double Kappa = 1) {
        Radius = radius;
        K_detect = k_Detect;
        K_anti = k_Anti;
        K_anti_max = K_Anti_max;
        d_anti = d_Anti;
        this.dMaxAttacker = dMaxAttacker;
        LambdaPath = lambdaPath;
        this.Kappa = Kappa;
    }

    public double DetectProb(double d) {
        Debug.Assert(d >= 0);
        // if (d <= Radius) {
        return Exp(-K_detect * d * d);
        // }
        // return 0;
    }

    // sigmoid(k_anti * (d-d_anti))
    // at d_anti p = 0.5
    public double AntiMissleProb(double d) {
        Debug.Assert(d >= 0);
        return K_anti_max / (1 + Exp(-K_anti * (d - d_anti)));
    }

}