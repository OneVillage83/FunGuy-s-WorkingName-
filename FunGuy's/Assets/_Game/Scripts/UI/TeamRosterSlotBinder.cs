using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TeamRosterSlotBinder : MonoBehaviour
{
    [SerializeField] private TeamMenuController teamController;
    [SerializeField] private Button[] slotButtons;
    [SerializeField] private Text[] slotLabels;
    [SerializeField] private bool bindOnStart = true;
    [SerializeField] private bool autoDiscoverSlots = true;

    private PlayerSave Save => Game.Save ??= SaveSystem.LoadOrNew();

    private void Start()
    {
        AutoDiscoverIfNeeded();
        if (bindOnStart) RebindSlots();
    }

    public void RebindSlots()
    {
        AutoDiscoverIfNeeded();
        if (teamController == null || slotButtons == null || slotButtons.Length == 0) return;

        EnsureData();
        List<OwnedUnit> ranked = Save.units
            .OrderByDescending(u => Game.Data.Characters.TryGetValue(u.charId, out var c) ? c.rarity : 0)
            .ThenByDescending(u => u.level)
            .ToList();

        for (int i = 0; i < slotButtons.Length; i++)
        {
            var button = slotButtons[i];
            if (button == null) continue;
            button.onClick.RemoveAllListeners();

            if (i >= ranked.Count)
            {
                button.interactable = false;
                SetLabel(i, "-- Empty --");
                continue;
            }

            button.interactable = true;
            string charId = ranked[i].charId;
            string name = Game.Data.Characters.TryGetValue(charId, out var def) ? def.name : charId;
            int rarity = Game.Data.Characters.TryGetValue(charId, out var cdef) ? cdef.rarity : 0;
            SetLabel(i, $"{name} {IdleHuntressTheme.Stars(rarity)}");
            button.onClick.AddListener(() =>
            {
                teamController.ToggleUnitInTeam(charId);
                RebindSlots();
            });
        }
    }

    private void SetLabel(int index, string text)
    {
        if (slotLabels == null || index < 0 || index >= slotLabels.Length) return;
        if (slotLabels[index] != null) slotLabels[index].text = text;
    }

    private void EnsureData()
    {
        if (Game.Data != null) return;
        Game.Data = new GameData();
        Game.Data.LoadAll();
    }

    private void AutoDiscoverIfNeeded()
    {
        if (!autoDiscoverSlots) return;

        if (teamController == null)
        {
            teamController = GetComponentInParent<TeamMenuController>(true)
                             ?? FindObjectsByType<TeamMenuController>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        }

        if (slotButtons == null || slotButtons.Length == 0)
        {
            slotButtons = GetComponentsInChildren<Button>(true)
                .Where(b => b != null && b.name.StartsWith("Btn_RosterSlot", System.StringComparison.OrdinalIgnoreCase))
                .OrderBy(b => b.name, System.StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        if ((slotLabels == null || slotLabels.Length == 0) && slotButtons != null && slotButtons.Length > 0)
        {
            var labels = new List<Text>();
            foreach (var btn in slotButtons)
            {
                var explicitLabel = btn.GetComponentsInChildren<Text>(true)
                    .FirstOrDefault(t => t.name.StartsWith("Lbl_RosterSlot", System.StringComparison.OrdinalIgnoreCase));
                if (explicitLabel != null)
                {
                    labels.Add(explicitLabel);
                    continue;
                }

                var anyLabel = btn.GetComponentInChildren<Text>(true);
                labels.Add(anyLabel);
            }
            slotLabels = labels.ToArray();
        }
    }
}
