using System;
using System.Collections.Generic;
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
    // 从 data/*.yaml 加载卡牌与配方。
    // ────────────────────────────────────────────────────────────
    private void SeedDemoData()
    {
        _suppressSignals = true;

        // —— 命名卡（套书/工具/材料 等需要手工指定的）——
        var specs = CardLoader.LoadFile("res://data/cards.yaml");
        foreach (var spec in specs)
        {
            try { Register(CardLoader.SpecToCard(spec)); }
            catch (Exception e) { GD.PrintErr($"[Seed] 跳过 \"{spec.Name}\": {e.Message}"); }
        }

        // —— 程序化生成的散卷（每题材抽 8 本 = ~48 本）——
        foreach (var c in ProceduralBooks.SpawnFromPools("res://data/seed/night_titles.json", 8, Rng))
            Register(c);

        _suppressSignals = false;

        // —— 配方 ——
        foreach (var r in RecipeLoader.LoadFile("res://data/recipes.yaml"))
            _recipes.Add(r);
    }
}
