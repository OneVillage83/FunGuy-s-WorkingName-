using System;
using System.Collections.Generic;
using System.Linq;

public class BattleSim {
  private const float ActionGaugeThreshold = 1000f;
  private const float DefenseConstant = 3000f;
  private const int StartTurnEnergyGain = 20;
  private const int DealDamageEnergyGain = 5;
  private const int TakeDamageEnergyGain = 10;
  private const int KillEnergyGain = 15;

  private readonly GameData _data;
  private readonly Random _rng;

  public BattleSim(GameData data) : this(data, rng: null) { }

  public BattleSim(GameData data, int seed) : this(data, new Random(seed)) { }

  public BattleSim(GameData data, Random rng) {
    _data = data;
    _rng = rng ?? new Random();
  }

  public bool RunBattle(List<CombatUnit> player, List<CombatUnit> enemy, int maxTurns = 200) {
    int turn = 0;

    while (turn++ < maxTurns && AnyAlive(player) && AnyAlive(enemy)) {
      var actor = NextActor(player, enemy);
      if (actor == null) break;
      if (actor.hp <= 0) continue;

      actor.actionGauge = Math.Max(0f, actor.actionGauge - ActionGaugeThreshold);
      GainEnergy(actor, StartTurnEnergyGain);
      TickUltCooldown(actor);

      bool turnSkipped = ApplyStartOfTurnStatuses(actor);
      if (turnSkipped) continue;

      var opponents = actor.side == TeamSide.Player ? enemy : player;
      var allies = actor.side == TeamSide.Player ? player : enemy;
      if (!AnyAlive(opponents)) break;

      if (!TrySelectSkill(actor, out var selectedSkill, out bool usedUlt)) continue;

      if (usedUlt) {
        SpendEnergy(actor, selectedSkill.energyCost > 0 ? selectedSkill.energyCost : actor.maxEnergy);
        actor.ultCdRemaining = Math.Max(0, selectedSkill.cooldown);
      }

      ExecuteSkill(actor, allies, opponents, selectedSkill);
    }

    return AnyAlive(player) && !AnyAlive(enemy);
  }

  private bool AnyAlive(List<CombatUnit> team) => team.Any(u => u.hp > 0);

  private CombatUnit NextActor(List<CombatUnit> player, List<CombatUnit> enemy) {
    var alive = player.Concat(enemy).Where(u => u.hp > 0).ToList();
    if (alive.Count == 0) return null;

    float highestGauge = alive.Max(u => u.actionGauge);
    if (highestGauge < ActionGaugeThreshold) {
      int ticks = alive
        .Select(u => TicksUntilAction(u))
        .DefaultIfEmpty(1)
        .Min();

      foreach (var unit in alive) {
        unit.actionGauge += Math.Max(1, unit.spd) * ticks;
      }
    }

    return alive
      .OrderByDescending(u => u.actionGauge)
      .ThenByDescending(u => u.spd)
      .ThenBy(u => u.side)
      .FirstOrDefault();
  }

  private static int TicksUntilAction(CombatUnit unit) {
    int speed = Math.Max(1, unit.spd);
    float needed = Math.Max(0f, ActionGaugeThreshold - unit.actionGauge);
    return Math.Max(1, (int)Math.Ceiling(needed / speed));
  }

  private static void TickUltCooldown(CombatUnit unit) {
    if (unit.ultCdRemaining > 0) unit.ultCdRemaining -= 1;
  }

  private bool TrySelectSkill(CombatUnit actor, out SkillDef selectedSkill, out bool usedUlt) {
    selectedSkill = null;
    usedUlt = false;

    _ = _data.Skills.TryGetValue(actor.basicSkillId, out var basicSkill);
    _ = _data.Skills.TryGetValue(actor.ultSkillId, out var ultSkill);
    if (basicSkill == null && ultSkill == null) return false;

    bool silenced = HasStatus(actor, "Silence");
    bool canCastUlt = !silenced &&
      ultSkill != null &&
      actor.ultCdRemaining <= 0 &&
      actor.energy >= Math.Max(0, ultSkill.energyCost);

    selectedSkill = canCastUlt ? ultSkill : basicSkill ?? ultSkill;
    usedUlt = canCastUlt;
    return selectedSkill != null;
  }

