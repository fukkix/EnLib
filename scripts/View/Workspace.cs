using EnLib.Core;
using Godot;

namespace EnLib.View;

/// <summary>
/// L1 工作桌面：摆放工作台 + 散卡。
/// Phase 1 写死布局：左侧"待处理筐"(竖排卡) + 中央一个【修复】台。
/// </summary>
public partial class Workspace : Control
{
    private VBoxContainer _inbox = null!;
    private HBoxContainer _verbs = null!;

    public override void _Ready()
    {
        // 全屏容器
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var root = new HBoxContainer
        {
            AnchorRight = 1,
            AnchorBottom = 1,
            OffsetLeft = 24,
            OffsetTop = 24,
            OffsetRight = -24,
            OffsetBottom = -24,
        };
        root.AddThemeConstantOverride("separation", 24);
        AddChild(root);

        // —— 左侧抽屉 ——
        var inboxPanel = new PanelContainer { CustomMinimumSize = new Vector2(180, 0) };
        var inboxBg = new StyleBoxFlat
        {
            BgColor = new Color("13131a"),
            BorderColor = new Color("3a3a4a"),
            BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
            ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 12, ContentMarginBottom = 12,
        };
        inboxPanel.AddThemeStyleboxOverride("panel", inboxBg);
        root.AddChild(inboxPanel);

        _inbox = new VBoxContainer();
        _inbox.AddThemeConstantOverride("separation", 8);
        inboxPanel.AddChild(_inbox);

        var inboxTitle = new Label { Text = "待处理筐" };
        inboxTitle.AddThemeColorOverride("font_color", new Color(0.7f, 0.65f, 0.55f));
        _inbox.AddChild(inboxTitle);

        // —— 中央工作台区 ——
        _verbs = new HBoxContainer();
        _verbs.AddThemeConstantOverride("separation", 16);
        _verbs.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _verbs.SizeFlagsVertical = SizeFlags.ExpandFill;
        root.AddChild(_verbs);

        // —— 用 GameState 的演示数据填充 ——
        foreach (var card in GameState.I.AllCards)
            _inbox.AddChild(CardView.Make(card));

        _verbs.AddChild(VerbSlot.Make(Verb.Restore));
    }
}
