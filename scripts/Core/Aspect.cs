namespace EnLib.Core;

/// <summary>
/// 所有 aspect 的枚举。配方匹配以此为唯一坐标。
/// 新增 aspect 时记得同步 DESIGN.md 的 4.2 节。
/// </summary>
public enum Aspect
{
    // 题材主题（继承自 idleditor 8 题材）
    Sun,        // 日 — 阳光、禁忌
    Silver,     // 银 — 神圣、腐蚀
    Mortal,     // 凡 — 凡人世界
    Ancient,    // 古 — 远古、神话
    Night,      // 夜 — 永夜本土

    // 修复主题
    Craft,      // 工 — 装帧/修复工艺
    Lore,       // 识 — 知识、学识
    Decay,      // 朽 — 损伤、腐烂

    // 血族与情感
    Blood,      // 血 — 血族属性、血饥
    Memory,     // 忆 — 回忆、历史
    Secret,     // 秘 — 隐藏、禁忌
}
