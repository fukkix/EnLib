using System;
using System.Collections.Generic;
using Godot;

namespace EnLib.Core;

/// <summary>
/// 全局游戏状态：持有所有卡牌、所有配方、随机种子。
/// 通过 Autoload 注入（project.godot 已配置 `GameState`）。
/// 视图层通过 `GameState.I` 静态访问。
/// </summary>
public partial class GameState : Node
{
    public static GameState I { get; private set; } = null!;

    public Random Rng { get; private set; } = new();

    private readonly Dictionary<string, Card> _cards = new();
    private readonly Dictionary<string, Location> _locations = new();
    private readonly List<Recipe> _recipes = new();

    public IReadOnlyCollection<Card> AllCards => _cards.Values;
    public IReadOnlyList<Recipe> Recipes => _recipes;

    /// <summary>任意"卡集合"发生变化时触发：增删卡、location 改变。</summary>
    [Signal]
    public delegate void CardsChangedEventHandler();

    public override void _EnterTree()
    {
        I = this;
    }

    public override void _Ready()
    {
        SeedDemoData();
        GD.Print($"[GameState] Ready. {_cards.Count} cards, {_recipes.Count} recipes.");
    }

    private bool _suppressSignals;

    public Card Register(Card c, Location loc = Location.Inbox)
    {
        _cards[c.Id] = c;
        _locations[c.Id] = loc;
        if (!_suppressSignals) EmitSignal(SignalName.CardsChanged);
        return c;
    }

    public Card? GetCard(string id) => _cards.GetValueOrDefault(id);

    public Location LocationOf(Card c)
        => _locations.TryGetValue(c.Id, out var l) ? l : Location.Inbox;

    public void SetLocation(Card c, Location loc)
    {
        if (!_locations.ContainsKey(c.Id)) return;
        if (_locations[c.Id] == loc) return;
        _locations[c.Id] = loc;
        if (!_suppressSignals) EmitSignal(SignalName.CardsChanged);
    }

    public IEnumerable<Card> CardsAt(Location loc)
    {
        foreach (var (id, l) in _locations)
            if (l == loc && _cards.TryGetValue(id, out var c)) yield return c;
    }

    public void Remove(Card c)
    {
        _cards.Remove(c.Id);
        _locations.Remove(c.Id);
        if (!_suppressSignals) EmitSignal(SignalName.CardsChanged);
    }

    public Recipe? FindRecipe(Verb verb, IReadOnlyList<Card> inputs)
    {
        foreach (var r in _recipes)
            if (r.Verb == verb && r.Matches(inputs)) return r;
        return null;
    }

