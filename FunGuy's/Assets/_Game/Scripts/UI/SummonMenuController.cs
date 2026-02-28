using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SummonMenuController : MonoBehaviour
{
    [SerializeField] private string defaultBannerId = "b_event_starspore";
    [SerializeField] private Text resultLabel;
    [SerializeField] private Text sporesLabel;
    [SerializeField] private Text bannerLabel;
    [SerializeField] private SummonRevealController revealController;
    [SerializeField] private Button pullOneButton;
    [SerializeField] private Button pullTenButton;

    private const int MultiPullCount = 10;
    private bool _busy;

    private PlayerSave Save => Game.Save ??= SaveSystem.LoadOrNew();

    private void Start()
    {
        EnsureServices();
        RefreshUi();
    }

    public void OnPullOnePressed()
    {
        ExecutePulls(1);
    }

    public void OnPullTenPressed()
    {
        ExecutePulls(MultiPullCount);
    }

    public void OnBackPressed()
    {
        SceneManager.LoadScene("Home");
    }

    private void ExecutePulls(int count)
    {
        if (_busy) return;
        EnsureServices();
        var pulledIds = new List<string>();
        bool usedTicket = false;

        for (int i = 0; i < count; i++)
        {
            bool consumeCurrency = true;
            if (!Save.tutorialCompleted && Save.tutorialTickets > 0 && count == 1 && i == 0)
            {
                Save.tutorialTickets -= 1;
                consumeCurrency = false;
                usedTicket = true;
            }

            try
            {
                pulledIds.Add(Game.Gacha.PullOne(Save, defaultBannerId, consumeCurrency));
            }
            catch (Exception ex)
            {
                if (pulledIds.Count == 0) SetResult($"Summon failed: {ex.Message}");
                break;
            }
        }

        if (pulledIds.Count > 0)
        {
            SaveSystem.Save(Save);
            NotifyTutorialOnFirstSummon();
            RefreshUi();
            PlayRevealOrFallback(pulledIds, usedTicket);
        }
    }

    private void EnsureServices()
    {
        if (Game.Data == null)
        {
            Game.Data = new GameData();
            Game.Data.LoadAll();
        }

        if (Game.Gacha == null)
        {
            Game.Gacha = new GachaService(Game.Data);
        }
    }

    private void NotifyTutorialOnFirstSummon()
    {
        if (TutorialManager.I == null) return;
        TutorialManager.I.OnFirstSummonCompleted();
    }

    private string BuildResultSummary(List<string> pulledIds, bool usedTicket)
    {
        var names = pulledIds
            .Select(id => Game.Data.Characters.TryGetValue(id, out var c) ? $"{c.name} ({c.rarity}★)" : id)
            .ToList();

        string prefix = usedTicket ? "Tutorial ticket used.\n" : string.Empty;
        return $"{prefix}Pulled {pulledIds.Count}:\n{string.Join(", ", names)}";
    }

    private void RefreshUi()
    {
        if (Game.Data != null && Game.Data.Banners.TryGetValue(defaultBannerId, out var banner) && bannerLabel != null)
        {
            bannerLabel.text = banner.name;
        }

        if (sporesLabel != null)
        {
            sporesLabel.text = $"Spores: {Save.spores}";
        }
    }

    private void SetResult(string message)
    {
        if (resultLabel != null) resultLabel.text = message;
        Debug.Log($"[Summon] {message}");
    }

    private void PlayRevealOrFallback(List<string> pulledIds, bool usedTicket)
    {
        var pulledCharacters = pulledIds
            .Where(id => Game.Data.Characters.ContainsKey(id))
            .Select(id => Game.Data.Characters[id])
            .ToList();

        if (revealController == null)
        {
            SetResult(BuildResultSummary(pulledIds, usedTicket));
            return;
        }

        _busy = true;
        SetPullButtonsInteractable(false);
        revealController.Play(pulledCharacters, () =>
        {
            _busy = false;
            SetPullButtonsInteractable(true);
            SetResult(BuildResultSummary(pulledIds, usedTicket));
        });
    }

    private void SetPullButtonsInteractable(bool interactable)
    {
        if (pullOneButton != null) pullOneButton.interactable = interactable;
        if (pullTenButton != null) pullTenButton.interactable = interactable;
    }
}
