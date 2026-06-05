using System;
using System.Collections.Generic;
using System.Linq;

namespace EnLib.Core;

public enum Verb
{
    Identify,
    Restore,
    Sort,
    Study,
    Reception,
}

public readonly record struct AspectRequirement(Aspect Aspect, int Min);

/// <summary>
/// 一次成功执行后产生的结果分支之一。Outcome 在配方运行结束时按权重随机抽取。
/// </summary>
public sealed class Outcome
{
    public string Id { get; init; } = "";
    public float Weight { get; init; } = 1f;
    public string TextPoolId { get; init; } = "";   // 文案池索引，后续接入
    public Action<RecipeContext>? Effect { get; init; }
}

/// <summary>
/// 配方触发时给 Effect 看的运行上下文。
/// </summary>
public sealed class RecipeContext
{
    public required IReadOnlyList<Card> InputCards { get; init; }
    public required AspectSet AspectsTotal { get; init; }
    public List<Card> SpawnedCards { get; } = new();

    /// <summary>第一张满足某类型的卡。配方 effect 常用来定位"主要目标"。</summary>
    public Card? FirstOf(CardType type) => InputCards.FirstOrDefault(c => c.Type == type);
}

/// <summary>
/// 静态配方定义。Phase 1 暂用代码硬编码；后续改为 YAML 加载。
/// </summary>
public sealed class Recipe
{
    public string Id { get; init; } = "";
    public Verb Verb { get; init; }
    public AspectRequirement[] Requires { get; init; } = Array.Empty<AspectRequirement>();
    /// <summary>aspect 阈值不够表达时的额外谓词，如"必含未辨识典籍"。</summary>
    public Func<IReadOnlyList<Card>, bool>? ExtraMatch { get; init; }
    public float DurationSec { get; init; } = 5f;
    public Outcome[] Outcomes { get; init; } = Array.Empty<Outcome>();

    /// <summary>给定一组输入卡，判断是否满足该配方的全部要求。</summary>
    public bool Matches(IReadOnlyList<Card> cards)
    {
        var sum = AspectSet.Sum(cards.Select(c => c.Aspects));
        foreach (var req in Requires)
            if (!sum.Meets(req.Aspect, req.Min)) return false;
        if (ExtraMatch != null && !ExtraMatch(cards)) return false;
        return true;
    }

    public Outcome PickOutcome(Random rng)
    {
        if (Outcomes.Length == 0) throw new InvalidOperationException($"Recipe {Id} has no outcomes.");
        var totalWeight = Outcomes.Sum(o => Math.Max(0f, o.Weight));
        if (totalWeight <= 0f) return Outcomes[0];
        var roll = rng.NextDouble() * totalWeight;
        double acc = 0;
        foreach (var o in Outcomes)
        {
            acc += Math.Max(0f, o.Weight);
            if (roll <= acc) return o;
        }
        return Outcomes[^1];
    }
}
