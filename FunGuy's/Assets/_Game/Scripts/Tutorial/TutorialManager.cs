using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// Enum declared outside class for save-step serialization.
public enum TutorialStep
{
    Welcome = 0,
    ShowSummonPool = 1,
    GiveTicket = 2,
    DoFirstSummon = 3,
    GoToTeamBuilder = 4,
    PlaceFirstUnit = 5,
    StartFirstBattle = 6,
    BattleWinRewards = 7,
    ExplainTankDps = 8,
    PlaceTankFrontDpsBack = 9,
    Complete = 10
}

// Tutorial controller.
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager I { get; private set; }

    [SerializeField] private TutorialOverlay overlay;
    [SerializeField] private int minimumStarterUnits = 3;
    [SerializeField] private string[] preferredStarterIds =
    {
        "c_barkrot_thane",
        "c_puffmage_orbi",
        "c_mosswhisper_luma",
    };

    private PlayerSave save;

    private void Awake()
    {
        if (I != null)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        save = Game.Save ??= SaveSystem.LoadOrNew();
        EnsureData();
        EnsureStarterProfileForFirstRun();

        if (save.tutorialCompleted)
        {
            if (overlay != null) overlay.Hide();
            return;
        }

        if (save.tutorialStep < 0 || save.tutorialStep > (int)TutorialStep.Complete)
            save.tutorialStep = (int)TutorialStep.Welcome;

        GoToStep((TutorialStep)save.tutorialStep);
    }

    public void GoToStep(TutorialStep step)
    {
        save.tutorialStep = (int)step;
        SaveSystem.Save(save);

        if (overlay != null) overlay.Show();

        switch (step)
        {
            case TutorialStep.Welcome:
                Present(
                    "Welcome, sproutling. Let's walk through menus, summoning, and your first battle.",
                    ContinueTutorial
                );
                break;

            case TutorialStep.ShowSummonPool:
                Present(
                    "This is the Summon menu. Pull your first unit to build your team.",
                    ContinueTutorial
                );
                SceneManager.LoadScene("Summon");
                break;

            case TutorialStep.GiveTicket:
                save.tutorialTickets = Mathf.Max(1, save.tutorialTickets);
                SaveSystem.Save(save);
                Present(
                    "You received a tutorial summon ticket. Use it now.",
                    ContinueTutorial
                );
                break;

            case TutorialStep.DoFirstSummon:
                Present(
                    "Summon one fighter. After the pull, continue to Team setup.",
                    null
                );
                break;

            case TutorialStep.GoToTeamBuilder:
                Present(
                    "Open Team and place your first unit.",
                    ContinueTutorial
                );
                SceneManager.LoadScene("Team");
                break;

            case TutorialStep.PlaceFirstUnit:
                Present(
                    "Place a Tank in front and a damage unit in back when available.",
                    ContinueTutorial
                );
                break;

            case TutorialStep.StartFirstBattle:
                Present(
                    "Start your first combat. Focus on turn order and signature skill timing.",
                    null
                );
                SceneManager.LoadScene("Battle");
                break;

            case TutorialStep.BattleWinRewards:
                save.gold += 100;
                save.spores += 10;
                SaveSystem.Save(save);
                Present(
                    "Battle clear rewards granted. Upgrade and continue your journey.",
                    ContinueTutorial
                );
                break;

            case TutorialStep.ExplainTankDps:
                Present(
                    "Tanks protect your line. DPS and Assassins finish priority targets.",
                    ContinueTutorial
                );
                break;

            case TutorialStep.PlaceTankFrontDpsBack:
                Present(
                    "Team strategy check: front = survivability, back = damage/control.",
                    ContinueTutorial
                );
                break;

            case TutorialStep.Complete:
                save.tutorialCompleted = true;
                SaveSystem.Save(save);
                if (overlay != null) overlay.Hide();
                SceneManager.LoadScene("Home");
                break;
        }
    }

    public void ContinueTutorial()
    {
        if (save == null || save.tutorialCompleted) return;
        var current = (TutorialStep)save.tutorialStep;
        if (current == TutorialStep.Complete) return;
        GoToStep(current + 1);
    }

    public void OnFirstSummonCompleted()
    {
        if (save == null || save.tutorialCompleted) return;
        if ((TutorialStep)save.tutorialStep <= TutorialStep.DoFirstSummon)
            GoToStep(TutorialStep.GoToTeamBuilder);
    }

    public void OnFirstBattleCompleted(bool won)
    {
        if (!won || save == null || save.tutorialCompleted) return;
        if ((TutorialStep)save.tutorialStep <= TutorialStep.StartFirstBattle)
            GoToStep(TutorialStep.BattleWinRewards);
    }

    private void Present(string message, System.Action onContinue)
    {
        if (overlay != null) overlay.Say(message, onContinue);
        else
        {
            Debug.Log($"[Tutorial] {message}");
            onContinue?.Invoke();
        }
    }

    private void EnsureData()
    {
        if (Game.Data != null) return;
        Game.Data = new GameData();
        Game.Data.LoadAll();
    }

    private void EnsureStarterProfileForFirstRun()
    {
        if (save == null || save.tutorialCompleted || Game.Data == null) return;

        bool changed = false;
        if (save.tutorialTickets < 1)
        {
            save.tutorialTickets = 1;
            changed = true;
        }

        int targetCount = Mathf.Clamp(minimumStarterUnits, 1, 5);
        var ownedIds = new HashSet<string>(
            save.units.Where(u => u != null && !string.IsNullOrWhiteSpace(u.charId)).Select(u => u.charId),
            System.StringComparer.OrdinalIgnoreCase);

        if (save.units.Count < targetCount)
        {
            foreach (var charId in ResolveStarterIds())
            {
                if (string.IsNullOrWhiteSpace(charId) || ownedIds.Contains(charId)) continue;
                save.units.Add(new OwnedUnit
                {
                    charId = charId,
                    level = 3,
                    copies = 1,
                    stars = 1,
                    coreLevel = 1,
                    xp = 0,
                    gearSlots = new List<GearSlotState>(),
                });
                ownedIds.Add(charId);
                changed = true;
                if (save.units.Count >= targetCount) break;
            }
        }

        save.activeTeam.RemoveAll(charId => string.IsNullOrWhiteSpace(charId) || !ownedIds.Contains(charId));
        if (save.activeTeam.Count == 0 && save.units.Count > 0)
        {
            save.activeTeam = save.units
                .Where(u => u != null && !string.IsNullOrWhiteSpace(u.charId))
                .Select(u => u.charId)
                .Take(targetCount)
                .ToList();
            changed = true;
        }

        if (!changed) return;
        SaveSystem.Save(save);
        Game.Save = save;
    }

    private IEnumerable<string> ResolveStarterIds()
    {
        var ordered = new List<string>();

        if (preferredStarterIds != null)
        {
            foreach (var id in preferredStarterIds)
            {
                if (string.IsNullOrWhiteSpace(id)) continue;
                if (!Game.Data.Characters.ContainsKey(id)) continue;
                ordered.Add(id);
            }
        }

        var roleFallback = new[] { "Tank", "DPS", "Healer" };
        foreach (var role in roleFallback)
        {
            var pick = Game.Data.Characters.Values
                .Where(c => c != null && string.Equals(c.role, role, System.StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(c => c.rarity)
                .ThenBy(c => c.id, System.StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
            if (pick != null) ordered.Add(pick.id);
        }

        foreach (var candidate in Game.Data.Characters.Values
                     .Where(c => c != null)
                     .OrderByDescending(c => c.rarity)
                     .ThenBy(c => c.id, System.StringComparer.OrdinalIgnoreCase))
        {
            ordered.Add(candidate.id);
        }

        return ordered.Distinct(System.StringComparer.OrdinalIgnoreCase);
    }
}
