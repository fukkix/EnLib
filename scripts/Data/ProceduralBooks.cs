using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using EnLib.Core;
using Godot;

namespace EnLib.Data;

/// <summary>
/// 从 idleditor 导出的标题池批量生成 Book 卡。
/// 不带 series，作为"散卷"流入，给玩家一个有规模感的初始馆藏。
/// </summary>
public static class ProceduralBooks
{
    // 题材 → (主 aspect, 副 aspect)
    private static readonly Dictionary<string, (Aspect main, Aspect sub)> GenreAspects = new()
    {
        { "sci-fi",         (Aspect.Sun,     Aspect.Lore)   },
        { "mystery",        (Aspect.Mortal,  Aspect.Secret) },
        { "suspense",       (Aspect.Silver,  Aspect.Secret) },
        { "social-science", (Aspect.Night,   Aspect.Lore)   },
        { "hybrid",         (Aspect.Ancient, Aspect.Night)  },
        { "light-novel",    (Aspect.Blood,   Aspect.Mortal) },
    };

    // 品相分布权重（残页多，精装少）
    private static readonly (BookCondition cond, int weight)[] CondDist =
    {
        (BookCondition.Fragment, 20),
        (BookCondition.Damaged,  30),
        (BookCondition.Worn,     30),
        (BookCondition.Good,     15),
        (BookCondition.Pristine,  5),
    };

    public static Dictionary<string, List<string>> LoadPools(string resPath)
    {
        using var file = FileAccess.Open(resPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr($"[ProceduralBooks] 无法读取 {resPath}");
            return new();
        }
        var json = file.GetAsText();
        return JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json) ?? new();
    }

    public static List<Card> SpawnFromPools(string resPath, int perGenre, Random rng)
    {
        var pools = LoadPools(resPath);
        var result = new List<Card>();
        foreach (var (genre, titles) in pools)
        {
            if (!GenreAspects.TryGetValue(genre, out var ga))
            {
                GD.PrintErr($"[ProceduralBooks] 未配置 aspect 映射：{genre}");
                continue;
            }
            var picks = titles.OrderBy(_ => rng.Next()).Take(perGenre);
            foreach (var t in picks)
            {
                var c = Card.New(CardType.Book, t);
                ApplyGenre(c, ga, rng);
                ApplyRandomCondition(c, rng);
                c.Identified = rng.NextDouble() < 0.6;
                result.Add(c);
            }
        }
        return result;
    }

    private static void ApplyGenre(Card c, (Aspect main, Aspect sub) ga, Random rng)
    {
        c.Aspects.Add(ga.main, rng.Next(2, 4));   // 2~3
        c.Aspects.Add(ga.sub,  rng.Next(1, 3));   // 1~2
    }

    private static void ApplyRandomCondition(Card c, Random rng)
    {
        var total = CondDist.Sum(x => x.weight);
        var roll = rng.Next(total);
        int acc = 0;
        foreach (var (cond, w) in CondDist)
        {
            acc += w;
            if (roll < acc) { c.Condition = cond; break; }
        }
        // 损伤越重 → decay 越高（用于修复台匹配）
        var decay = 4 - (int)c.Condition;
        if (decay > 0) c.Aspects.Add(Aspect.Decay, decay);
    }
}
