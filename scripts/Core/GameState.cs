using System;
using System.Collections.Generic;
using System.Linq;
using EnLib.Data;
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

        // —— 从 YAML 加载卡牌 ——
        var specs = CardLoader.LoadFile("res://data/cards.yaml");
        foreach (var spec in specs)
        {
            try { Register(CardLoader.SpecToCard(spec)); }
            catch (Exception e) { GD.PrintErr($"[Seed] 跳过 \"{spec.Name}\": {e.Message}"); }
        }

        _suppressSignals = false;

        // —— 辨识配方 ——
        _recipes.Add(new Recipe
        {
            Id = "identify_basic",
            Verb = Verb.Identify,
            Requires = new[] { new AspectRequirement(Aspect.Lore, 1) },
            ExtraMatch = cards => cards.Any(c => c.Type == CardType.Book && !c.Identified),
            DurationSec = 6f,
            Outcomes = new[]
            {
                new Outcome
                {
                    Id = "identified",
                    Weight = 100,
                    TextPoolId = "identify.success",
                    Effect = ctx =>
                    {
                        foreach (var c in ctx.InputCards)
                            if (c.Type == CardType.Book && !c.Identified) c.Identified = true;
                    },
                },
            },
        });

        // —— 整理配方 ——
        _recipes.Add(new Recipe
        {
            Id = "sort_basic",
            Verb = Verb.Sort,
            ExtraMatch = cards =>
            {
                var books = cards.Where(c => c.Type == CardType.Book && c.SeriesId != null).ToList();
                return books.GroupBy(b => b.SeriesId).Any(g => g.Count() >= 2);
            },
            DurationSec = 3f,
            Outcomes = new[]
            {
                new Outcome
                {
                    Id = "sort_complete",
                    Weight = 100,
                    TextPoolId = "sort.result",
                    Effect = ctx =>
                    {
                        var books = ctx.InputCards
                            .Where(c => c.Type == CardType.Book && c.SeriesId != null)
                            .ToList();
                        var biggestGroup = books
                            .GroupBy(b => b.SeriesId!)
                            .OrderByDescending(g => g.Count())
                            .First();

                        var members = biggestGroup.ToList();
                        var seriesId = biggestGroup.Key;
                        var total = members[0].SeriesTotal ?? members.Count;
                        var hasAllVolumes = members.Count == total
                            && Enumerable.Range(1, total).All(i => members.Any(m => m.VolumeNo == i));
                        var allPristine = members.All(m => m.Condition == BookCondition.Pristine);

                        if (hasAllVolumes && allPristine)
                        {
                            // 装函仪式 → 入 L3
                            foreach (var b in members) I.SetLocation(b, Location.Shelf);
                            GD.Print($"[Sort] 装函成功！{seriesId} 入藏。");
                        }
                        else
                        {
                            // 缺册 → 生成寻书契机（找第一个缺的卷号）
                            var have = members.Select(m => m.VolumeNo).ToHashSet();
                            var missing = Enumerable.Range(1, total).FirstOrDefault(i => !have.Contains(i));
                            if (missing > 0)
                            {
                                var seriesName = members.FirstOrDefault(m => m.Identified)?.DisplayName
                                                 ?? seriesId;
                                var baseName = seriesName.Split('·')[0];
                                var lead = Card.New(CardType.Lead, $"寻书契机·{baseName}·卷{missing}");
                                lead.Description = "提交访客台可加速对应卷流入。";
                                lead.Aspects.Add(Aspect.Memory, 1);
                                ctx.SpawnedCards.Add(lead);
                            }
                            GD.Print($"[Sort] {seriesId} 不完整，已生成寻书契机。");
                        }
                    },
                },
            },
        });

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

}
