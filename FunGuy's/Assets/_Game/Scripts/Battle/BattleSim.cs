using System;
using System.Collections.Generic;
using System.Linq;

public class BattleSim {
  private readonly GameData _data;
  private readonly Random _rng = new();

  public BattleSim(GameData data) { _data = data; }

  public bool RunBattle(List<CombatUnit> player, List<CombatUnit> enemy, int maxTurns = 200) {
    int turn = 0;

    while (turn++ < maxTurns && AnyAlive(player) && AnyAlive(enemy)) {
      var all = player.Concat(enemy).Where(u => u.hp > 0).OrderByDescending(u => u.spd).ToList();

      foreach (var actor in all) {
        if (actor.hp <= 0) continue;

        // tick start-of-turn statuses (regen, poison, burn, freeze)
        if (ApplyStartOfTurnStatuses(actor)) {
          // if Freeze consumed their turn, skip
          continue;
        }

        var opponents = actor.side == TeamSide.Player ? enemy : player;
        var allies    = actor.side == TeamSide.Player ? player : enemy;

        // choose skill: ult if ready, else basic
        string skillId = actor.ultCdRemaining <= 0 ? actor.ultSkillId : actor.basicSkillId;
        if (skillId == actor.ultSkillId) actor.ultCdRemaining = _data.Skills[skillId].cooldown;
        else actor.ultCdRemaining = Math.Max(0, actor.ultCdRemaining - 1);

        ExecuteSkill(actor, allies, opponents, _data.Skills[skillId]);

        if (!AnyAlive(player) || !AnyAlive(enemy)) break;
      }
    }

    return AnyAlive(player) && !AnyAlive(enemy);
  }

  private bool AnyAlive(List<CombatUnit> team) => team.Any(u => u.hp > 0);

  private bool ApplyStartOfTurnStatuses(CombatUnit u) {
    bool frozen = false;

    foreach (var s in u.statuses.ToList()) {
      if (s.status == "Regen") {
        Heal(u, (int)MathF.Round(u.maxHp * s.potency));
      }
      if (s.status == "Poison" || s.status == "Burn") {
        DealPure(u, (int)MathF.Round(u.maxHp * s.potency));
      }
      if (s.status == "Freeze") {
        frozen = true;
      }

      s.remainingTurns -= 1;
      if (s.remainingTurns <= 0) u.statuses.Remove(s);
    }

    return frozen; // if frozen, skip action
  }

  private void ExecuteSkill(CombatUnit actor, List<CombatUnit> allies, List<CombatUnit> enemies, SkillDef skill) {
    var targets = SelectTargets(skill.target, actor, allies, enemies);
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
    float roll = (float)_rng.NextDouble();
    if (eff.type == "ApplyStatus" && roll > eff.chance) return;

    switch (eff.type) {
      case "Damage": {
        int dmg = CalcDamage(actor, target, eff.scale);
        DealDamage(target, dmg);
        break;
      }
      case "Heal": {
        int amt = (int)MathF.Round(actor.atk * eff.scale);
        Heal(target, amt);
        break;
      }
      case "Shield": {
        int amt = (int)MathF.Round(target.maxHp * eff.scale);
        target.shield += amt;
        break;
      }
      case "ApplyStatus": {
        target.statuses.Add(new StatusInstance {
          status = eff.status,
          remainingTurns = eff.duration,
          potency = eff.potency
        });
        break;
      }
    }
  }

  private int CalcDamage(CombatUnit a, CombatUnit d, float scale) {
    // MVP formula: (ATK*scale) - DEF*0.5, min 1
    float raw = a.atk * scale;
    float mitigated = raw - (d.def * 0.5f);
    return Math.Max(1, (int)MathF.Round(mitigated));
  }

  private void DealDamage(CombatUnit t, int dmg) {
    if (t.shield > 0) {
      int absorbed = Math.Min(t.shield, dmg);
      t.shield -= absorbed;
      dmg -= absorbed;
    }
    if (dmg > 0) t.hp = Math.Max(0, t.hp - dmg);
  }

  private void DealPure(CombatUnit t, int dmg) {
    // pure ignores DEF but still hits shield
    DealDamage(t, dmg);
  }

  private void Heal(CombatUnit t, int amt) {
    t.hp = Math.Min(t.maxHp, t.hp + Math.Max(0, amt));
  }
}
