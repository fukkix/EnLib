using System.Linq;
using EnLib.Core;
using Godot;

namespace EnLib.View;

/// <summary>
/// L3 馆藏书架：底栏横向滚动条，每个已上架的系列显示为一个函套图标。
/// </summary>
public partial class ShelfPanel : PanelContainer
{
    private HBoxContainer _stacks = null!;
    private Label _emptyHint = null!;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(0, 96);

        var bg = new StyleBoxFlat
        {
            BgColor = new Color("0d0d14"),
            BorderColor = new Color("3a2a1a"),
            BorderWidthTop = 2,
            ContentMarginLeft = 12, ContentMarginRight = 12,
            ContentMarginTop = 8,   ContentMarginBottom = 8,
        };
        AddThemeStyleboxOverride("panel", bg);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 4);
        AddChild(vbox);

        var title = new Label { Text = "馆藏书架" };
        title.AddThemeColorOverride("font_color", new Color(0.78f, 0.65f, 0.4f));
        title.AddThemeFontSizeOverride("font_size", 12);
        vbox.AddChild(title);

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            VerticalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        vbox.AddChild(scroll);

        _stacks = new HBoxContainer();
        _stacks.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(_stacks);

        _emptyHint = new Label { Text = "尚无入藏的套书。" };
        _emptyHint.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.45f));
        _emptyHint.AddThemeFontSizeOverride("font_size", 11);
        _stacks.AddChild(_emptyHint);

        GameState.I.CardsChanged += Rebuild;
        Rebuild();
    }

    public override void _ExitTree()
    {
        if (GameState.I != null) GameState.I.CardsChanged -= Rebuild;
    }

    private void Rebuild()
    {
        foreach (var child in _stacks.GetChildren()) child.QueueFree();

        var shelved = GameState.I.CardsAt(Location.Shelf)
            .Where(c => c.Type == CardType.Book && c.SeriesId != null)
            .GroupBy(c => c.SeriesId!)
            .ToList();

        if (shelved.Count == 0)
        {
            _emptyHint = new Label { Text = "尚无入藏的套书。" };
            _emptyHint.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.45f));
            _emptyHint.AddThemeFontSizeOverride("font_size", 11);
            _stacks.AddChild(_emptyHint);
            return;
        }

        foreach (var group in shelved)
        {
            var members = group.OrderBy(b => b.VolumeNo).ToList();
            var seriesName = members.FirstOrDefault(m => m.Identified)?.DisplayName.Split('·')[0]
                             ?? group.Key;
            _stacks.AddChild(MakeStack(seriesName, members.Count, members.First().SeriesTotal ?? members.Count));
        }
    }

    private static Control MakeStack(string seriesName, int count, int total)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(120, 60) };
        var bg = new StyleBoxFlat
        {
            BgColor = new Color("2a2018"),
            BorderColor = new Color("a08050"),
            BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
            CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3,
            ContentMarginLeft = 8, ContentMarginRight = 8,
            ContentMarginTop = 6, ContentMarginBottom = 6,
        };
        panel.AddThemeStyleboxOverride("panel", bg);

        var vbox = new VBoxContainer();
        panel.AddChild(vbox);

        var name = new Label { Text = seriesName };
        name.AddThemeColorOverride("font_color", new Color(0.92f, 0.85f, 0.7f));
        name.AddThemeFontSizeOverride("font_size", 13);
        vbox.AddChild(name);

        var info = new Label { Text = $"全 {total} 卷" };
        info.AddThemeColorOverride("font_color", new Color(0.6f, 0.55f, 0.45f));
        info.AddThemeFontSizeOverride("font_size", 10);
        vbox.AddChild(info);

        return panel;
    }
}
