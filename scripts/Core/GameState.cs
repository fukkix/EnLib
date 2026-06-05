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
    private readonly List<Recipe> _recipes = new();

    public IReadOnlyCollection<Card> AllCards => _cards.Values;
    public IReadOnlyList<Recipe> Recipes => _recipes;

    public override void _EnterTree()
    {
        I = this;
    }

    public override void _Ready()
    {
        SeedDemoData();
        GD.Print($"[GameState] Ready. {_cards.Count} cards, {_recipes.Count} recipes.");
    }

    public Card Register(Card c)
    {
        _cards[c.Id] = c;
        return c;
    }

    public Card? GetCard(string id) => _cards.GetValueOrDefault(id);

    public void Remove(Card c) => _cards.Remove(c.Id);

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
        // —— 三张测试卡 ——
        var book = Card.New(CardType.Book, "残卷·银器禁令·卷二");
        book.Condition = BookCondition.Damaged;
        book.Aspects.Add(Aspect.Silver, 2);
        book.Aspects.Add(Aspect.Night, 1);
        book.Aspects.Add(Aspect.Decay, 2);
        book.Description = "书页边缘有银粉腐蚀的痕迹。";
        Register(book);

        var tool = Card.New(CardType.Tool, "牛骨折页刀");
        tool.Aspects.Add(Aspect.Craft, 2);
        tool.Description = "用了几个会期。仍是顺手的那一柄。";
        Register(tool);

        var material = Card.New(CardType.Material, "鞣酸纸");
        material.Aspects.Add(Aspect.Craft, 1);
        material.Aspects.Add(Aspect.Silver, 1); // 用来中和银粉
        material.Charges = 1;
        material.Description = "据说能压住银的腥气。";
        Register(material);

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
