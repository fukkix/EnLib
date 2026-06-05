using EnLib.Core;
using Godot;

namespace EnLib.View;

/// <summary>
/// 待处理筐里的一张紧凑卡：单行 + 类型色条 + 标题 + 副标 + aspects。
/// 拖拽 payload 与 CardView 相同（Card.Id 字符串）。
/// </summary>
public partial class CardRow : PanelContainer
{
    public Card Card { get; private set; } = null!;

    private static readonly System.Collections.Generic.Dictionary<CardType, Color> AccentColors = new()
    {
        { CardType.Book,      new Color("8a6a3a") },
        { CardType.Knowledge, new Color("5a7aa0") },
        { CardType.Tool,      new Color("8a8a8a") },
        { CardType.Material,  new Color("6aa06a") },
        { CardType.State,     new Color("8a6aa0") },
        { CardType.Visitor,   new Color("c0a050") },
        { CardType.Lead,      new Color("c05050") },
    };

    public static CardRow Make(Card card)
    {
        var r = new CardRow();
        r.Card = card;
        r.MouseFilter = MouseFilterEnum.Stop;
        r.CustomMinimumSize = new Vector2(0, 36);

        var bg = new StyleBoxFlat
        {
            BgColor = new Color("1a1a22"),
            BorderColor = AccentColors[card.Type],
            BorderWidthLeft = 4,
            ContentMarginLeft = 8, ContentMarginRight = 8,
            ContentMarginTop = 4,  ContentMarginBottom = 4,
            CornerRadiusTopLeft = 2, CornerRadiusBottomLeft = 2,
        };
        r.AddThemeStyleboxOverride("panel", bg);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        r.AddChild(row);

        var title = new Label { Text = card.DisplayTitle };
        title.AddThemeColorOverride("font_color", new Color(0.92f, 0.88f, 0.78f));
        title.AddThemeFontSizeOverride("font_size", 13);
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(title);

        var meta = new Label { Text = MetaFor(card) };
        meta.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.6f));
        meta.AddThemeFontSizeOverride("font_size", 10);
        row.AddChild(meta);

        return r;
    }

    private static string MetaFor(Card c) => c.Type switch
    {
        CardType.Book                              => $"{c.Condition}",
        CardType.Material when c.Charges.HasValue  => $"×{c.Charges}",
        _                                          => c.Type.ToString(),
    };

    public override Variant _GetDragData(Vector2 atPosition)
    {
        var preview = CardView.Make(Card);
        preview.Modulate = new Color(1, 1, 1, 0.75f);
        SetDragPreview(preview);
        return Card.Id;
    }
}
