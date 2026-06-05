using EnLib.Core;
using Godot;

namespace EnLib.View;

/// <summary>
/// 一张卡的视觉表现。可拖拽。
/// 拖拽 payload 是 Card.Id（字符串）；接收端通过 GameState 查回 Card。
/// </summary>
public partial class CardView : PanelContainer
{
    public Card Card { get; private set; } = null!;

    private Label _title = null!;
    private Label _subtitle = null!;
    private Label _aspectsLabel = null!;

    private static readonly System.Collections.Generic.Dictionary<CardType, Color> TypeColors = new()
    {
        { CardType.Book,      new Color("4a3520") },
        { CardType.Knowledge, new Color("2a3a55") },
        { CardType.Tool,      new Color("3a3a3a") },
        { CardType.Material,  new Color("2a4a2a") },
        { CardType.State,     new Color("3a2a4a") },
        { CardType.Visitor,   new Color("5a4a20") },
        { CardType.Lead,      new Color("5a2020") },
    };

    public static CardView Make(Card card)
    {
        var v = new CardView();
        v.Card = card;
        v.CustomMinimumSize = new Vector2(140, 90);
        v.MouseFilter = MouseFilterEnum.Stop;

        var bg = new StyleBoxFlat
        {
            BgColor = TypeColors[card.Type],
            BorderColor = new Color("8a6a3a"),
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            ContentMarginTop = 6,
            ContentMarginBottom = 6,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
        };
        v.AddThemeStyleboxOverride("panel", bg);

        var vbox = new VBoxContainer();
        v.AddChild(vbox);

        v._title = new Label { Text = card.DisplayName };
        v._title.AddThemeColorOverride("font_color", new Color(0.95f, 0.92f, 0.85f));
        vbox.AddChild(v._title);

        v._subtitle = new Label();
        v._subtitle.AddThemeColorOverride("font_color", new Color(0.7f, 0.65f, 0.55f));
        v._subtitle.AddThemeFontSizeOverride("font_size", 11);
        vbox.AddChild(v._subtitle);

        v._aspectsLabel = new Label();
        v._aspectsLabel.AddThemeColorOverride("font_color", new Color(0.55f, 0.6f, 0.7f));
        v._aspectsLabel.AddThemeFontSizeOverride("font_size", 10);
        vbox.AddChild(v._aspectsLabel);

        v.Refresh();
        return v;
    }

    public void Refresh()
    {
        _title.Text = Card.DisplayName;
        _subtitle.Text = SubtitleFor(Card);
        _aspectsLabel.Text = Card.Aspects.ToString();
    }

    private static string SubtitleFor(Card c) => c.Type switch
    {
        CardType.Book => $"{c.Type} · {c.Condition}",
        CardType.Material when c.Charges.HasValue => $"{c.Type} ×{c.Charges}",
        _ => c.Type.ToString(),
    };

    public override Variant _GetDragData(Vector2 atPosition)
    {
        var preview = Make(Card);
        preview.Modulate = new Color(1, 1, 1, 0.7f);
        SetDragPreview(preview);
        return Card.Id;
    }
}
