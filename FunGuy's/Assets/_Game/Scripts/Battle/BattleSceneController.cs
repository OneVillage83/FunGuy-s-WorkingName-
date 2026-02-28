using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BattleSceneController : MonoBehaviour
{
    [SerializeField] private string stageId = "s_1_1";
    [SerializeField] private Text battleResultLabel;
    [SerializeField] private Text stageInfoLabel;
    [SerializeField] private Text teamPreviewLabel;
    [SerializeField] private bool healTeamBetweenWaves = false;

    private PlayerSave Save => Game.Save ??= SaveSystem.LoadOrNew();
    private bool _lastBattleWon;
    private int _lastWaveReached;
    private int _lastWaveCount;

    private void Start()
    {
        EnsureData();
        RefreshStageUi();
    }

    public void OnRunBattlePressed()
    {
        EnsureData();

        var playerTeam = BuildPlayerTeam();
        if (playerTeam.Count == 0)
        {
            SetResult("Cannot start battle: missing player team.");
            return;
        }

        if (!Game.Data.Stages.TryGetValue(stageId, out var stage) || stage.waves == null || stage.waves.Count == 0)
        {
            SetResult($"Cannot start battle: stage {stageId} has no waves.");
            return;
        }

        _lastWaveReached = 0;
        _lastWaveCount = stage.waves.Count;
        _lastBattleWon = true;

        for (int i = 0; i < stage.waves.Count; i++)
        {
            int waveNumber = i + 1;
            var enemyTeam = BuildEnemyTeam(stage, i);
            if (enemyTeam.Count == 0)
            {
                _lastBattleWon = false;
                SetResult($"Stage data error: wave {waveNumber} has no enemies.");
                break;
            }

            var sim = new BattleSim(Game.Data);
            bool waveWon = sim.RunBattle(playerTeam, enemyTeam);
            _lastWaveReached = waveNumber;

            if (!waveWon)
            {
                _lastBattleWon = false;
                break;
            }

            if (healTeamBetweenWaves && waveNumber < stage.waves.Count)
            {
                foreach (var unit in playerTeam.Where(u => u.hp > 0))
                {
                    unit.hp = Mathf.Min(unit.maxHp, unit.hp + Mathf.RoundToInt(unit.maxHp * 0.25f));
                    unit.statuses.Clear();
                }
            }
        }

        HandleBattleOutcome(_lastBattleWon);
    }

    public void OnBackPressed()
    {
        SceneManager.LoadScene("Home");
    }

    public void OnGoTeamPressed()
    {
        SceneManager.LoadScene("Team");
    }

    public void OnRetryPressed()
    {
        OnRunBattlePressed();
    }

    public void OnNextStagePressed()
    {
        EnsureData();
        var ordered = Game.Data.Stages.Keys.OrderBy(k => k, System.StringComparer.OrdinalIgnoreCase).ToList();
        if (ordered.Count == 0) return;

        int idx = ordered.IndexOf(stageId);
        if (idx < 0) idx = 0;
        int next = Mathf.Clamp(idx + 1, 0, ordered.Count - 1);
        stageId = ordered[next];
        RefreshStageUi();
    }

    public void OnSelectStage(string newStageId)
    {
        if (string.IsNullOrWhiteSpace(newStageId)) return;
        stageId = newStageId;
        RefreshStageUi();
    }

    private List<CombatUnit> BuildPlayerTeam()
    {
        var selected = Save.activeTeam.Count > 0
            ? Save.activeTeam
            : Save.units.Take(5).Select(u => u.charId).ToList();

        var units = new List<CombatUnit>();
        foreach (var charId in selected)
        {
            var owned = Save.units.FirstOrDefault(u => u.charId == charId);
            if (owned == null) continue;
            if (!Game.Data.Characters.TryGetValue(charId, out var def)) continue;
            units.Add(ToCombatUnit(def, owned.level, TeamSide.Player));
        }

        return units;
    }

    private List<CombatUnit> BuildEnemyTeam(StageDef stage, int waveIndex)
    {
        if (stage == null || stage.waves == null || stage.waves.Count <= waveIndex) return new List<CombatUnit>();

        var firstWave = stage.waves[waveIndex];
        var units = new List<CombatUnit>();

        foreach (var waveUnit in firstWave)
        {
            if (!Game.Data.Enemies.TryGetValue(waveUnit.enemyId, out var def)) continue;
            units.Add(ToCombatUnit(def, waveUnit.level, TeamSide.Enemy));
        }

        return units;
    }

    private CombatUnit ToCombatUnit(CharacterDef def, int level, TeamSide side)
    {
        int lvl = Mathf.Max(1, level);
        int maxHp = ScaleStat(def.baseStats.hp, def.growth.hp, lvl);
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
            atk = ScaleStat(def.baseStats.atk, def.growth.atk, lvl),
            def = ScaleStat(def.baseStats.def, def.growth.def, lvl),
            spd = ScaleStat(def.baseStats.spd, def.growth.spd, lvl),
            pot = ScaleStat(def.baseStats.pot, def.growth.pot, lvl),
            basicSkillId = def.skills?.basic,
            ultSkillId = def.skills?.ult,
            ultCdRemaining = 0,
            energy = 0,
            maxEnergy = 100,
            actionGauge = 0f,
            shield = 0,
            statuses = new List<StatusInstance>(),
        };
    }

    private CombatUnit ToCombatUnit(EnemyDef def, int level, TeamSide side)
    {
        int lvl = Mathf.Max(1, level);
        int maxHp = ScaleStat(def.baseStats.hp, 0f, lvl);
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
            atk = ScaleStat(def.baseStats.atk, 0f, lvl),
            def = ScaleStat(def.baseStats.def, 0f, lvl),
            spd = ScaleStat(def.baseStats.spd, 0f, lvl),
            pot = ScaleStat(def.baseStats.pot, 0f, lvl),
            basicSkillId = def.skills?.basic,
            ultSkillId = def.skills?.ult,
            ultCdRemaining = 0,
            energy = 0,
            maxEnergy = 100,
            actionGauge = 0f,
            shield = 0,
            statuses = new List<StatusInstance>(),
        };
    }

    private int ScaleStat(int baseValue, float growth, int level)
    {
        return Mathf.Max(1, Mathf.RoundToInt(baseValue + (growth * (level - 1))));
    }

    private void HandleBattleOutcome(bool won)
    {
        if (won && Game.Data.Stages.TryGetValue(stageId, out var stage) && stage.rewards != null)
        {
            Save.gold += stage.rewards.gold;
            Save.spores += stage.rewards.spores;
            Save.accountLevel += Mathf.Max(0, stage.rewards.accountXp);
            SaveSystem.Save(Save);
        }

        if (TutorialManager.I != null)
        {
            TutorialManager.I.OnFirstBattleCompleted(won);
        }

        if (won)
        {
            SetResult($"Victory! Cleared all {_lastWaveCount} wave(s).");
        }
        else
        {
            SetResult($"Defeat on wave {_lastWaveReached}/{_lastWaveCount}.");
        }
        RefreshStageUi();
    }

    private void EnsureData()
    {
        if (Game.Data == null)
        {
            Game.Data = new GameData();
            Game.Data.LoadAll();
        }
    }

    private void SetResult(string message)
    {
        if (battleResultLabel != null) battleResultLabel.text = message;
        Debug.Log($"[BattleScene] {message}");
    }

    private void RefreshStageUi()
    {
        if (stageInfoLabel != null)
        {
            if (Game.Data.Stages.TryGetValue(stageId, out var stage))
            {
                int waveCount = stage.waves == null ? 0 : stage.waves.Count;
                stageInfoLabel.text = $"{stage.name} ({stage.id})\nRec Power: {stage.recommendedPower}\nWaves: {waveCount}\nRewards: +{stage.rewards.gold} Gold, +{stage.rewards.spores} Spores";
            }
            else
            {
                stageInfoLabel.text = $"Stage {stageId} not found.";
            }
        }

        if (teamPreviewLabel != null)
        {
            string summary = Save.activeTeam.Count == 0 ? "Auto-team will be used." : string.Join(", ", Save.activeTeam);
            teamPreviewLabel.text = $"Current Team: {summary}";
        }
    }
}
