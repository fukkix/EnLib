using System;
using System.Collections.Generic;
using System.Linq;
using EnLib.Core;
using Godot;

namespace EnLib.View;

/// <summary>
/// 一个工作台。接受拖入的卡，匹配配方，倒计时，产出。
/// Phase 1 用最朴素的"任意 N 张卡 → 试图匹配配方"逻辑；
/// 后续会拆 InputSlot / RequirementSlot 等更细致的槽位。
/// </summary>
public partial class VerbSlot : PanelContainer
{
    public Verb Verb { get; private set; }

    private Label _title = null!;
    private Label _status = null!;
    private VBoxContainer _cardsBox = null!;
    private ProgressBar _progress = null!;

    private readonly List<CardView> _cardViews = new();
    private Recipe? _activeRecipe;
    private float _elapsed;
    private bool _running;

    public static VerbSlot Make(Verb verb)
    {
        var v = new VerbSlot();
        v.Verb = verb;
        v.CustomMinimumSize = new Vector2(220, 280);
        v.MouseFilter = MouseFilterEnum.Stop;

        var bg = new StyleBoxFlat
        {
            BgColor = new Color("1a1a22"),
            BorderColor = new Color("6a5a3a"),
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            ContentMarginTop = 10,
            ContentMarginBottom = 10,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
        };
        v.AddThemeStyleboxOverride("panel", bg);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 6);
        v.AddChild(vbox);

        v._title = new Label { Text = $"【{LabelOf(verb)}】" };
        v._title.AddThemeColorOverride("font_color", new Color(0.92f, 0.88f, 0.75f));
        v._title.AddThemeFontSizeOverride("font_size", 16);
        vbox.AddChild(v._title);

        v._status = new Label { Text = "（拖卡入此）" };
        v._status.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.55f));
        v._status.AddThemeFontSizeOverride("font_size", 11);
        vbox.AddChild(v._status);

        v._cardsBox = new VBoxContainer();
        v._cardsBox.AddThemeConstantOverride("separation", 4);
        vbox.AddChild(v._cardsBox);

        v._progress = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 1,
            Step = 0.001,
            ShowPercentage = false,
            Visible = false,
            CustomMinimumSize = new Vector2(0, 10),
        };
        vbox.AddChild(v._progress);

        v.SetProcess(true);
        return v;
    }

    private static string LabelOf(Verb v) => v switch
    {
        Verb.Identify  => "辨识",
        Verb.Restore   => "修复",
        Verb.Sort      => "整理",
        Verb.Study     => "研究",
        Verb.Reception => "访客",
        _ => v.ToString(),
    };

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (_running || data.VariantType != Variant.Type.String) return false;
        var card = GameState.I.GetCard(data.AsString());
        return card != null && GameState.I.LocationOf(card) == Location.Inbox;
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        var id = data.AsString();
        var card = GameState.I.GetCard(id);
        if (card == null) return;
        if (_cardViews.Any(cv => cv.Card.Id == id)) return;

        GameState.I.SetLocation(card, Location.InSlot);

        var view = CardView.Make(card);
        _cardViews.Add(view);
        _cardsBox.AddChild(view);

        TryStartRecipe();
        RefreshStatus();
    }

    private void TryStartRecipe()
    {
        var cards = _cardViews.Select(v => v.Card).ToList();
        var recipe = GameState.I.FindRecipe(Verb, cards);
        if (recipe == null) return;

        _activeRecipe = recipe;
        _elapsed = 0;
        _running = true;
        _progress.Visible = true;
        _progress.Value = 0;
    }

    private void RefreshStatus()
    {
        if (_running) _status.Text = $"运行中：{_activeRecipe?.Id}";
        else if (_cardViews.Count == 0) _status.Text = "（拖卡入此）";
        else _status.Text = $"等待配方匹配（{_cardViews.Count} 张）";
    }

    public override void _Process(double delta)
    {
        if (!_running || _activeRecipe == null) return;
        _elapsed += (float)delta;
        var ratio = Math.Clamp(_elapsed / _activeRecipe.DurationSec, 0f, 1f);
        _progress.Value = ratio;
        if (ratio >= 1f) Finish();
    }

    private void Finish()
    {
        if (_activeRecipe == null) return;

        var cards = _cardViews.Select(v => v.Card).ToList();
        var ctx = new RecipeContext
        {
            InputCards = cards,
            AspectsTotal = AspectSet.Sum(cards.Select(c => c.Aspects)),
        };

        var outcome = _activeRecipe.PickOutcome(GameState.I.Rng);
        outcome.Effect?.Invoke(ctx);

        GD.Print($"[Verb {Verb}] outcome={outcome.Id}  cards={string.Join(",", cards.Select(c => c.DisplayName))}");

        // 消耗 charges-based 材料；活下来的卡归位 Inbox
        // （若 Effect 已把卡移走，例如整理→Shelf，则不动它）
        foreach (var c in cards)
        {
            if (c.Type == CardType.Material && c.Charges.HasValue)
            {
                c.Charges--;
                if (c.Charges <= 0)
                {
                    GameState.I.Remove(c);
                    continue;
                }
            }
            if (GameState.I.LocationOf(c) == Location.InSlot)
                GameState.I.SetLocation(c, Location.Inbox);
        }

        // 注册新生成的卡（默认进 Inbox）
        foreach (var sp in ctx.SpawnedCards) GameState.I.Register(sp);

        // 槽内的视图都释放（数据已归位 / 删除）
        foreach (var view in _cardViews) view.QueueFree();
        _cardViews.Clear();
        _activeRecipe = null;
        _running = false;
        _progress.Visible = false;
        RefreshStatus();

        EmitSignal(SignalName.RecipeFinished, outcome.Id);
    }

    [Signal]
    public delegate void RecipeFinishedEventHandler(string outcomeId);
}
