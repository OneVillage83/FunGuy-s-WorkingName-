# Idle Huntress-Style UI Wiring Guide

This project now has code-ready UI controllers and skinning scripts for a portrait-card anime style.

## 1) Home Scene

### Root setup
- `Canvas/HomeRoot`
- `HomeRoot` add:
  - `UiPrefabBlueprintBinder` (optional auto-bind of common fields by object name)
  - `IdleHuntressSkin` (`tone = Home`)
  - `StaggeredEntranceAnimator` (optional for menu card entrances)
  - `HomeMenuController`

### `HomeMenuController` bindings
- `welcomeLabel` -> top hero line text
- `accountStatsLabel` -> currency/account strip text
- Button hooks:
  - Start -> `OnStartPressed`
  - Summon -> `OnSummonPressed`
  - Team -> `OnTeamPressed`
  - Battle -> `OnBattlePressed`
  - Options -> `OnOptionsPressed`

## 2) Summon Scene

### Root setup
- `Canvas/SummonRoot`
- `SummonRoot` add:
  - `UiPrefabBlueprintBinder` (optional)
  - `IdleHuntressSkin` (`tone = Summon`)
  - `SummonMenuController`

### `SummonMenuController` bindings
- `resultLabel` -> text area below summon buttons
- `sporesLabel` -> currency strip
- `bannerLabel` -> banner title text
- `pullOneButton` -> pull x1 button
- `pullTenButton` -> pull x10 button
- `revealController` -> `SummonRevealController` component on reveal panel

### Reveal panel setup
- `Canvas/SummonRevealPanel` with:
  - `SummonRevealController`
  - `CanvasGroup` (optional)
- `SummonRevealController` bindings:
  - `root` -> reveal panel root game object
  - `titleLabel` -> "New Funguy Acquired"
  - `nameLabel` -> unit name
  - `rarityLabel` -> star row
  - `rarityFrame` -> card frame image
  - `rarityGlow` -> glow/vfx image
  - `nextButton` -> continue/reveal-next button

## 3) Team Scene

### Root setup
- `Canvas/TeamRoot`
- `TeamRoot` add:
  - `UiPrefabBlueprintBinder` (optional)
  - `IdleHuntressSkin` (`tone = Team`)
  - `TeamMenuController`
  - `TeamRosterSlotBinder` (optional quick-select button grid)

### `TeamMenuController` bindings
- `teamStatusLabel` -> active team summary
- `rosterLabel` -> multiline owned roster list
- `hintLabel` -> tactical hint line
- Button hooks:
  - Auto Fill -> `AutoFillTeam`
  - Clear Team -> `OnClearTeamPressed`
  - Start Battle -> `OnStartBattlePressed`
  - Back -> `OnBackPressed`

### Optional unit cards
- Each card button can call:
  - add: `AddUnitToTeam(string charId)`
  - remove: `RemoveUnitFromTeam(string charId)`

### Optional quick-select roster grid
- Add `TeamRosterSlotBinder` on Team scene.
- Assign:
  - `teamController`
  - `slotButtons` (top N owned units)
  - `slotLabels` (matching text labels)
- Calling `RebindSlots()` refreshes labels and click actions automatically.

## 4) Battle Scene

### Root setup
- `Canvas/BattleRoot`
- `BattleRoot` add:
  - `UiPrefabBlueprintBinder` (optional)
  - `IdleHuntressSkin` (`tone = Battle`)
  - `BattleSceneController`

### `BattleSceneController` bindings
- `battleResultLabel` -> result line
- `stageInfoLabel` -> stage name/rewards
- `teamPreviewLabel` -> selected team summary
- Button hooks:
  - Run Battle -> `OnRunBattlePressed`
  - Retry -> `OnRetryPressed`
  - Next Stage -> `OnNextStagePressed`
  - Team -> `OnGoTeamPressed`
  - Back -> `OnBackPressed`

## 4.1) Debug / QA Scene (Recommended)

Create a lightweight debug panel scene or embed in `Options`.

