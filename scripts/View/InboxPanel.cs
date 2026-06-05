using System.Collections.Generic;
using System.Linq;
using EnLib.Core;
using Godot;

namespace EnLib.View;

/// <summary>
/// L2 待处理筐面板：顶部类型筛选 chips + 滚动列表。
/// 监听 GameState.CardsChanged 自动重建列表。
/// </summary>
public partial class InboxPanel : PanelContainer
{
    private HBoxContainer _filterRow = null!;
    private VBoxContainer _list = null!;
    private Label _countLabel = null!;

    private CardType? _filter = null;  // null = 全部
    private Button? _allChip;
    private readonly Dictionary<CardType, Button> _typeChips = new();

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(280, 0);

        var bg = new StyleBoxFlat
        {
            BgColor = new Color("13131a"),
            BorderColor = new Color("3a3a4a"),
            BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
            ContentMarginLeft = 10, ContentMarginRight = 10,
            ContentMarginTop = 10,  ContentMarginBottom = 10,
        };
        AddThemeStyleboxOverride("panel", bg);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);
        AddChild(vbox);

        // 标题 + 计数
        var header = new HBoxContainer();
        var title = new Label { Text = "待处理筐" };
        title.AddThemeColorOverride("font_color", new Color(0.85f, 0.78f, 0.6f));
        title.AddThemeFontSizeOverride("font_size", 15);
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(title);
        _countLabel = new Label();
        _countLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.55f));
        _countLabel.AddThemeFontSizeOverride("font_size", 11);
        header.AddChild(_countLabel);
        vbox.AddChild(header);

        // 筛选 chips
        _filterRow = new HBoxContainer();
        _filterRow.AddThemeConstantOverride("separation", 4);
        vbox.AddChild(_filterRow);
        BuildChips();

        // 滚动列表
        var scroll = new ScrollContainer();
        scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        vbox.AddChild(scroll);

        _list = new VBoxContainer();
        _list.AddThemeConstantOverride("separation", 3);
        _list.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.AddChild(_list);

        GameState.I.CardsChanged += Rebuild;
        Rebuild();
    }

    public override void _ExitTree()
    {
        if (GameState.I != null) GameState.I.CardsChanged -= Rebuild;
    }

    private void BuildChips()
    {
        AddChip(null, "全部");
        foreach (CardType t in System.Enum.GetValues(typeof(CardType)))
            AddChip(t, ChipLabel(t));
    }

    private void AddChip(CardType? type, string label)
    {
        var btn = new Button { Text = label, ToggleMode = true };
        btn.AddThemeFontSizeOverride("font_size", 11);
        btn.CustomMinimumSize = new Vector2(0, 24);
        btn.Pressed += () =>
        {
            _filter = type;
            UpdateChipStates();
            Rebuild();
        };
        _filterRow.AddChild(btn);
        if (type.HasValue) _typeChips[type.Value] = btn;
        else _allChip = btn;
        if (type == _filter) btn.ButtonPressed = true;
    }

    private void UpdateChipStates()
    {
        if (_allChip != null) _allChip.ButtonPressed = (_filter == null);
        foreach (var (t, b) in _typeChips) b.ButtonPressed = (_filter == t);
    }

    private static string ChipLabel(CardType t) => t switch
    {
        CardType.Book      => "典籍",
        CardType.Knowledge => "知识",
        CardType.Tool      => "工具",
        CardType.Material  => "材料",
        CardType.State     => "状态",
        CardType.Visitor   => "访客",
        CardType.Lead      => "契机",
        _ => t.ToString(),
    };

    private void Rebuild()
    {
        foreach (var child in _list.GetChildren()) child.QueueFree();

        var inbox = GameState.I.CardsAt(Location.Inbox).ToList();
        var filtered = (_filter == null ? inbox : inbox.Where(c => c.Type == _filter.Value))
            .OrderBy(c => c.Type)
            .ThenBy(c => c.DisplayName)
            .ToList();

        foreach (var c in filtered) _list.AddChild(CardRow.Make(c));
        _countLabel.Text = $"{filtered.Count} / {inbox.Count}";
    }
}
