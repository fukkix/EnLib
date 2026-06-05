using EnLib.Core;
using Godot;

namespace EnLib.View;

/// <summary>
/// L1 工作桌面：左侧 InboxPanel，中央工作台区。
/// </summary>
public partial class Workspace : Control
{
    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var root = new HBoxContainer
        {
            AnchorRight = 1, AnchorBottom = 1,
            OffsetLeft = 24, OffsetTop = 24,
            OffsetRight = -24, OffsetBottom = -24,
        };
        root.AddThemeConstantOverride("separation", 24);
        AddChild(root);

        // 左侧抽屉
        var inbox = new InboxPanel();
        root.AddChild(inbox);

        // 中央工作台区
        var verbs = new HBoxContainer();
        verbs.AddThemeConstantOverride("separation", 16);
        verbs.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        verbs.SizeFlagsVertical = SizeFlags.ExpandFill;
        root.AddChild(verbs);

        verbs.AddChild(VerbSlot.Make(Verb.Restore));
        verbs.AddChild(VerbSlot.Make(Verb.Restore));  // 暂时放两个修复台演示并行
    }
}
