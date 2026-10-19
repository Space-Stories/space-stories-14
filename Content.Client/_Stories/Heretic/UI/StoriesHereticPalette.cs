namespace Content.Client._Stories.Heretic.UI;

public readonly record struct StoriesHereticPalette(Color Accent, Color Dark, string NodeState)
{
    public static readonly StoriesHereticPalette General = new(Color.FromHex("#6fcf73"), Color.FromHex("#122a17"), "node_side");

    private static readonly Dictionary<string, StoriesHereticPalette> Paths = new()
    {
        ["STHereticPathAsh"] = new(Color.FromHex("#e8742a"), Color.FromHex("#3a1d10"), "node_ash"),
        ["STHereticPathFlesh"] = new(Color.FromHex("#d03a48"), Color.FromHex("#3a0d14"), "node_flesh"),
        ["STHereticPathVoid"] = new(Color.FromHex("#7fd4f5"), Color.FromHex("#0f2633"), "node_void"),
        ["STHereticPathRust"] = new(Color.FromHex("#b8693a"), Color.FromHex("#2e1a0f"), "node_rust"),
        ["STHereticPathBlade"] = new(Color.FromHex("#c9d1d9"), Color.FromHex("#23272d"), "node_blade"),
        ["STHereticPathCosmos"] = new(Color.FromHex("#a56ee6"), Color.FromHex("#21113a"), "node_cosmos"),
        ["STHereticPathLock"] = new(Color.FromHex("#e8c547"), Color.FromHex("#382c0b"), "node_lock"),
        ["STHereticPathMoon"] = new(Color.FromHex("#b9d3f7"), Color.FromHex("#1b2337"), "node_moon"),
    };

    public static StoriesHereticPalette Get(string? path)
    {
        return path != null && Paths.TryGetValue(path, out var palette) ? palette : General;
    }
}
