using System;
using System.Collections.Generic;
using System.Linq;
using EnLib.Core;
using Godot;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace EnLib.Data;

/// <summary>
/// 从 data/recipes.yaml 加载配方。把 YAML 的声明式 do/extra_match
/// 拼装成 Recipe.Effect 委托与 ExtraMatch 谓词。
/// </summary>
public static class RecipeLoader
{
    // ──── YAML schema ────
    public sealed class RecipeSpec
    {
        public string Id { get; set; } = "";
        public string Verb { get; set; } = "";
        public Dictionary<string, int> Requires { get; set; } = new();
        public string? ExtraMatch { get; set; }
        public float Duration { get; set; } = 5f;
        public List<OutcomeSpec> Outcomes { get; set; } = new();
    }

    public sealed class OutcomeSpec
    {
        public string Id { get; set; } = "";
        public float Weight { get; set; } = 1f;
        public string Text { get; set; } = "";
        public List<Dictionary<string, object>> Do { get; set; } = new();
    }

    private static readonly IDeserializer _yaml = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static List<Recipe> LoadFile(string resPath)
    {
        using var file = FileAccess.Open(resPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr($"[RecipeLoader] 无法打开 {resPath}");
            return new();
        }
        var text = file.GetAsText();
        var specs = _yaml.Deserialize<List<RecipeSpec>>(text) ?? new();
        return specs.Select(BuildRecipe).ToList();
    }

    private static Recipe BuildRecipe(RecipeSpec spec)
    {
        var requires = spec.Requires
            .Select(kv => new AspectRequirement(ParseAspect(kv.Key), kv.Value))
            .ToArray();

        return new Recipe
        {
            Id = spec.Id,
            Verb = ParseVerb(spec.Verb),
            Requires = requires,
            ExtraMatch = spec.ExtraMatch != null ? Predicates[spec.ExtraMatch] : null,
            DurationSec = spec.Duration,
            Outcomes = spec.Outcomes.Select(BuildOutcome).ToArray(),
        };
    }

    private static Outcome BuildOutcome(OutcomeSpec spec) => new()
    {
        Id = spec.Id,
        Weight = spec.Weight,
        TextPoolId = spec.Text,
        Effect = BuildEffect(spec.Do),
    };

    private static Action<RecipeContext> BuildEffect(List<Dictionary<string, object>> actions)
    {
        var pipeline = actions.Select(BuildAction).ToList();
        return ctx => { foreach (var a in pipeline) a(ctx); };
    }

    private static Action<RecipeContext> BuildAction(Dictionary<string, object> args)
    {
        var name = ArgString(args, "action") ?? throw new ArgumentException("action 必填");
        return name switch
        {
            "condition_change"        => Actions.ConditionChange(ArgInt(args, "amount", 1)),
            "identify_books"          => Actions.IdentifyBooks,
            "spawn_card"              => Actions.SpawnCard(args),
            "try_shelf_or_spawn_lead" => Actions.TryShelfOrSpawnLead,
            _ => throw new ArgumentException($"未知 action: {name}"),
        };
    }

    // ──── ExtraMatch 谓词注册表 ────
    private static readonly Dictionary<string, Func<IReadOnlyList<Card>, bool>> Predicates = new()
    {
        ["has_unidentified_book"]  = cards => cards.Any(c => c.Type == CardType.Book && !c.Identified),
        ["has_same_series_2plus"]  = cards =>
        {
            var books = cards.Where(c => c.Type == CardType.Book && c.SeriesId != null);
            return books.GroupBy(b => b.SeriesId).Any(g => g.Count() >= 2);
        },
    };

    // ──── 类型解析辅助 ────
    private static Verb ParseVerb(string s) => s.ToLowerInvariant() switch
    {
        "identify"  => Verb.Identify,
        "restore"   => Verb.Restore,
        "sort"      => Verb.Sort,
        "study"     => Verb.Study,
        "reception" => Verb.Reception,
        _ => throw new ArgumentException($"未知 verb: {s}"),
    };

    private static Aspect ParseAspect(string s) => s.ToLowerInvariant() switch
    {
        "sun"     => Aspect.Sun,
        "silver"  => Aspect.Silver,
        "mortal"  => Aspect.Mortal,
        "ancient" => Aspect.Ancient,
        "night"   => Aspect.Night,
        "craft"   => Aspect.Craft,
        "lore"    => Aspect.Lore,
        "decay"   => Aspect.Decay,
        "blood"   => Aspect.Blood,
        "memory"  => Aspect.Memory,
        "secret"  => Aspect.Secret,
        _ => throw new ArgumentException($"未知 aspect: {s}"),
    };

