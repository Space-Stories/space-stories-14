namespace Content.Client._Stories.Blackjack.UI;

public readonly record struct StoriesBlackjackButtonColors(Color Fill, Color Border, Color Title, Color Subtitle);

public static class StoriesBlackjackPalette
{
    public static readonly Color Backdrop = Color.FromHex("#10151c");
    public static readonly Color Panel = Color.FromHex("#0b0f14");
    public static readonly Color Glass = Color.FromHex("#0b0f14ee");
    public static readonly Color Felt = Color.FromHex("#1e5a3a");
    public static readonly Color FeltBright = Color.FromHex("#58b883");
    public static readonly Color Gold = Color.FromHex("#d9ae2a");
    public static readonly Color GoldBright = Color.FromHex("#fff2b0");
    public static readonly Color Text = Color.FromHex("#e6edf3");
    public static readonly Color Muted = Color.FromHex("#98a2ae");
    public static readonly Color Dim = Color.FromHex("#6e7681");
    public static readonly Color Win = Color.FromHex("#6fcf73");
    public static readonly Color Lose = Color.FromHex("#e5534b");
    public static readonly Color Shade = Color.FromHex("#000000");
    public static readonly Color StepCurrentFill = Color.FromHex("#18140a");
    public static readonly Color StepIdleBorder = Color.FromHex("#232a34");
    public static readonly Color BadgeFill = Color.FromHex("#10151c");
    public static readonly Color BadgeBorder = Color.FromHex("#2b3340");

    public static readonly StoriesBlackjackButtonColors[] PrimaryButton =
    [
        new(Color.FromHex("#1e5a3a"), Color.FromHex("#3f9468"), Color.FromHex("#f2fff6"), Color.FromHex("#b6e3c7")),
        new(Color.FromHex("#154229"), Color.FromHex("#2d7a52"), Color.FromHex("#d8f0e2"), Color.FromHex("#9cc9ae")),
        new(Color.FromHex("#27724a"), Color.FromHex("#6fd09a"), Color.FromHex("#ffffff"), Color.FromHex("#d3f2df")),
        new(Color.FromHex("#18241e"), Color.FromHex("#25332b"), Color.FromHex("#5f6e66"), Color.FromHex("#4a564f")),
    ];

    public static readonly StoriesBlackjackButtonColors[] AccentButton =
    [
        new(Color.FromHex("#14110a"), Color.FromHex("#d9ae2a"), Color.FromHex("#d9ae2a"), Color.FromHex("#a38a3a")),
        new(Color.FromHex("#0c0a05"), Color.FromHex("#a8861f"), Color.FromHex("#b8941f"), Color.FromHex("#7d6a2c")),
        new(Color.FromHex("#2a2208"), Color.FromHex("#fff2b0"), Color.FromHex("#fff2b0"), Color.FromHex("#d9c36b")),
        new(Color.FromHex("#101216"), Color.FromHex("#3b3828"), Color.FromHex("#6b6648"), Color.FromHex("#4d4a38")),
    ];

    public static readonly StoriesBlackjackButtonColors[] QuietButton =
    [
        new(Color.FromHex("#151b24"), Color.FromHex("#323b49"), Color.FromHex("#e6edf3"), Color.FromHex("#8b949e")),
        new(Color.FromHex("#0e1319"), Color.FromHex("#323b49"), Color.FromHex("#b9c1c9"), Color.FromHex("#6e7681")),
        new(Color.FromHex("#1f2835"), Color.FromHex("#56627a"), Color.FromHex("#ffffff"), Color.FromHex("#a8b1bb")),
        new(Color.FromHex("#0e1218"), Color.FromHex("#1b212b"), Color.FromHex("#4d5560"), Color.FromHex("#3f464f")),
    ];

    public static readonly StoriesBlackjackButtonColors[] LinkButton =
    [
        new(Color.Transparent, Color.Transparent, Color.FromHex("#98a2ae"), Color.FromHex("#6e7681")),
        new(Color.Transparent, Color.Transparent, Color.FromHex("#fff2b0"), Color.FromHex("#d9c36b")),
        new(Color.Transparent, Color.Transparent, Color.FromHex("#fff2b0"), Color.FromHex("#d9c36b")),
        new(Color.Transparent, Color.Transparent, Color.FromHex("#4d5560"), Color.FromHex("#3f464f")),
    ];
}
