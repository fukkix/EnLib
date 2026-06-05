using EnLib.Core;
using Godot;

namespace EnLib.View;

/// <summary>
/// L1 工作桌面：左 Inbox + 中央工作台 + 底 Shelf。
/// </summary>
public partial class Workspace : Control
{
    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var outer = new VBoxContainer
        {
            AnchorRight = 1, AnchorBottom = 1,
            OffsetLeft = 24, OffsetTop = 24,
            OffsetRight = -24, OffsetBottom = -24,
        };
        outer.AddThemeConstantOverride("separation", 16);
        AddChild(outer);

        // 上部：Inbox | Verbs
        var top = new HBoxContainer();
        top.AddThemeConstantOverride("separation", 16);
        top.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        top.SizeFlagsVertical = SizeFlags.ExpandFill;
        outer.AddChild(top);

        var inbox = new InboxPanel();
        top.AddChild(inbox);

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

        // 底部：Shelf
        var shelf = new ShelfPanel();
        outer.AddChild(shelf);
    }
}
