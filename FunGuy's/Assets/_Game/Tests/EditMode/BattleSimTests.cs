using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public class BattleSimTests
{
    [Test]
    public void Battle_IsDeterministic_WithSameSeedAndInputs()
    {
        var data = BuildBattleData();
        var playerA = BuildPlayerTeam();
        var enemyA = BuildEnemyTeam();
        var playerB = CloneUnits(playerA);
        var enemyB = CloneUnits(enemyA);

        var simA = new BattleSim(data, seed: 101);
        var simB = new BattleSim(data, seed: 101);

        bool resultA = simA.RunBattle(playerA, enemyA, maxTurns: 120);
        bool resultB = simB.RunBattle(playerB, enemyB, maxTurns: 120);

        Assert.AreEqual(resultA, resultB);
        CollectionAssert.AreEqual(
            playerA.Select(u => u.hp).Concat(enemyA.Select(u => u.hp)).ToArray(),
            playerB.Select(u => u.hp).Concat(enemyB.Select(u => u.hp)).ToArray());
    }

    [Test]
    public void BiomeAdvantage_AppliesExpectedDamageMultiplier()
    {
        var data = BuildBattleData();
        var attacker = new CombatUnit
        {
            side = TeamSide.Player,
            id = "attacker",
            name = "attacker",
            biome = "Forest",
            atk = 100,
            def = 0,
            spd = 1000,
            pot = 10,
            hp = 500,
            maxHp = 500,
            basicSkillId = "atk_basic",
            ultSkillId = "atk_basic",
            maxEnergy = 100,
            statuses = new List<StatusInstance>()
        };
        var defender = new CombatUnit
        {
            side = TeamSide.Enemy,
            id = "defender",
            name = "defender",
            biome = "Wetlands",
            atk = 1,
            def = 0,
            spd = 1,
            pot = 1,
            hp = 1000,
            maxHp = 1000,
            basicSkillId = "atk_basic",
            ultSkillId = "atk_basic",
            maxEnergy = 100,
            statuses = new List<StatusInstance>()
        };

        var sim = new BattleSim(data, seed: 55);
        _ = sim.RunBattle(new List<CombatUnit> { attacker }, new List<CombatUnit> { defender }, maxTurns: 1);

        // Formula: 100 * (3000/(3000+0)) * 1.5 = 150
        Assert.AreEqual(850, defender.hp);
    }

    private static GameData BuildBattleData()
    {
        var data = new GameData();
        data.Skills = new Dictionary<string, SkillDef>
        {
            ["atk_basic"] = new SkillDef
            {
                id = "atk_basic",
                target = "EnemyFront",
                cooldown = 0,
                energyCost = 0,
                effects = new List<EffectDef> { new() { type = "Damage", scale = 1f } }
            },
            ["ult_poison"] = new SkillDef
            {
                id = "ult_poison",
                target = "AllEnemies",
                cooldown = 3,
                energyCost = 100,
                effects = new List<EffectDef>
                {
                    new() { type = "Damage", scale = 0.7f },
                    new() { type = "ApplyStatus", status = "Poison", chance = 0.4f, duration = 2, potency = 0.05f }
                }
            }
        };
        return data;
    }

    private static List<CombatUnit> BuildPlayerTeam()
    {
        return new List<CombatUnit>
        {
            new()
            {
                side = TeamSide.Player, id = "p1", name = "p1", biome = "Forest",
                maxHp = 900, hp = 900, atk = 120, def = 40, spd = 150, pot = 80,
                basicSkillId = "atk_basic", ultSkillId = "ult_poison", maxEnergy = 100,
                statuses = new List<StatusInstance>()
            },
            new()
            {
                side = TeamSide.Player, id = "p2", name = "p2", biome = "Kitchen",
                maxHp = 800, hp = 800, atk = 90, def = 55, spd = 110, pot = 50,
                basicSkillId = "atk_basic", ultSkillId = "atk_basic", maxEnergy = 100,
                statuses = new List<StatusInstance>()
            }
        };
    }

    private static List<CombatUnit> BuildEnemyTeam()
    {
        return new List<CombatUnit>
        {
            new()
            {
                side = TeamSide.Enemy, id = "e1", name = "e1", biome = "Wetlands",
                maxHp = 950, hp = 950, atk = 95, def = 50, spd = 120, pot = 45,
                basicSkillId = "atk_basic", ultSkillId = "atk_basic", maxEnergy = 100,
                statuses = new List<StatusInstance>()
            },
            new()
            {
                side = TeamSide.Enemy, id = "e2", name = "e2", biome = "Decay",
                maxHp = 700, hp = 700, atk = 110, def = 35, spd = 140, pot = 40,
                basicSkillId = "atk_basic", ultSkillId = "atk_basic", maxEnergy = 100,
                statuses = new List<StatusInstance>()
            }
        };
    }

    private static List<CombatUnit> CloneUnits(List<CombatUnit> source)
    {
        return source.Select(s => new CombatUnit
        {
            side = s.side,
            id = s.id,
            name = s.name,
            level = s.level,
            biome = s.biome,
            classArchetype = s.classArchetype,
            role = s.role,
            maxHp = s.maxHp,
            hp = s.hp,
            atk = s.atk,
            def = s.def,
            spd = s.spd,
            pot = s.pot,
            basicSkillId = s.basicSkillId,
            ultSkillId = s.ultSkillId,
            ultCdRemaining = s.ultCdRemaining,
            energy = s.energy,
            maxEnergy = s.maxEnergy,
            actionGauge = s.actionGauge,
            shield = s.shield,
            statuses = s.statuses.Select(x => new StatusInstance
            {
                status = x.status,
                remainingTurns = x.remainingTurns,
                potency = x.potency,
                stacks = x.stacks
            }).ToList()
        }).ToList();
    }
}