  private bool ApplyStartOfTurnStatuses(CombatUnit u) {
    bool skipTurn = false;

    foreach (var s in u.statuses.ToList()) {
      int stacks = Math.Max(1, s.stacks);
      string status = (s.status ?? string.Empty).Trim().ToLowerInvariant();

      if (status == "regen") Heal(u, (int)MathF.Round(u.maxHp * s.potency * stacks));
      if (status == "poison" || status == "burn" || status == "bleed") {
        int dot = (int)MathF.Round(u.maxHp * s.potency * stacks);
        DealPure(u, dot);
      }
      if (status == "freeze" || status == "stun") skipTurn = true;

      s.remainingTurns -= 1;
      if (s.remainingTurns <= 0) u.statuses.Remove(s);
    }

    return skipTurn;
  }

  private void ExecuteSkill(CombatUnit actor, List<CombatUnit> allies, List<CombatUnit> enemies, SkillDef skill) {
    var targets = SelectTargets(skill.target, actor, allies, enemies);
    if (targets.Count == 0 || skill.effects == null) return;

    foreach (var eff in skill.effects) {
      foreach (var t in targets) ApplyEffect(actor, t, allies, enemies, eff);
    }
  }

  private List<CombatUnit> SelectTargets(string targetRule, CombatUnit actor, List<CombatUnit> allies, List<CombatUnit> enemies) {
    enemies = enemies.Where(u => u.hp > 0).ToList();
    allies  = allies.Where(u => u.hp > 0).ToList();

    return targetRule switch {
      "Self" => new List<CombatUnit> { actor },
      "AllEnemies" => enemies,
      "AllAllies" => allies,
      "EnemyFront" => enemies.Count > 0 ? new List<CombatUnit> { enemies[0] } : new List<CombatUnit>(),
      "RandomEnemy2" => enemies.OrderBy(_ => _rng.Next()).Take(2).ToList(),
      _ => enemies.Count > 0 ? new List<CombatUnit> { enemies[0] } : new List<CombatUnit>()
    };
  }

  private void ApplyEffect(CombatUnit actor, CombatUnit target, List<CombatUnit> allies, List<CombatUnit> enemies, EffectDef eff) {
    if (target.hp <= 0) return;

    float roll = (float)_rng.NextDouble();
    if (eff.type == "ApplyStatus" && roll > EffectiveStatusChance(actor, target, eff)) return;

    switch (eff.type) {
      case "Damage": {
        int dmg = CalcDamage(actor, target, eff.scale);
        var result = DealDamage(target, dmg);
        if (result.totalDamage > 0) {
          GainEnergy(target, TakeDamageEnergyGain);
          GainEnergy(actor, result.killed ? KillEnergyGain : DealDamageEnergyGain);
        }
        break;
      }
      case "Heal": {
        int amt = (int)MathF.Round(Math.Max(1, actor.pot) * eff.scale);
        Heal(target, amt);
        break;
      }
      case "Shield": {
        int amt = (int)MathF.Round(target.maxHp * eff.scale);
        target.shield += amt;
        break;
      }
      case "ApplyStatus": {
        ApplyStatus(target, eff);
        break;
      }
    }
  }

  private float EffectiveStatusChance(CombatUnit actor, CombatUnit target, EffectDef eff) {
    float baseChance = eff.chance <= 0 ? 1f : eff.chance;
    float potencyBonus = Math.Max(0, actor.pot) / 1000f;
    float resistance = Math.Max(0, target.pot) / 2000f;
    return Clamp01(baseChance + potencyBonus - resistance);
  }

