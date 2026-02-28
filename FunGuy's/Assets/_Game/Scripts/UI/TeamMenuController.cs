using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TeamMenuController : MonoBehaviour
{
    [SerializeField] private int maxTeamSize = 5;
    [SerializeField] private string battleSceneName = "Battle";
    [SerializeField] private Text teamStatusLabel;
    [SerializeField] private Text rosterLabel;
    [SerializeField] private Text hintLabel;
    [SerializeField] private bool autoSeedFromSaveIfTeamEmpty = true;

    private PlayerSave Save => Game.Save ??= SaveSystem.LoadOrNew();

    private void Start()
    {
        if (autoSeedFromSaveIfTeamEmpty && Save.activeTeam.Count == 0 && Save.units.Count > 0)
        {
            AutoFillTeam();
        }
        RefreshStatus();
    }

    public void AddUnitToTeam(string charId)
    {
        if (string.IsNullOrWhiteSpace(charId)) return;
        if (!Save.units.Any(u => u.charId == charId)) return;
        if (Save.activeTeam.Contains(charId)) return;
        if (Save.activeTeam.Count >= maxTeamSize) return;

        Save.activeTeam.Add(charId);
        SaveSystem.Save(Save);
        AdvanceTutorialOnTeamPlacement();
        RefreshStatus();
    }

    public void RemoveUnitFromTeam(string charId)
    {
        if (!Save.activeTeam.Remove(charId)) return;
        SaveSystem.Save(Save);
        RefreshStatus();
    }

    public void ToggleUnitInTeam(string charId)
    {
        if (string.IsNullOrWhiteSpace(charId)) return;
        if (Save.activeTeam.Contains(charId)) RemoveUnitFromTeam(charId);
        else AddUnitToTeam(charId);
    }

    public void AddUnitByRosterIndex(int rosterIndex)
    {
        var ranked = RankedOwnedUnits();
        if (rosterIndex < 0 || rosterIndex >= ranked.Count) return;
        AddUnitToTeam(ranked[rosterIndex].charId);
    }

    public void RemoveUnitByTeamIndex(int teamIndex)
    {
        if (teamIndex < 0 || teamIndex >= Save.activeTeam.Count) return;
        RemoveUnitFromTeam(Save.activeTeam[teamIndex]);
    }

    public void AutoFillTeam()
    {
        var ranked = RankedOwnedUnits()
            .Take(maxTeamSize)
            .Select(u => u.charId)
            .ToList();

        Save.activeTeam = ranked;
        SaveSystem.Save(Save);
        AdvanceTutorialOnTeamPlacement();
        RefreshStatus();
    }

    public void OnClearTeamPressed()
    {
        Save.activeTeam.Clear();
        SaveSystem.Save(Save);
        RefreshStatus();
    }

    public void OnStartBattlePressed()
    {
        if (Save.activeTeam.Count == 0)
        {
            AutoFillTeam();
        }

        SaveSystem.Save(Save);

        if (TutorialManager.I != null && !Save.tutorialCompleted)
        {
            var step = (TutorialStep)Save.tutorialStep;
            if (step == TutorialStep.GoToTeamBuilder || step == TutorialStep.PlaceFirstUnit)
            {
                TutorialManager.I.GoToStep(TutorialStep.StartFirstBattle);
                return;
            }
        }

        SceneManager.LoadScene(battleSceneName);
    }

    public void OnBackPressed()
    {
        SceneManager.LoadScene("Home");
    }

    private void AdvanceTutorialOnTeamPlacement()
    {
        if (TutorialManager.I == null || Save.tutorialCompleted) return;
        if ((TutorialStep)Save.tutorialStep == TutorialStep.GoToTeamBuilder)
        {
            TutorialManager.I.GoToStep(TutorialStep.PlaceFirstUnit);
        }
    }

    private int GetRarity(string charId)
    {
        if (Game.Data == null || Game.Data.Characters == null) return 0;
        return Game.Data.Characters.TryGetValue(charId, out var c) ? c.rarity : 0;
    }

    private void EnsureData()
    {
        if (Game.Data != null) return;
        Game.Data = new GameData();
        Game.Data.LoadAll();
    }

    private void RefreshStatus()
    {
        EnsureData();

        if (teamStatusLabel != null)
        {
            string roster = Save.activeTeam.Count == 0
                ? "No units selected"
                : string.Join(", ", Save.activeTeam.Select((id, i) => $"[{i + 1}] {ResolveName(id)}"));
            teamStatusLabel.text = $"Team ({Save.activeTeam.Count}/{maxTeamSize}): {roster}";
        }

        if (rosterLabel != null)
        {
            var lines = RankedOwnedUnits()
                .Select(u =>
                {
                    var rarity = GetRarity(u.charId);
                    string stars = IdleHuntressTheme.Stars(rarity);
                    string marker = Save.activeTeam.Contains(u.charId) ? "[In Team]" : "[Bench]";
                    string name = ResolveName(u.charId);
                    return $"{marker} {name} {stars} Lv.{u.level}  (ID: {u.charId})";
                })
                .ToList();

            rosterLabel.text = lines.Count == 0 ? "No units owned yet. Visit Summon." : string.Join("\n", lines);
        }

        if (hintLabel != null)
        {
            hintLabel.text = Save.activeTeam.Count < 3
                ? "Tip: build at least 3 units for smoother stage clears."
                : "Tip: frontload tanks, backline DPS/Tactician for better tempo.";
        }
    }

    private List<OwnedUnit> RankedOwnedUnits()
    {
        EnsureData();
        return Save.units
            .OrderByDescending(u => GetRarity(u.charId))
            .ThenByDescending(u => u.level)
            .ToList();
    }

    private string ResolveName(string charId)
    {
        return Game.Data.Characters.TryGetValue(charId, out var c) ? c.name : charId;
    }
}
