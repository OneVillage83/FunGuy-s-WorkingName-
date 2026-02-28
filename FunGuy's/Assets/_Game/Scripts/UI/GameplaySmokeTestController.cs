using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GameplaySmokeTestController : MonoBehaviour
{
    [SerializeField] private Text outputLabel;
    [SerializeField] private int gachaSeed = 7331;
    [SerializeField] private int battleSeed = 1447;
    [SerializeField] private string bannerId = "b_standard";
    [SerializeField] private string stageId = "s_1_1";

    public void OnRunSmokeTestsPressed()
    {
        var report = RunSmokeTests();
        string joined = string.Join("\n", report.lines);
        if (outputLabel != null) outputLabel.text = joined;

        if (report.passed) Debug.Log($"[SmokeTests] PASS\n{joined}");
        else Debug.LogError($"[SmokeTests] FAIL\n{joined}");
    }

    public (bool passed, List<string> lines) RunSmokeTests()
    {
        EnsureData();
        var lines = new List<string>();
        bool passed = true;

        void Check(string name, Func<bool> test)
        {
            bool ok;
            try { ok = test(); }
            catch (Exception ex)
            {
                ok = false;
                lines.Add($"[FAIL] {name} -> {ex.Message}");
                passed = false;
                return;
            }

            lines.Add(ok ? $"[PASS] {name}" : $"[FAIL] {name}");
            if (!ok) passed = false;
        }

        Check("GameData loaded", () => Game.Data != null && Game.Data.Characters.Count > 0 && Game.Data.Stages.Count > 0);
        Check("Stage wave data valid", StageWaveDataValid);
        Check("Gacha deterministic sequence", GachaDeterministicSequence);
        Check("Battle deterministic result", BattleDeterministicResult);
        Check("Save model roundtrip", SaveModelRoundtrip);

        lines.Insert(0, $"Smoke Test Result: {(passed ? "PASS" : "FAIL")}");
        return (passed, lines);
    }

    private bool StageWaveDataValid()
    {
        foreach (var stage in Game.Data.Stages.Values)
        {
            if (stage.waves == null || stage.waves.Count == 0) return false;
            foreach (var wave in stage.waves)
            {
                if (wave == null || wave.Count == 0) return false;
                if (wave.Any(wu => !Game.Data.Enemies.ContainsKey(wu.enemyId))) return false;
            }
        }

        return true;
    }

    private bool GachaDeterministicSequence()
    {
        var s1 = NewRichSave();
        var s2 = NewRichSave();
        var g1 = new GachaService(Game.Data, gachaSeed);
        var g2 = new GachaService(Game.Data, gachaSeed);

        var seq1 = new List<string>();
        var seq2 = new List<string>();
        for (int i = 0; i < 20; i++)
        {
            seq1.Add(g1.PullOne(s1, bannerId, consumeCurrency: false));
            seq2.Add(g2.PullOne(s2, bannerId, consumeCurrency: false));
        }

        return seq1.SequenceEqual(seq2);
    }

    private bool BattleDeterministicResult()
    {
        if (!Game.Data.Stages.TryGetValue(stageId, out var stage) || stage.waves.Count == 0) return false;
        var chars = Game.Data.Characters.Values.Take(3).ToList();
        if (chars.Count < 2) return false;

        var p1 = new List<CombatUnit>
        {
            ToCombat(chars[0], TeamSide.Player, level: 10),
            ToCombat(chars[1], TeamSide.Player, level: 10),
        };

        var e1 = stage.waves[0]
            .Select(wu => Game.Data.Enemies.TryGetValue(wu.enemyId, out var e) ? ToCombat(e, TeamSide.Enemy, wu.level) : null)
            .Where(u => u != null)
            .ToList();
        if (e1.Count == 0) return false;

        var p2 = p1.Select(CloneUnit).ToList();
        var e2 = e1.Select(CloneUnit).ToList();

        var b1 = new BattleSim(Game.Data, battleSeed);
        var b2 = new BattleSim(Game.Data, battleSeed);
        bool r1 = b1.RunBattle(p1, e1);
        bool r2 = b2.RunBattle(p2, e2);

        int hp1 = p1.Sum(u => u.hp) + e1.Sum(u => u.hp);
        int hp2 = p2.Sum(u => u.hp) + e2.Sum(u => u.hp);
        return r1 == r2 && hp1 == hp2;
    }

    private bool SaveModelRoundtrip()
    {
        var save = NewRichSave();
        save.tutorialStep = 3;
        save.tutorialCompleted = false;
        save.units.Add(new OwnedUnit { charId = "sample", level = 7, copies = 1, stars = 2, coreLevel = 1 });
        SaveMapUtils.SetInt(save.bannerPity, "b_standard", 11);
        SaveMapUtils.SetBool(save.bannerFeaturedGuarantee, "b_event_starspore:featured", true);

        string json = JsonUtility.ToJson(save);
        var loaded = JsonUtility.FromJson<PlayerSave>(json);
        if (loaded == null) return false;

        return loaded.units.Count == 1
            && loaded.tutorialStep == 3
            && SaveMapUtils.GetInt(loaded.bannerPity, "b_standard", 0) == 11
            && SaveMapUtils.GetBool(loaded.bannerFeaturedGuarantee, "b_event_starspore:featured", false);
    }

    private PlayerSave NewRichSave()
    {
        return new PlayerSave
        {
            version = 2,
            createdUtc = DateTime.UtcNow.ToString("o"),
            lastSavedUtc = DateTime.UtcNow.ToString("o"),
            gold = 999999,
            spores = 999999,
            accountLevel = 1,
            sporeEssence = 9999,
            coreFragments = 9999,
            primeSpores = 9999,
            tutorialCompleted = true,
            units = new List<OwnedUnit>(),
            activeTeam = new List<string>(),
            bannerPity = new List<StringIntEntry>(),
            bannerFeaturedGuarantee = new List<StringBoolEntry>(),
            bannerHistory = new List<BannerHistoryEntry>(),
        };
    }

    private CombatUnit ToCombat(CharacterDef def, TeamSide side, int level)
    {
        int lvl = Mathf.Max(1, level);
        int maxHp = Mathf.Max(1, Mathf.RoundToInt(def.baseStats.hp + (def.growth.hp * (lvl - 1))));
        return new CombatUnit
        {
            side = side,
            id = def.id,
            name = def.name,
            level = lvl,
            biome = def.biome,
            classArchetype = def.classArchetype,
            role = def.role,
            maxHp = maxHp,
            hp = maxHp,
            atk = Mathf.Max(1, Mathf.RoundToInt(def.baseStats.atk + (def.growth.atk * (lvl - 1)))),
            def = Mathf.Max(1, Mathf.RoundToInt(def.baseStats.def + (def.growth.def * (lvl - 1)))),
            spd = Mathf.Max(1, Mathf.RoundToInt(def.baseStats.spd + (def.growth.spd * (lvl - 1)))),
            pot = Mathf.Max(1, Mathf.RoundToInt(def.baseStats.pot + (def.growth.pot * (lvl - 1)))),
            basicSkillId = def.skills?.basic,
            ultSkillId = def.skills?.ult,
            maxEnergy = 100,
            statuses = new List<StatusInstance>(),
        };
    }

    private CombatUnit ToCombat(EnemyDef def, TeamSide side, int level)
    {
        int lvl = Mathf.Max(1, level);
        int maxHp = Mathf.Max(1, def.baseStats.hp + ((lvl - 1) * 10));
        return new CombatUnit
        {
            side = side,
            id = def.id,
            name = def.name,
            level = lvl,
            biome = def.biome,
            classArchetype = def.classArchetype,
            role = def.role,
            maxHp = maxHp,
            hp = maxHp,
            atk = Mathf.Max(1, def.baseStats.atk + ((lvl - 1) * 2)),
            def = Mathf.Max(1, def.baseStats.def + ((lvl - 1) * 2)),
            spd = Mathf.Max(1, def.baseStats.spd + ((lvl - 1) * 1)),
            pot = Mathf.Max(1, def.baseStats.pot + ((lvl - 1) * 1)),
            basicSkillId = def.skills?.basic,
            ultSkillId = def.skills?.ult,
            maxEnergy = 100,
            statuses = new List<StatusInstance>(),
        };
    }

    private CombatUnit CloneUnit(CombatUnit src)
    {
        return new CombatUnit
        {
            side = src.side,
            id = src.id,
            name = src.name,
            level = src.level,
            biome = src.biome,
            classArchetype = src.classArchetype,
            role = src.role,
            maxHp = src.maxHp,
            hp = src.hp,
            atk = src.atk,
            def = src.def,
            spd = src.spd,
            pot = src.pot,
            basicSkillId = src.basicSkillId,
            ultSkillId = src.ultSkillId,
            ultCdRemaining = src.ultCdRemaining,
            energy = src.energy,
            maxEnergy = src.maxEnergy,
            actionGauge = src.actionGauge,
            shield = src.shield,
            statuses = src.statuses.Select(s => new StatusInstance
            {
                status = s.status,
                remainingTurns = s.remainingTurns,
                potency = s.potency,
                stacks = s.stacks
            }).ToList(),
        };
    }

    private void EnsureData()
    {
        if (Game.Data != null) return;
        Game.Data = new GameData();
        Game.Data.LoadAll();
    }
}
