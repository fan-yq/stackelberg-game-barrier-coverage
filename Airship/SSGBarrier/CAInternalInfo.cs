namespace Airship.Deployment.SSGBarrier;

public sealed record CAInternalInfo {
    public string Note { get; set; } = "default";
    public int PopSizeDef { get; set; }
    public int PopSizeAttForEachDef { get; set; }
    public int PopSizeAtt { get; set; }
    public DateTime CreateAt { get; set; } = DateTime.Now;
    public required List<double> MMU { get; set; }
    public required List<int> ChangeAttCount { get; set; }
    public required List<int> ChangeDefCount { get; set; }
}