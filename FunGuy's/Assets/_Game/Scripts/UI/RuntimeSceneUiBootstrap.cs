using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class RuntimeSceneUiBootstrap
{
    private static Font _cachedFont;
    private static bool _registered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        if (_registered) return;
        _registered = true;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;

        EnsureEventSystem(scene);

        if (string.Equals(scene.name, "Boot", StringComparison.OrdinalIgnoreCase)) return;

        var canvas = EnsureCanvas(scene);

        switch (scene.name)
        {
            case "Home":
                EnsureHomeScene(scene, canvas);
                break;
            case "Summon":
                EnsureSummonScene(scene, canvas);
                break;
            case "Team":
                EnsureTeamScene(scene, canvas);
                break;
            case "Battle":
                EnsureBattleScene(scene, canvas);
                break;
            case "Tutorial":
                EnsureTutorialScene(scene, canvas);
                break;
            case "Options":
                EnsureOptionsScene(scene, canvas);
                break;
        }

        foreach (var binder in FindInScene<UiPrefabBlueprintBinder>(scene))
        {
            if (binder == null) continue;
            binder.AutoBindCommonReferences();
        }
    }

    private static void EnsureHomeScene(Scene scene, Canvas canvas)
    {
        var root = EnsureSceneRoot(scene, canvas.transform, "HomeRoot");
        var controller = EnsureSceneComponent<HomeMenuController>(scene, root.transform);
        var spotlight = EnsureComponent<TutorialSpotlightController>(root);

        var bg = EnsurePanel(root.transform, "Img_Background", IdleHuntressTheme.BackgroundFor(UiTone.Home),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var shell = EnsurePanel(root.transform, "Panel_Main",
            WithAlpha(IdleHuntressTheme.PanelFor(UiTone.Home), 0.96f),
            CenterAnchor, CenterAnchor, Vector2.zero, new Vector2(900f, 1520f));

        var title = EnsureLabel(shell.transform, "Lbl_Welcome",
            "Welcome, Commander. Build your squad and clear the frontier.",
            42, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetRect(title.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, 600f), new Vector2(820f, 150f));

        var stats = EnsureLabel(shell.transform, "Lbl_AccountStats",
            "Lv.1  Gold:100  Spores:50", 30, FontStyle.Normal, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(stats.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, 500f), new Vector2(820f, 100f));

        var start = EnsureButton(shell.transform, "Btn_Start", "Start", new Vector2(0f, 320f), new Vector2(560f, 108f), IdleHuntressTheme.AccentFor(UiTone.Home), out var startLabel);
        var summon = EnsureButton(shell.transform, "Btn_Summon", "Summon", new Vector2(0f, 190f), new Vector2(560f, 108f), IdleHuntressTheme.AccentFor(UiTone.Home), out var summonLabel);
        var team = EnsureButton(shell.transform, "Btn_Team", "Team", new Vector2(0f, 60f), new Vector2(560f, 108f), IdleHuntressTheme.AccentFor(UiTone.Home), out var teamLabel);
        var battle = EnsureButton(shell.transform, "Btn_Battle", "Battle", new Vector2(0f, -70f), new Vector2(560f, 108f), IdleHuntressTheme.AccentFor(UiTone.Home), out var battleLabel);
        var options = EnsureButton(shell.transform, "Btn_Options", "Options", new Vector2(0f, -200f), new Vector2(560f, 108f), IdleHuntressTheme.AccentFor(UiTone.Home), out var optionsLabel);

        var hint = EnsureLabel(shell.transform, "Lbl_TutorialHint", string.Empty, 24, FontStyle.Italic, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(hint.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, -360f), new Vector2(780f, 140f));

        ConfigureSkin(root, UiTone.Home,
            new[] { bg.GetComponent<Image>() },
            new[] { shell.GetComponent<Image>() },
            new[] { start.GetComponent<Image>(), summon.GetComponent<Image>(), team.GetComponent<Image>(), battle.GetComponent<Image>(), options.GetComponent<Image>() },
            new[] { start, summon, team, battle, options },
            new[] { title },
            new[] { stats, hint, startLabel, summonLabel, teamLabel, battleLabel, optionsLabel });

        spotlight.Configure(
            hint,
            new[]
            {
                Target(TutorialStep.Welcome, start, "Tutorial begins automatically for new players."),
            });

        // Keep the home controller on root so binder can wire all local controls.
        controller.transform.SetParent(root.transform, false);
    }

    private static void EnsureSummonScene(Scene scene, Canvas canvas)
    {
        var root = EnsureSceneRoot(scene, canvas.transform, "SummonRoot");
        var controller = EnsureSceneComponent<SummonMenuController>(scene, root.transform);
        var spotlight = EnsureComponent<TutorialSpotlightController>(root);

        var bg = EnsurePanel(root.transform, "Img_Background", IdleHuntressTheme.BackgroundFor(UiTone.Summon),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var shell = EnsurePanel(root.transform, "Panel_Main",
            WithAlpha(IdleHuntressTheme.PanelFor(UiTone.Summon), 0.96f),
            CenterAnchor, CenterAnchor, Vector2.zero, new Vector2(920f, 1540f));

        var bannerLabel = EnsureLabel(shell.transform, "Lbl_Banner", "Starseed Bloom Event Banner",
            40, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetRect(bannerLabel.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, 610f), new Vector2(840f, 120f));

        var sporesLabel = EnsureLabel(shell.transform, "Lbl_Spores", "Spores: 50",
            30, FontStyle.Normal, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(sporesLabel.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, 520f), new Vector2(840f, 90f));

        var pullOne = EnsureButton(shell.transform, "Btn_PullOne", "Pull x1", new Vector2(0f, 350f), new Vector2(540f, 108f), IdleHuntressTheme.AccentFor(UiTone.Summon), out var pullOneLabel);
        var pullTen = EnsureButton(shell.transform, "Btn_PullTen", "Pull x10", new Vector2(0f, 220f), new Vector2(540f, 108f), IdleHuntressTheme.AccentFor(UiTone.Summon), out var pullTenLabel);
        var back = EnsureButton(shell.transform, "Btn_Back", "Back", new Vector2(0f, 90f), new Vector2(540f, 108f), IdleHuntressTheme.AccentFor(UiTone.Summon), out var backLabel);

        var result = EnsureLabel(shell.transform, "Lbl_Result",
            "Summon results appear here.", 28, FontStyle.Normal, TextAnchor.UpperLeft, SoftWhite);
        SetRect(result.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, -280f), new Vector2(820f, 640f));

        var hint = EnsureLabel(shell.transform, "Lbl_TutorialHint", string.Empty, 24, FontStyle.Italic, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(hint.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, -610f), new Vector2(820f, 120f));

        var revealRoot = EnsurePanel(root.transform, "SummonReveal", new Color(0f, 0f, 0f, 0.85f),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var revealCard = EnsurePanel(revealRoot.transform, "Panel_RevealCard",
            WithAlpha(IdleHuntressTheme.PanelFor(UiTone.Summon), 0.98f),
            CenterAnchor, CenterAnchor, Vector2.zero, new Vector2(740f, 980f));

        var revealTitle = EnsureLabel(revealCard.transform, "Lbl_RevealTitle", "New Funguy Acquired",
            40, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetRect(revealTitle.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, 360f), new Vector2(640f, 120f));

        var revealGlow = EnsurePanel(revealCard.transform, "Img_RarityGlow",
            WithAlpha(IdleHuntressTheme.AccentFor(UiTone.Summon), 0.35f),
            CenterAnchor, CenterAnchor, new Vector2(0f, 80f), new Vector2(460f, 460f));
        var revealFrame = EnsurePanel(revealCard.transform, "Img_RarityFrame",
            IdleHuntressTheme.RarityColor(4),
            CenterAnchor, CenterAnchor, new Vector2(0f, 80f), new Vector2(360f, 360f));
        var revealName = EnsureLabel(revealCard.transform, "Lbl_RevealName", "Unknown Funguy",
            36, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetRect(revealName.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, -180f), new Vector2(640f, 100f));

        var revealRarity = EnsureLabel(revealCard.transform, "Lbl_RevealRarity", "★★★",
            30, FontStyle.Normal, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(revealRarity.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, -250f), new Vector2(640f, 90f));

        var revealNext = EnsureButton(revealCard.transform, "Btn_RevealNext", "Next", new Vector2(0f, -360f), new Vector2(420f, 100f), IdleHuntressTheme.AccentFor(UiTone.Summon), out var revealNextLabel);

        var revealController = EnsureSceneComponent<SummonRevealController>(scene, revealRoot.transform);
        revealController.transform.SetParent(revealRoot.transform, false);
        revealRoot.SetActive(false);

        ConfigureSkin(root, UiTone.Summon,
            new[] { bg.GetComponent<Image>() },
            new[] { shell.GetComponent<Image>(), revealCard.GetComponent<Image>() },
            new[] { pullOne.GetComponent<Image>(), pullTen.GetComponent<Image>(), back.GetComponent<Image>(), revealNext.GetComponent<Image>(), revealGlow.GetComponent<Image>(), revealFrame.GetComponent<Image>() },
            new[] { pullOne, pullTen, back, revealNext },
            new[] { bannerLabel, revealTitle, revealName },
            new[] { sporesLabel, result, hint, pullOneLabel, pullTenLabel, backLabel, revealRarity, revealNextLabel });

        spotlight.Configure(
            hint,
            new[]
            {
                Target(TutorialStep.ShowSummonPool, pullOne, "Your first stop is this banner. Continue and pull once."),
                Target(TutorialStep.GiveTicket, pullOne, "Use the tutorial ticket to perform your first summon."),
                Target(TutorialStep.DoFirstSummon, pullOne, "Pull one unit to continue onboarding."),
            });

        controller.transform.SetParent(root.transform, false);
    }

    private static void EnsureTeamScene(Scene scene, Canvas canvas)
    {
        var root = EnsureSceneRoot(scene, canvas.transform, "TeamRoot");
        var controller = EnsureSceneComponent<TeamMenuController>(scene, root.transform);
        var slotBinder = EnsureSceneComponent<TeamRosterSlotBinder>(scene, root.transform);
        var spotlight = EnsureComponent<TutorialSpotlightController>(root);

        var bg = EnsurePanel(root.transform, "Img_Background", IdleHuntressTheme.BackgroundFor(UiTone.Team),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var shell = EnsurePanel(root.transform, "Panel_Main",
            WithAlpha(IdleHuntressTheme.PanelFor(UiTone.Team), 0.96f),
            CenterAnchor, CenterAnchor, Vector2.zero, new Vector2(940f, 1560f));

        var teamStatus = EnsureLabel(shell.transform, "Lbl_TeamStatus", "Team (0/5): No units selected",
            29, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
        SetRect(teamStatus.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, 620f), new Vector2(860f, 110f));

        var hint = EnsureLabel(shell.transform, "Lbl_Hint", "Tip: build at least 3 units for smoother stage clears.",
            24, FontStyle.Italic, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(hint.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, 500f), new Vector2(860f, 90f));

        var slotStrip = EnsurePanel(shell.transform, "Panel_RosterSlots",
            WithAlpha(IdleHuntressTheme.BackgroundFor(UiTone.Team), 0.55f),
            CenterAnchor, CenterAnchor, new Vector2(0f, 340f), new Vector2(860f, 130f));

        var slotButtons = new List<Button>();
        var slotLabels = new List<Text>();
        float firstX = -320f;
        for (int i = 0; i < 5; i++)
        {
            float x = firstX + (i * 160f);
            var slot = EnsureButton(slotStrip.transform, $"Btn_RosterSlot{i + 1}", $"Slot {i + 1}",
                new Vector2(x, 0f), new Vector2(150f, 86f), IdleHuntressTheme.AccentFor(UiTone.Team), out var slotLabel, $"Lbl_RosterSlot{i + 1}");
            slotButtons.Add(slot);
            slotLabels.Add(slotLabel);
        }

        var roster = EnsureLabel(shell.transform, "Lbl_Roster", "No units owned yet. Visit Summon.",
            25, FontStyle.Normal, TextAnchor.UpperLeft, SoftWhite);
        SetRect(roster.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, -20f), new Vector2(860f, 700f));

        var autoFill = EnsureButton(shell.transform, "Btn_AutoFill", "Auto Fill", new Vector2(-210f, -610f), new Vector2(240f, 92f), IdleHuntressTheme.AccentFor(UiTone.Team), out var autoFillLabel);
        var clearTeam = EnsureButton(shell.transform, "Btn_ClearTeam", "Clear Team", new Vector2(50f, -610f), new Vector2(240f, 92f), IdleHuntressTheme.AccentFor(UiTone.Team), out var clearLabel);
        var startBattle = EnsureButton(shell.transform, "Btn_StartBattle", "Start Battle", new Vector2(310f, -610f), new Vector2(240f, 92f), IdleHuntressTheme.AccentFor(UiTone.Team), out var startBattleLabel);
        var back = EnsureButton(shell.transform, "Btn_Back", "Back", new Vector2(0f, -720f), new Vector2(500f, 92f), IdleHuntressTheme.AccentFor(UiTone.Team), out var backLabel);

        ConfigureSkin(root, UiTone.Team,
            new[] { bg.GetComponent<Image>() },
            new[] { shell.GetComponent<Image>(), slotStrip.GetComponent<Image>() },
            new[] { autoFill.GetComponent<Image>(), clearTeam.GetComponent<Image>(), startBattle.GetComponent<Image>(), back.GetComponent<Image>() }
                .Concat(slotButtons.Select(b => b.GetComponent<Image>())).ToArray(),
            new[] { autoFill, clearTeam, startBattle, back }.Concat(slotButtons).ToArray(),
            new[] { teamStatus },
            new[] { hint, roster, autoFillLabel, clearLabel, startBattleLabel, backLabel }
                .Concat(slotLabels).ToArray());

        spotlight.Configure(
            hint,
            new[]
            {
                Target(TutorialStep.GoToTeamBuilder, autoFill, "Auto-fill to place your first formation."),
                Target(TutorialStep.PlaceFirstUnit, slotButtons.FirstOrDefault(), "Tap roster slots to toggle team placement."),
                Target(TutorialStep.StartFirstBattle, startBattle, "When ready, start your first battle."),
            });

        controller.transform.SetParent(root.transform, false);
        slotBinder.transform.SetParent(root.transform, false);
    }

    private static void EnsureBattleScene(Scene scene, Canvas canvas)
    {
        var root = EnsureSceneRoot(scene, canvas.transform, "BattleRoot");
        var controller = EnsureSceneComponent<BattleSceneController>(scene, root.transform);
        var spotlight = EnsureComponent<TutorialSpotlightController>(root);

        var bg = EnsurePanel(root.transform, "Img_Background", IdleHuntressTheme.BackgroundFor(UiTone.Battle),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var shell = EnsurePanel(root.transform, "Panel_Main",
            WithAlpha(IdleHuntressTheme.PanelFor(UiTone.Battle), 0.96f),
            CenterAnchor, CenterAnchor, Vector2.zero, new Vector2(940f, 1540f));

        var stageInfo = EnsureLabel(shell.transform, "Lbl_StageInfo", "Stage info will appear here.",
            28, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
        SetRect(stageInfo.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, 620f), new Vector2(860f, 220f));

        var teamPreview = EnsureLabel(shell.transform, "Lbl_TeamPreview", "Current Team: Auto-team will be used.",
            26, FontStyle.Normal, TextAnchor.UpperLeft, SoftWhite);
        SetRect(teamPreview.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, 430f), new Vector2(860f, 130f));

        var battleResult = EnsureLabel(shell.transform, "Lbl_BattleResult", "Run battle to simulate combat.",
            30, FontStyle.Bold, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(battleResult.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, 180f), new Vector2(860f, 220f));

        var runBattle = EnsureButton(shell.transform, "Btn_RunBattle", "Run Battle", new Vector2(-230f, -150f), new Vector2(280f, 96f), IdleHuntressTheme.AccentFor(UiTone.Battle), out var runBattleLabel);
        var retry = EnsureButton(shell.transform, "Btn_Retry", "Retry", new Vector2(60f, -150f), new Vector2(240f, 96f), IdleHuntressTheme.AccentFor(UiTone.Battle), out var retryLabel);
        var nextStage = EnsureButton(shell.transform, "Btn_NextStage", "Next Stage", new Vector2(320f, -150f), new Vector2(240f, 96f), IdleHuntressTheme.AccentFor(UiTone.Battle), out var nextLabel);
        var goTeam = EnsureButton(shell.transform, "Btn_GoTeam", "Team", new Vector2(-160f, -270f), new Vector2(280f, 96f), IdleHuntressTheme.AccentFor(UiTone.Battle), out var teamLabel);
        var back = EnsureButton(shell.transform, "Btn_Back", "Back", new Vector2(170f, -270f), new Vector2(280f, 96f), IdleHuntressTheme.AccentFor(UiTone.Battle), out var backLabel);

        var hint = EnsureLabel(shell.transform, "Lbl_TutorialHint", string.Empty, 24, FontStyle.Italic, TextAnchor.MiddleCenter, SoftWhite);
        SetRect(hint.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, -420f), new Vector2(840f, 120f));

        ConfigureSkin(root, UiTone.Battle,
            new[] { bg.GetComponent<Image>() },
            new[] { shell.GetComponent<Image>() },
            new[] { runBattle.GetComponent<Image>(), retry.GetComponent<Image>(), nextStage.GetComponent<Image>(), goTeam.GetComponent<Image>(), back.GetComponent<Image>() },
            new[] { runBattle, retry, nextStage, goTeam, back },
            new[] { stageInfo, battleResult },
            new[] { teamPreview, hint, runBattleLabel, retryLabel, nextLabel, teamLabel, backLabel });

        spotlight.Configure(
            hint,
            new[]
            {
                Target(TutorialStep.StartFirstBattle, runBattle, "Run battle now to complete onboarding combat."),
            });

        controller.transform.SetParent(root.transform, false);
    }

    private static void EnsureTutorialScene(Scene scene, Canvas canvas)
    {
        // Tutorial manager and overlay are persistent once created.
        if (TutorialManager.I != null) return;

        var root = EnsureSceneRoot(scene, canvas.transform, "TutorialRoot");
        var manager = EnsureSceneComponent<TutorialManager>(scene, root.transform);

        var bg = EnsurePanel(root.transform, "Img_Background", WithAlpha(Color.black, 0.80f),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var overlayRoot = EnsurePanel(root.transform, "TutorialOverlayRoot", WithAlpha(Color.black, 0.72f),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var frame = EnsurePanel(overlayRoot.transform, "Panel_TutorialFrame",
            WithAlpha(IdleHuntressTheme.PanelFor(UiTone.Home), 0.98f),
            CenterAnchor, CenterAnchor, Vector2.zero, new Vector2(820f, 720f));

        var title = EnsureLabel(frame.transform, "Lbl_TutorialTitle", "Commander Tutorial",
            42, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetRect(title.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, 240f), new Vector2(700f, 110f));

        var message = EnsureLabel(frame.transform, "Lbl_TutorialMessage",
            "Welcome, sproutling. We will walk through summon, team setup, and first combat.",
            29, FontStyle.Normal, TextAnchor.UpperLeft, SoftWhite);
        SetRect(message.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, 20f), new Vector2(700f, 300f));

        var continueButton = EnsureButton(frame.transform, "Btn_TutorialContinue", "Continue",
            new Vector2(0f, -250f), new Vector2(420f, 96f), IdleHuntressTheme.AccentFor(UiTone.Home), out var continueLabel);

        var overlay = EnsureSceneComponent<TutorialOverlay>(scene, overlayRoot.transform);
        overlay.transform.SetParent(overlayRoot.transform, false);

        ConfigureSkin(root, UiTone.Home,
            new[] { bg.GetComponent<Image>(), overlayRoot.GetComponent<Image>() },
            new[] { frame.GetComponent<Image>() },
            new[] { continueButton.GetComponent<Image>() },
            new[] { continueButton },
            new[] { title },
            new[] { message, continueLabel });

        manager.transform.SetParent(root.transform, false);
        overlayRoot.SetActive(true);
    }

    private static void EnsureOptionsScene(Scene scene, Canvas canvas)
    {
        var root = EnsureSceneRoot(scene, canvas.transform, "OptionsRoot");
        var debugRoot = EnsureChild(root.transform, "DebugRoot");
        StretchToParent(EnsureRectTransform(debugRoot));

        var debugController = EnsureSceneComponent<DebugProgressionController>(scene, debugRoot.transform);
        var smokeController = EnsureSceneComponent<GameplaySmokeTestController>(scene, debugRoot.transform);

        var bg = EnsurePanel(debugRoot.transform, "Img_Background", IdleHuntressTheme.BackgroundFor(UiTone.Home),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var shell = EnsurePanel(debugRoot.transform, "Panel_Main",
            WithAlpha(IdleHuntressTheme.PanelFor(UiTone.Home), 0.96f),
            CenterAnchor, CenterAnchor, Vector2.zero, new Vector2(940f, 1540f));

        var title = EnsureLabel(shell.transform, "Lbl_OptionsTitle", "Debug / QA Controls",
            40, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetRect(title.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, 630f), new Vector2(860f, 120f));

        var status = EnsureLabel(shell.transform, "Lbl_DebugStatus", "Ready.",
            26, FontStyle.Normal, TextAnchor.MiddleLeft, SoftWhite);
        SetRect(status.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, 535f), new Vector2(860f, 90f));

        var reset = EnsureButton(shell.transform, "Btn_ResetSave", "Reset Save", new Vector2(-230f, 380f), new Vector2(300f, 88f), IdleHuntressTheme.AccentFor(UiTone.Home), out var resetLabel);
        var grant = EnsureButton(shell.transform, "Btn_GrantStarterResources", "Grant Resources", new Vector2(120f, 380f), new Vector2(330f, 88f), IdleHuntressTheme.AccentFor(UiTone.Home), out var grantLabel);
        var seed = EnsureButton(shell.transform, "Btn_SeedStarterRoster", "Seed Roster", new Vector2(-230f, 275f), new Vector2(300f, 88f), IdleHuntressTheme.AccentFor(UiTone.Home), out var seedLabel);
        var skip = EnsureButton(shell.transform, "Btn_SkipTutorial", "Skip Tutorial", new Vector2(120f, 275f), new Vector2(330f, 88f), IdleHuntressTheme.AccentFor(UiTone.Home), out var skipLabel);
        var openSummon = EnsureButton(shell.transform, "Btn_OpenSummon", "Open Summon", new Vector2(-230f, 170f), new Vector2(300f, 88f), IdleHuntressTheme.AccentFor(UiTone.Home), out var openSummonLabel);
        var runSmoke = EnsureButton(shell.transform, "Btn_RunSmokeTests", "Run Smoke Tests", new Vector2(120f, 170f), new Vector2(330f, 88f), IdleHuntressTheme.AccentFor(UiTone.Home), out var smokeLabel);
        var backHome = EnsureButton(shell.transform, "Btn_BackHome", "Back Home", new Vector2(0f, 60f), new Vector2(640f, 88f), IdleHuntressTheme.AccentFor(UiTone.Home), out var backLabel);

        var smokeOutput = EnsureLabel(shell.transform, "Lbl_SmokeOutput",
            "Smoke Test Result: not run.", 24, FontStyle.Normal, TextAnchor.UpperLeft, SoftWhite);
        SetRect(smokeOutput.rectTransform, CenterAnchor, CenterAnchor, new Vector2(0f, -370f), new Vector2(860f, 760f));

        ConfigureSkin(debugRoot, UiTone.Home,
            new[] { bg.GetComponent<Image>() },
            new[] { shell.GetComponent<Image>() },
            new[] { reset.GetComponent<Image>(), grant.GetComponent<Image>(), seed.GetComponent<Image>(), skip.GetComponent<Image>(), openSummon.GetComponent<Image>(), runSmoke.GetComponent<Image>(), backHome.GetComponent<Image>() },
            new[] { reset, grant, seed, skip, openSummon, runSmoke, backHome },
            new[] { title },
            new[] { status, smokeOutput, resetLabel, grantLabel, seedLabel, skipLabel, openSummonLabel, smokeLabel, backLabel });

        debugController.transform.SetParent(debugRoot.transform, false);
        smokeController.transform.SetParent(debugRoot.transform, false);
    }

    private static Canvas EnsureCanvas(Scene scene)
    {
        var existing = FindInScene<Canvas>(scene).FirstOrDefault();
        if (existing != null) return existing;

        var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(canvasGo, scene);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static void EnsureEventSystem(Scene scene)
    {
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        SceneManager.MoveGameObjectToScene(es, scene);
    }

    private static GameObject EnsureSceneRoot(Scene scene, Transform canvas, string rootName)
    {
        var root = EnsureChild(canvas, rootName);
        StretchToParent(EnsureRectTransform(root));
        EnsureComponent<UiPrefabBlueprintBinder>(root);
        return root;
    }

    private static T EnsureSceneComponent<T>(Scene scene, Transform parent) where T : Component
    {
        var existing = FindInScene<T>(scene).FirstOrDefault();
        if (existing != null)
        {
            if (parent != null && existing.transform.parent != parent)
            {
                existing.transform.SetParent(parent, false);
            }
            return existing;
        }

        return parent.gameObject.AddComponent<T>();
    }

    private static void ConfigureSkin(
        GameObject root,
        UiTone tone,
        Image[] backgrounds,
        Image[] panels,
        Image[] accents,
        Button[] buttons,
        Text[] titles,
        Text[] body)
    {
        var skin = EnsureComponent<IdleHuntressSkin>(root);
        SetPrivateField(skin, "tone", tone);
        SetPrivateField(skin, "backgroundLayers", backgrounds.Where(x => x != null).ToArray());
        SetPrivateField(skin, "panelLayers", panels.Where(x => x != null).ToArray());
        SetPrivateField(skin, "accentLayers", accents.Where(x => x != null).ToArray());
        SetPrivateField(skin, "primaryButtons", buttons.Where(x => x != null).ToArray());
        SetPrivateField(skin, "titleLabels", titles.Where(x => x != null).ToArray());
        SetPrivateField(skin, "bodyLabels", body.Where(x => x != null).ToArray());
        skin.ApplyTheme();
    }

    private static TutorialSpotlightTarget Target(TutorialStep step, Button button, string hint)
    {
        var graphic = button == null ? null : (button.targetGraphic ?? button.GetComponent<Graphic>());
        return new TutorialSpotlightTarget
        {
            step = step,
            target = graphic,
            hint = hint
        };
    }

    private static List<T> FindInScene<T>(Scene scene) where T : Component
    {
        var found = new List<T>();
        foreach (var root in scene.GetRootGameObjects())
        {
            found.AddRange(root.GetComponentsInChildren<T>(true));
        }
        return found;
    }

    private static GameObject EnsureChild(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing != null) return existing.gameObject;

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject EnsurePanel(
        Transform parent,
        string name,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        var go = EnsureChild(parent, name);
        var rt = EnsureRectTransform(go);
        SetRect(rt, anchorMin, anchorMax, anchoredPosition, sizeDelta);
        var image = EnsureComponent<Image>(go);
        image.color = color;
        image.raycastTarget = false;
        return go;
    }

    private static Button EnsureButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Color color,
        out Text labelText,
        string labelName = null)
    {
        var go = EnsureChild(parent, name);
        var rt = EnsureRectTransform(go);
        SetRect(rt, CenterAnchor, CenterAnchor, anchoredPosition, sizeDelta);

        var image = EnsureComponent<Image>(go);
        image.color = color;
        image.raycastTarget = true;

        var button = EnsureComponent<Button>(go);
        button.targetGraphic = image;

        labelText = EnsureLabel(
            go.transform,
            string.IsNullOrWhiteSpace(labelName) ? "Label" : labelName,
            label,
            30,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            Color.black);
        StretchToParent(labelText.rectTransform);

        return button;
    }

    private static Text EnsureLabel(
        Transform parent,
        string name,
        string text,
        int fontSize,
        FontStyle style,
        TextAnchor anchor,
        Color color)
    {
        var go = EnsureChild(parent, name);
        var label = EnsureComponent<Text>(go);
        label.font = ResolveFont();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = anchor;
        label.color = color;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.supportRichText = true;
        label.raycastTarget = false;
        return label;
    }

    private static RectTransform EnsureRectTransform(GameObject go)
    {
        return EnsureComponent<RectTransform>(go);
    }

    private static void SetRect(
        RectTransform rt,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = CenterAnchor;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
    }

    private static void StretchToParent(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = CenterAnchor;
        rt.anchoredPosition = Vector2.zero;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }

    private static T EnsureComponent<T>(GameObject go) where T : Component
    {
        var existing = go.GetComponent<T>();
        return existing != null ? existing : go.AddComponent<T>();
    }

    private static Font ResolveFont()
    {
        if (_cachedFont != null) return _cachedFont;

        _cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_cachedFont == null)
        {
            _cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return _cachedFont;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        if (target == null || string.IsNullOrWhiteSpace(fieldName)) return;
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
        var field = target.GetType().GetField(fieldName, flags);
        if (field == null) return;
        field.SetValue(target, value);
    }

    private static readonly Vector2 CenterAnchor = new(0.5f, 0.5f);
    private static readonly Color SoftWhite = new(0.92f, 0.92f, 0.92f, 1f);
}