  private void ApplyStatus(CombatUnit target, EffectDef eff) {
    string name = eff.status ?? string.Empty;
    if (string.IsNullOrWhiteSpace(name)) return;

    bool stackable = IsStackableStatus(name);
    var existing = target.statuses.FirstOrDefault(s => string.Equals(s.status, name, StringComparison.OrdinalIgnoreCase));

    if (existing == null || stackable) {
      if (existing != null && stackable) {
        existing.stacks = Math.Max(1, existing.stacks) + 1;
        existing.remainingTurns = Math.Max(existing.remainingTurns, eff.duration);
        existing.potency += eff.potency;
      } else {
        target.statuses.Add(new StatusInstance {
          status = name,
          remainingTurns = Math.Max(1, eff.duration),
          potency = eff.potency,
          stacks = 1,
        });
      }
      return;
    }

    existing.remainingTurns = Math.Max(existing.remainingTurns, eff.duration);
    existing.potency = Math.Max(existing.potency, eff.potency);
  }

  private static bool IsStackableStatus(string status) {
    string key = (status ?? string.Empty).Trim().ToLowerInvariant();
    return key == "poison" || key == "burn" || key == "bleed" || key == "regen";
  }

  private bool HasStatus(CombatUnit unit, string status) {
    return unit.statuses.Any(s => string.Equals(s.status, status, StringComparison.OrdinalIgnoreCase));
  }

  private int CalcDamage(CombatUnit a, CombatUnit d, float scale) {
    float raw = a.atk * scale;
    float mitigated = raw * (DefenseConstant / (DefenseConstant + Math.Max(0, d.def)));
    float biomeMultiplier = BiomeMultiplier(a.biome, d.biome);
    return Math.Max(1, (int)MathF.Round(mitigated * biomeMultiplier));
  }

  private (int totalDamage, bool killed) DealDamage(CombatUnit t, int dmg) {
    int before = t.hp + t.shield;
    if (t.shield > 0) {
      int absorbed = Math.Min(t.shield, dmg);
      t.shield -= absorbed;
      dmg -= absorbed;
    }
    if (dmg > 0) t.hp = Math.Max(0, t.hp - dmg);

    int after = t.hp + t.shield;
    int dealt = Math.Max(0, before - after);
    return (dealt, t.hp <= 0);
  }

  private void DealPure(CombatUnit t, int dmg) {
    _ = DealDamage(t, Math.Max(1, dmg));
  }

  private void Heal(CombatUnit t, int amt) {
    t.hp = Math.Min(t.maxHp, t.hp + Math.Max(0, amt));
  }

  private static float BiomeMultiplier(string attackerBiome, string defenderBiome) {
    string attacker = NormalizeBiome(attackerBiome);
    string defender = NormalizeBiome(defenderBiome);

    if (attacker == "kitchen" || defender == "kitchen") return 1f;
    if (HasAdvantage(attacker, defender)) return 1.5f;
    if (HasAdvantage(defender, attacker)) return 0.75f;
    return 1f;
  }

  private static bool HasAdvantage(string attacker, string defender) {
    return attacker switch {
      "forest" => defender == "wetlands",
      "wetlands" => defender == "decay",
      "decay" => defender == "tundra",
      "tundra" => defender == "forest",
      _ => false,
    };
  }

  private static string NormalizeBiome(string biome) {
    return (biome ?? string.Empty).Trim().ToLowerInvariant();
  }

  private static void GainEnergy(CombatUnit unit, int amount) {
    unit.energy = Math.Min(unit.maxEnergy, unit.energy + Math.Max(0, amount));
  }

  private static void SpendEnergy(CombatUnit unit, int amount) {
    unit.energy = Math.Max(0, unit.energy - Math.Max(0, amount));
  }

  private static float Clamp01(float value) {
    if (value < 0f) return 0f;
    if (value > 1f) return 1f;
    return value;
  }
}
