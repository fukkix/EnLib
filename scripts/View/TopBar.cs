using EnLib.Core;
using Godot;

namespace EnLib.View;

/// <summary>
/// 顶栏：夜晚计数、血饥指针、暂停状态。
/// </summary>
public partial class TopBar : PanelContainer
{
    private Label _night = null!;
    private Label _thirst = null!;
    private ProgressBar _thirstBar = null!;
    private Label _pauseHint = null!;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(0, 36);

        var bg = new StyleBoxFlat
        {
            BgColor = new Color("0a0a10"),
            BorderColor = new Color("3a2a1a"),
            BorderWidthBottom = 1,
            ContentMarginLeft = 16, ContentMarginRight = 16,
            ContentMarginTop = 6,   ContentMarginBottom = 6,
        };
        AddThemeStyleboxOverride("panel", bg);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 20);
        AddChild(row);

        _night = MakeLabel(new Color(0.85f, 0.78f, 0.6f), 13);
        row.AddChild(_night);

        var thirstWrap = new HBoxContainer();
        thirstWrap.AddThemeConstantOverride("separation", 8);
        thirstWrap.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(thirstWrap);

        _thirst = MakeLabel(new Color(0.7f, 0.5f, 0.55f), 12);
        thirstWrap.AddChild(_thirst);

        _thirstBar = new ProgressBar
        {
            MinValue = 0, MaxValue = 100, ShowPercentage = false,
            CustomMinimumSize = new Vector2(160, 14),
        };
        thirstWrap.AddChild(_thirstBar);

        _pauseHint = MakeLabel(new Color(0.95f, 0.7f, 0.4f), 12);
        row.AddChild(_pauseHint);

        GameState.I.ClockTicked += Refresh;
        Refresh();
    }

    public override void _ExitTree()
    {
        if (GameState.I != null) GameState.I.ClockTicked -= Refresh;
    }

    private void Refresh()
    {
        var gs = GameState.I;
        _night.Text = $"夜 {gs.NightNumber}";
        _thirst.Text = $"血饥 {gs.BloodThirst:F0}/100";
        _thirstBar.Value = gs.BloodThirst;
        _pauseHint.Text = gs.Paused ? "⏸ 暂停（空格继续）" : "";
    }

    private static Label MakeLabel(Color c, int size)
    {
        var l = new Label();
        l.AddThemeColorOverride("font_color", c);
        l.AddThemeFontSizeOverride("font_size", size);
        return l;
    }
}
