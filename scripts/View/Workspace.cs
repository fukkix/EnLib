using EnLib.Core;
using Godot;

namespace EnLib.View;

/// <summary>
/// L1 工作桌面：顶 TopBar + 中部 [Inbox | Verbs] + 底 Shelf。
/// </summary>
public partial class Workspace : Control
{
    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var outer = new VBoxContainer
        {
            AnchorRight = 1, AnchorBottom = 1,
        };
        outer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        outer.AddThemeConstantOverride("separation", 0);
        AddChild(outer);

        outer.AddChild(new TopBar());

        // 中间内容（左右留白）
        var midWrap = new MarginContainer();
        midWrap.AddThemeConstantOverride("margin_left", 24);
        midWrap.AddThemeConstantOverride("margin_right", 24);
        midWrap.AddThemeConstantOverride("margin_top", 16);
        midWrap.AddThemeConstantOverride("margin_bottom", 8);
        midWrap.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        midWrap.SizeFlagsVertical = SizeFlags.ExpandFill;
        outer.AddChild(midWrap);

        var midInner = new VBoxContainer();
        midInner.AddThemeConstantOverride("separation", 16);
        midWrap.AddChild(midInner);

        // [Inbox | Verbs]
        var top = new HBoxContainer();
        top.AddThemeConstantOverride("separation", 16);
        top.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        top.SizeFlagsVertical = SizeFlags.ExpandFill;
        midInner.AddChild(top);

        top.AddChild(new InboxPanel());

        var verbs = new HFlowContainer();
        verbs.AddThemeConstantOverride("h_separation", 12);
        verbs.AddThemeConstantOverride("v_separation", 12);
        verbs.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        verbs.SizeFlagsVertical = SizeFlags.ExpandFill;
        top.AddChild(verbs);

        verbs.AddChild(VerbSlot.Make(Verb.Identify));
        verbs.AddChild(VerbSlot.Make(Verb.Restore));
        verbs.AddChild(VerbSlot.Make(Verb.Restore));
        verbs.AddChild(VerbSlot.Make(Verb.Sort));

        midInner.AddChild(new ShelfPanel());

        SetProcessUnhandledInput(true);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("toggle_pause"))
        {
            GameState.I.TogglePaused();
            GetViewport().SetInputAsHandled();
        }
    }
}
