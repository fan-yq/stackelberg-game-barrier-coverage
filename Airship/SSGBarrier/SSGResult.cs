namespace Airship.Deployment.SSGBarrier;

public class SSGResultBase<T> {
    public string? CaseName { get; set; }
    public string? Method { get; set; }
    public required AirshipMixedStrategy AirshipMixedStrategy { get; set; }
    public required AttackerStrategy[] AttackerStrategies { get; set; }
    public required T Env { get; set; }
    public required SSGBarrierSetting Setting { get; set; }
    public int Iteration { get; set; }
    public DateTime CreateAt { get; set; } = DateTime.Now;
}

public sealed class SSGResult2 : SSGResultBase<ISSGBarrierEnv> {
    public string ApiVersion => "SSGResult/v2";
    public AirshipStrategy2[] AirshipStrategies { get; set; } = [];
    public required double MaxMinU { get; set; }
    public required double MinMaxU { get; set; }
    public required double[][] UMatrix { get; set; }
}

public sealed class SSGResultG<T> : SSGResultBase<T> {
    public string ApiVersion => "SSGResult/v2";
    public AirshipStrategy2[] AirshipStrategies { get; set; } = [];
    public required double MaxMinU { get; set; }
    public required double MinMaxU { get; set; }
    public required double[][] UMatrix { get; set; }
}