    private static CardType ParseCardType(string s) => s.ToLowerInvariant() switch
    {
        "book"      => CardType.Book,
        "knowledge" => CardType.Knowledge,
        "tool"      => CardType.Tool,
        "material"  => CardType.Material,
        "state"     => CardType.State,
        "visitor"   => CardType.Visitor,
        "lead"      => CardType.Lead,
        _ => throw new ArgumentException($"未知 card type: {s}"),
    };

    private static string? ArgString(Dictionary<string, object> d, string key)
        => d.TryGetValue(key, out var v) ? v?.ToString() : null;

    private static int ArgInt(Dictionary<string, object> d, string key, int fallback)
        => d.TryGetValue(key, out var v) && int.TryParse(v?.ToString(), out var n) ? n : fallback;

    // ──── 内置 actions ────
    private static class Actions
    {
        public static Action<RecipeContext> ConditionChange(int amount) => ctx =>
        {
            var b = ctx.FirstOf(CardType.Book);
            if (b == null) return;
            var newCond = Math.Clamp((int)b.Condition + amount, 0, (int)BookCondition.Pristine);
            b.Condition = (BookCondition)newCond;
        };

        public static void IdentifyBooks(RecipeContext ctx)
        {
            foreach (var c in ctx.InputCards)
                if (c.Type == CardType.Book && !c.Identified) c.Identified = true;
        }

        public static Action<RecipeContext> SpawnCard(Dictionary<string, object> args) => ctx =>
        {
            var type = ParseCardType(ArgString(args, "type") ?? "state");
            var name = ResolveName(args, ctx);
            var desc = ArgString(args, "description") ?? "";

            var c = Card.New(type, name);
            c.Description = desc;
            if (args.TryGetValue("aspects", out var asp) && asp is Dictionary<object, object> dict)
            {
                foreach (var (k, v) in dict)
                    c.Aspects.Add(ParseAspect(k.ToString()!), int.Parse(v.ToString()!));
            }
            ctx.SpawnedCards.Add(c);
        };

        public static void TryShelfOrSpawnLead(RecipeContext ctx)
        {
            var books = ctx.InputCards
                .Where(c => c.Type == CardType.Book && c.SeriesId != null)
                .ToList();
            if (books.Count == 0) return;

            var biggest = books.GroupBy(b => b.SeriesId!).OrderByDescending(g => g.Count()).First();
            var members = biggest.ToList();
            var seriesId = biggest.Key;
            var total = members[0].SeriesTotal ?? members.Count;
            var hasAllVolumes = members.Count == total
                && Enumerable.Range(1, total).All(i => members.Any(m => m.VolumeNo == i));
            var allPristine = members.All(m => m.Condition == BookCondition.Pristine);

            if (hasAllVolumes && allPristine)
            {
                foreach (var b in members) GameState.I.SetLocation(b, Location.Shelf);
                GD.Print($"[Sort] 装函成功！{seriesId} 入藏。");
            }
            else
            {
                var have = members.Select(m => m.VolumeNo).ToHashSet();
                var missing = Enumerable.Range(1, total).FirstOrDefault(i => !have.Contains(i));
                if (missing > 0)
                {
                    var seriesName = members.FirstOrDefault(m => m.Identified)?.DisplayName.Split('·')[0]
                                     ?? seriesId;
                    var lead = Card.New(CardType.Lead, $"寻书契机·{seriesName}·卷{missing}");
                    lead.Description = "提交访客台可加速对应卷流入。";
                    lead.Aspects.Add(Aspect.Memory, 1);
                    ctx.SpawnedCards.Add(lead);
                }
                GD.Print($"[Sort] {seriesId} 不完整，已生成寻书契机。");
            }
        }

        private static string ResolveName(Dictionary<string, object> args, RecipeContext ctx)
        {
            if (ArgString(args, "name") is { } literal) return literal;
            var tpl = ArgString(args, "name_template") ?? "未命名卡";
            var book = ctx.FirstOf(CardType.Book);
            return tpl.Replace("{book.name}", book?.DisplayTitle ?? "");
        }
    }
}
