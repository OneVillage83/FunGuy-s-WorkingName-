# Testing Guide

## Automated Tests (EditMode)

Location:
- `Assets/_Game/Tests/EditMode`

Assembly:
- `Game.EditModeTests.asmdef`

Current coverage:
- `GachaServiceTests`
  - deterministic pull sequence with seed
  - featured guarantee carry behavior on limited banner
- `BattleSimTests`
  - deterministic outcome with seed
  - biome advantage damage multiplier sanity check
- `DataAndSaveTests`
  - stage wave enemy-reference integrity
  - save-model JSON roundtrip for map-like lists

Run in Unity:
1. Open **Window -> General -> Test Runner**.
2. Select **EditMode**.
3. Run all tests or filter by class.

## Runtime Smoke Tests

Script:
- `Assets/_Game/Scripts/UI/GameplaySmokeTestController.cs`

Recommended setup:
- Add to Debug panel scene (or `Options` scene).
- Bind a button to `OnRunSmokeTestsPressed`.
- Bind `outputLabel` to a multiline text UI element.

Smoke suite checks:
- Data load validity
- Stage wave data sanity
- Deterministic gacha sequence (seeded)
- Deterministic battle result (seeded)
- Save-model serialization roundtrip

## Debug Utilities

Script:
- `Assets/_Game/Scripts/UI/DebugProgressionController.cs`

Useful actions:
- Reset save
- Grant starter resources
- Seed starter roster
- Skip tutorial
