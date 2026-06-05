using System.Collections.Generic;
using EnLib.Core;
using Godot;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace EnLib.Data;

/// <summary>
/// 从 data/*.yaml 加载卡牌定义。Phase 2.3：仅卡牌；配方仍写在 GameState 里。
/// </summary>
public static class CardLoader
{
    /// <summary>YAML 中的一条卡定义。字段命名走 snake_case。</summary>
    public sealed class CardSpec
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string? Condition { get; set; }
        public bool Identified { get; set; } = true;
        public string? Series { get; set; }
        public int? Volume { get; set; }
        public int? SeriesTotal { get; set; }
        public int? Charges { get; set; }
        public string? Description { get; set; }
        public Dictionary<string, int> Aspects { get; set; } = new();
    }

    private static readonly IDeserializer _yaml = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    /// <summary>读取 res://path 下的 YAML，解析为 CardSpec 列表。</summary>
    public static List<CardSpec> LoadFile(string resPath)
    {
        using var file = FileAccess.Open(resPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr($"[CardLoader] 无法打开 {resPath}: {FileAccess.GetOpenError()}");
            return new();
        }
        var text = file.GetAsText();
        return _yaml.Deserialize<List<CardSpec>>(text) ?? new();
    }

    /// <summary>把 CardSpec 转成运行时 Card 实例。</summary>
    public static Card SpecToCard(CardSpec spec)
    {
        var type = ParseType(spec.Type);
        var c = Card.New(type, spec.Name);
        c.Description = spec.Description ?? "";
        c.Charges = spec.Charges;

        if (type == CardType.Book)
        {
            c.Identified = spec.Identified;
            c.SeriesId = spec.Series;
            c.VolumeNo = spec.Volume;
            c.SeriesTotal = spec.SeriesTotal;
            if (spec.Condition != null) c.Condition = ParseCondition(spec.Condition);
        }

        foreach (var (key, val) in spec.Aspects)
            c.Aspects.Add(ParseAspect(key), val);

        return c;
    }

    private static CardType ParseType(string s) => s.ToLowerInvariant() switch
    {
        "book"      => CardType.Book,
        "knowledge" => CardType.Knowledge,
        "tool"      => CardType.Tool,
        "material"  => CardType.Material,
        "state"     => CardType.State,
        "visitor"   => CardType.Visitor,
        "lead"      => CardType.Lead,
        _ => throw new System.ArgumentException($"未知 type: {s}"),
    };

    private static BookCondition ParseCondition(string s) => s.ToLowerInvariant() switch
    {
        "fragment" => BookCondition.Fragment,
        "damaged"  => BookCondition.Damaged,
        "worn"     => BookCondition.Worn,
        "good"     => BookCondition.Good,
        "pristine" => BookCondition.Pristine,
        _ => throw new System.ArgumentException($"未知 condition: {s}"),
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
        _ => throw new System.ArgumentException($"未知 aspect: {s}"),
    };
}
