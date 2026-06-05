namespace EnLib.Core;

/// <summary>
/// 卡牌的当前位置。GameState 跟踪每张卡所属层。
/// 视图层根据 Location 决定是否显示。
/// </summary>
public enum Location
{
    Inbox,   // L2 待处理筐
    InSlot,  // 已被某个工作台占用
    Shelf,   // L3 馆藏（已上架，不再可拖）
}