    // ────────────────────────────────────────────────────────────
    // Phase 1 演示数据：手写一个修复配方 + 三张测试卡。
    // 等数据驱动到位后整段会被 YAML 加载取代。
    // ────────────────────────────────────────────────────────────
    private void SeedDemoData()
    {
        _suppressSignals = true;

        // —— 典籍（带不同 series / condition / aspect 组合，方便测试筛选）——
        Book("银器禁令·卷一", "silver-edict", 1, 3,
             BookCondition.Worn,    (Aspect.Silver, 2), (Aspect.Night, 1), (Aspect.Decay, 1));
        Book("银器禁令·卷二", "silver-edict", 2, 3,
             BookCondition.Damaged, (Aspect.Silver, 2), (Aspect.Night, 1), (Aspect.Decay, 2));
        Book("银器禁令·卷三", "silver-edict", 3, 3,
             BookCondition.Fragment,(Aspect.Silver, 3), (Aspect.Night, 1), (Aspect.Decay, 3));
        Book("夜行邮政员的劳动条件", null, null, null,
             BookCondition.Good,    (Aspect.Night, 2), (Aspect.Lore, 1));
        Book("镜中的低语·案件录", "mirror-whispers", 1, 2,
             BookCondition.Worn,    (Aspect.Mortal, 1), (Aspect.Secret, 2), (Aspect.Decay, 1));
        Book("永生者税务局十年回顾", null, null, null,
             BookCondition.Damaged, (Aspect.Night, 2), (Aspect.Lore, 2), (Aspect.Decay, 1));
        Book("远古纪事·序卷", "ancient-chronicle", 0, 7,
             BookCondition.Fragment,(Aspect.Ancient, 3), (Aspect.Memory, 1), (Aspect.Decay, 3));
        Book("关于\"为什么不见日光\"的常见误解", null, null, null,
             BookCondition.Pristine,(Aspect.Sun, 2), (Aspect.Lore, 1));

        // —— 知识 ——
        Knowledge("装帧史·初级", 1, (Aspect.Craft, 1), (Aspect.Lore, 1));
        Knowledge("银器化学", 1,   (Aspect.Silver, 1), (Aspect.Lore, 1));
        Knowledge("古拉丁文",  1,   (Aspect.Ancient, 1), (Aspect.Lore, 2));

        // —— 工具（耐用品）——
        Tool("牛骨折页刀", (Aspect.Craft, 2));
        Tool("铜版烫金机", (Aspect.Craft, 2), (Aspect.Memory, 1));

        // —— 材料（消耗品）——
        Material("鞣酸纸", 3, (Aspect.Craft, 1), (Aspect.Silver, 1));
        Material("桑皮纸", 5, (Aspect.Craft, 1));
        Material("阿拉伯树胶", 2, (Aspect.Craft, 1), (Aspect.Ancient, 1));

        // —— 访客 / 契机 ——
        Visitor("信使乌鸦", "缓解血饥；偶尔带来契机。", (Aspect.Blood, 1));
        Lead("关于卷七", "馆长留下的纸条，让你别打开它。", (Aspect.Secret, 3), (Aspect.Memory, 2));

        _suppressSignals = false;

        // —— 一个修复配方 ——
        _recipes.Add(new Recipe
        {
            Id = "restore_basic",
            Verb = Verb.Restore,
            Requires = new[]
            {
                new AspectRequirement(Aspect.Decay, 1),
                new AspectRequirement(Aspect.Craft, 2),
            },
            DurationSec = 5f,
            Outcomes = new[]
            {
                new Outcome
                {
                    Id = "success",
                    Weight = 70,
                    TextPoolId = "restore.success",
                    Effect = ctx =>
                    {
                        var b = ctx.FirstOf(CardType.Book);
                        if (b != null && b.Condition < BookCondition.Pristine)
                            b.Condition += 1;
                    },
                },
                new Outcome
                {
                    Id = "jackpot",
                    Weight = 10,
                    TextPoolId = "restore.jackpot",
                    Effect = ctx =>
                    {
                        var b = ctx.FirstOf(CardType.Book);
                        if (b != null)
                        {
                            var leap = (int)b.Condition + 2;
                            b.Condition = (BookCondition)Math.Min(leap, (int)BookCondition.Pristine);
                        }
                    },
                },
                new Outcome
                {
                    Id = "flaw",
                    Weight = 20,
                    TextPoolId = "restore.flaw",
                    Effect = ctx =>
                    {
                        // 留个小瑕疵：condition 不变，但生成一张"瑕疵记录"状态卡
                        var b = ctx.FirstOf(CardType.Book);
                        if (b != null)
                        {
                            var state = Card.New(CardType.State, $"瑕疵·{b.DisplayName}");
                            state.Description = "夹缝里始终留着一道淡灰的痕。也许只有你会注意到。";
                            ctx.SpawnedCards.Add(state);
                        }
                    },
                },
            },
        });
    }

    // —— Seeding helpers ——
    private Card Book(string title, string? series, int? volNo, int? seriesTotal,
                      BookCondition cond, params (Aspect a, int v)[] aspects)
    {
        var c = Card.New(CardType.Book, title);
        c.Condition = cond;
        c.SeriesId = series;
        c.VolumeNo = volNo;
        if (seriesTotal.HasValue) c.Description = $"全 {seriesTotal} 卷之一";
        foreach (var (a, v) in aspects) c.Aspects.Add(a, v);
        return Register(c);
    }

    private Card Knowledge(string title, int level, params (Aspect a, int v)[] aspects)
    {
        var c = Card.New(CardType.Knowledge, $"{title}·{level} 级");
        foreach (var (a, v) in aspects) c.Aspects.Add(a, v);
        return Register(c);
    }

    private Card Tool(string title, params (Aspect a, int v)[] aspects)
    {
        var c = Card.New(CardType.Tool, title);
        c.Charges = null;  // 耐用
        foreach (var (a, v) in aspects) c.Aspects.Add(a, v);
        return Register(c);
    }

    private Card Material(string title, int charges, params (Aspect a, int v)[] aspects)
    {
        var c = Card.New(CardType.Material, title);
        c.Charges = charges;
        foreach (var (a, v) in aspects) c.Aspects.Add(a, v);
        return Register(c);
    }

    private Card Visitor(string title, string desc, params (Aspect a, int v)[] aspects)
    {
        var c = Card.New(CardType.Visitor, title);
        c.Description = desc;
        foreach (var (a, v) in aspects) c.Aspects.Add(a, v);
        return Register(c);
    }

    private Card Lead(string title, string desc, params (Aspect a, int v)[] aspects)
    {
        var c = Card.New(CardType.Lead, title);
        c.Description = desc;
        foreach (var (a, v) in aspects) c.Aspects.Add(a, v);
        return Register(c);
    }
}
