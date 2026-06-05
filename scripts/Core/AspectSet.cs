using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EnLib.Core;

/// <summary>
/// 一个 aspect → 强度的字典。卡牌、配方需求、当前堆叠的合计，
/// 都用同一个结构来表达，方便统一比较与累加。
/// </summary>
public sealed class AspectSet : IEnumerable<KeyValuePair<Aspect, int>>
{
    private readonly Dictionary<Aspect, int> _values = new();

    public AspectSet() { }

    public AspectSet(IEnumerable<KeyValuePair<Aspect, int>> seed)
    {
        foreach (var kv in seed) _values[kv.Key] = kv.Value;
    }

    public int this[Aspect a]
    {
        get => _values.TryGetValue(a, out var v) ? v : 0;
        set
        {
            if (value == 0) _values.Remove(a);
            else _values[a] = value;
        }
    }

    public void Add(Aspect a, int amount)
    {
        this[a] = this[a] + amount;
    }

    public void Merge(AspectSet other)
    {
        foreach (var kv in other._values) Add(kv.Key, kv.Value);
    }

    /// <summary>当前集合的 aspect[a] 是否 >= min。</summary>
    public bool Meets(Aspect a, int min) => this[a] >= min;

    public bool IsEmpty => _values.Count == 0;

    public IEnumerator<KeyValuePair<Aspect, int>> GetEnumerator() => _values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString()
    {
        if (IsEmpty) return "·";
        var sb = new StringBuilder();
        foreach (var kv in _values.OrderBy(k => k.Key))
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(kv.Key).Append(':').Append(kv.Value);
        }
        return sb.ToString();
    }

    /// <summary>把多张卡的 aspect 累加成一个总集。</summary>
    public static AspectSet Sum(IEnumerable<AspectSet> sets)
    {
        var result = new AspectSet();
        foreach (var s in sets) result.Merge(s);
        return result;
    }
}