- Add `DebugProgressionController` for save/resource utilities:
  - Reset Save -> `OnResetSavePressed`
  - Grant Starter Resources -> `OnGrantStarterResourcesPressed`
  - Seed Starter Roster -> `OnSeedStarterRosterPressed`
  - Skip Tutorial -> `OnSkipTutorialPressed`
  - Open Summon -> `OnOpenSummonPressed`
  - Back Home -> `OnBackHomePressed`
- Add `GameplaySmokeTestController` for one-click smoke checks:
  - Run Smoke Tests -> `OnRunSmokeTestsPressed`
  - `outputLabel` -> multiline results text

## 5) Notes
- `IdleHuntressSkin` controls panel/button/text colors and accent pulse.
- `StaggeredEntranceAnimator` can be attached to scene root cards for soft panel reveal.
- `TutorialSpotlightController` can pulse-highlight menu buttons by tutorial step.
- `UiPrefabBlueprintBinder` auto-finds and assigns common serialized references by name/path.
- Tutorial progression is already wired from:
  - summon completion (`OnFirstSummonCompleted`)
  - battle completion (`OnFirstBattleCompleted`)

## 5.1) Blueprint Binder Naming

`UiPrefabBlueprintBinder` looks for common aliases. Prefer these names for fastest setup:

- Home:
  - `Lbl_Welcome`
  - `Lbl_AccountStats`
- Summon:
  - `Lbl_Result`
  - `Lbl_Spores`
  - `Lbl_Banner`
  - `Btn_PullOne`
  - `Btn_PullTen`
  - `SummonReveal` (with `SummonRevealController`)
- Team:
  - `Lbl_TeamStatus`
  - `Lbl_Roster`
  - `Lbl_Hint`
- Battle:
  - `Lbl_BattleResult`
  - `Lbl_StageInfo`
  - `Lbl_TeamPreview`
- Tutorial overlay:
  - `TutorialOverlayRoot`
  - `Lbl_TutorialMessage`
  - `Btn_TutorialContinue`

### Auto-button onClick wiring

`UiPrefabBlueprintBinder` now also supports runtime button event wiring by convention.
Enable `autoWireButtons` on the binder.

Default button-name mappings:
- `Btn_Start` -> `OnStartPressed`
- `Btn_Options` -> `OnOptionsPressed`
- `Btn_Summon` -> `OnSummonPressed`
- `Btn_Team` -> `OnTeamPressed`
- `Btn_Battle` -> `OnBattlePressed`
- `Btn_PullOne` -> `OnPullOnePressed`
- `Btn_PullTen` -> `OnPullTenPressed`
- `Btn_AutoFill` -> `AutoFillTeam`
- `Btn_ClearTeam` -> `OnClearTeamPressed`
- `Btn_StartBattle` -> `OnStartBattlePressed`
- `Btn_RunBattle` -> `OnRunBattlePressed`
- `Btn_Retry` -> `OnRetryPressed`
- `Btn_NextStage` -> `OnNextStagePressed`
- `Btn_GoTeam` -> `OnGoTeamPressed`
- `Btn_Back` -> `OnBackPressed`
- `Btn_TutorialContinue` -> `ContinueTutorial`
- `Btn_ResetSave` -> `OnResetSavePressed`
- `Btn_GrantStarterResources` -> `OnGrantStarterResourcesPressed`
- `Btn_SeedStarterRoster` -> `OnSeedStarterRosterPressed`
- `Btn_SkipTutorial` -> `OnSkipTutorialPressed`
- `Btn_RunSmokeTests` -> `OnRunSmokeTestsPressed`

You can add extra `customButtonRules` entries to map any button name to a method and optional target type.

## 6) Tutorial Spotlight Setup

Add `TutorialSpotlightController` to each scene root that should highlight UI.

### Suggested mapping
- Home scene:
  - `ShowSummonPool` -> Summon button
- Team scene:
  - `GoToTeamBuilder` and `PlaceFirstUnit` -> team slot area or autofill button
  - `StartFirstBattle` -> Start Battle button
- Battle scene:
  - `StartFirstBattle` -> Run Battle button

### Binding
- `targets`: add entries with `step`, `target`, and optional `hint` text
- `hintLabel`: optional helper label below top bar for tutorial instruction text
