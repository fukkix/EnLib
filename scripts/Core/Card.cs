using System;

namespace EnLib.Core;

public enum CardType
{
    Book,       // 典籍
    Knowledge,  // 知识
    Tool,       // 工具
    Material,   // 材料
    State,      // 状态
    Visitor,    // 访客
    Lead,       // 契机
}

/// <summary>
/// 典籍的五档品相。每一档解锁更多可读内容（见 DESIGN.md §4.3）。
/// </summary>
public enum BookCondition
{
    Fragment = 0,
    Damaged  = 1,
    Worn     = 2,
    Good     = 3,
    Pristine = 4,
}

/// <summary>
/// 运行时卡牌。每张实际存在的卡都是一个独立实例。
/// （CardDef 资源用于设计期模板；此处先用 POCO，待数据驱动阶段再接入。）
/// </summary>
public sealed class Card
{
    public string Id { get; }
    public CardType Type { get; }
    public string DisplayName { get; set; }
    public string Description { get; set; } = "";
    public AspectSet Aspects { get; } = new();

    // —— Book 专属字段 ——
    public BookCondition Condition { get; set; } = BookCondition.Good;
    public bool Identified { get; set; } = true;
    public string? SeriesId { get; set; }
    public int? VolumeNo { get; set; }

    // —— Tool / Material 专属字段 ——
    public int? Charges { get; set; }   // null = 耐用（无限）

    public Card(string id, CardType type, string displayName)
    {
        Id = id;
        Type = type;
        DisplayName = displayName;
    }

    /// <summary>新建一个全新的、带新 ID 的卡。</summary>
    public static Card New(CardType type, string displayName)
        => new(Guid.NewGuid().ToString("N")[..10], type, displayName);
}
