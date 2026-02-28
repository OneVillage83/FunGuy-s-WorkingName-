using System;
using System.Collections.Generic;
using UnityEngine;

public static class SaveSystem
{
    // Bump this when you change PlayerSave schema in a breaking way
    private const int CurrentVersion = 2;

    // Primary + backup keys
    private const string Key = "FUNGI_SAVE_V2";
    private const string BackupKey = "FUNGI_SAVE_V2_BAK";
    private static readonly string[] LegacyKeys = { "FUNGI_SAVE_V1", "FUNGI_SAVE_V1_BAK" };

    public static bool HasSave()
    {
        if (PlayerPrefs.HasKey(Key) && !string.IsNullOrEmpty(PlayerPrefs.GetString(Key)))
            return true;

        foreach (var legacyKey in LegacyKeys)
        {
            if (PlayerPrefs.HasKey(legacyKey) && !string.IsNullOrEmpty(PlayerPrefs.GetString(legacyKey)))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to load without creating a new save.
    /// Returns true if a valid save was loaded (or recovered from backup).
    /// </summary>
    public static bool TryLoad(out PlayerSave save)
    {
        save = null;

        // Try primary
        if (TryLoadFromKey(Key, out save))
            return true;

        // Try backup recovery
        if (TryLoadFromKey(BackupKey, out save))
        {
            Debug.LogWarning("[SaveSystem] Primary save invalid; recovered from backup.");
            // Re-save to primary so next load is clean
            Save(save);
            return true;
        }

        // Try legacy saves and migrate to v2 keys
        foreach (var legacyKey in LegacyKeys)
        {
            if (TryLoadFromKey(legacyKey, out save))
            {
                Debug.LogWarning($"[SaveSystem] Loaded legacy save from {legacyKey}; migrated to v2.");
                Save(save);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Loads if possible; otherwise creates a new save (and persists it).
    /// </summary>
    public static PlayerSave LoadOrNew()
    {
        if (TryLoad(out var loaded))
            return loaded;

        var created = NewSave();
        Save(created);
        return created;
    }

    public static void Save(PlayerSave save)
    {
        if (save == null) return;

        EnsureLists(save);

        save.version = CurrentVersion;
        save.lastSavedUtc = DateTime.UtcNow.ToString("o");

        // Serialize once
        var json = JsonUtility.ToJson(save);

        // Write backup first (so we always have last-known-good)
        PlayerPrefs.SetString(BackupKey, json);

        // Then write primary
        PlayerPrefs.SetString(Key, json);
        PlayerPrefs.Save();
    }

    public static void DeleteSave()
    {
        if (PlayerPrefs.HasKey(Key)) PlayerPrefs.DeleteKey(Key);
        if (PlayerPrefs.HasKey(BackupKey)) PlayerPrefs.DeleteKey(BackupKey);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Creates and persists a fresh save (overwriting existing).
    /// </summary>
    public static PlayerSave ResetToNewSave()
    {
        var save = NewSave();
        Save(save);
        return save;
    }

    /// <summary>
    /// Call this when tutorial is completed.
    /// </summary>
    public static void MarkTutorialComplete()
    {
        var save = LoadOrNew();
        save.tutorialCompleted = true;
        Save(save);
    }

    /// <summary>
    /// Debug helper: grants currencies and persists.
    /// </summary>
    public static PlayerSave GrantDebugResources(int gold = 0, int spores = 0, int sporeEssence = 0, int coreFragments = 0, int primeSpores = 0)
    {
        var save = LoadOrNew();
        save.gold = Math.Max(0, save.gold + Math.Max(0, gold));
        save.spores = Math.Max(0, save.spores + Math.Max(0, spores));
        save.sporeEssence = Math.Max(0, save.sporeEssence + Math.Max(0, sporeEssence));
        save.coreFragments = Math.Max(0, save.coreFragments + Math.Max(0, coreFragments));
        save.primeSpores = Math.Max(0, save.primeSpores + Math.Max(0, primeSpores));
        Save(save);
        return save;
    }

    /// <summary>
    /// Debug helper: sets tutorial step and completion state explicitly.
    /// </summary>
    public static PlayerSave SetTutorialStateForDebug(int step, bool completed)
    {
        var save = LoadOrNew();
        save.tutorialStep = Math.Max(0, step);
        save.tutorialCompleted = completed;
        Save(save);
        return save;
    }

    // ------------------------
    // Internal helpers
    // ------------------------

    private static bool TryLoadFromKey(string key, out PlayerSave save)
    {
        save = null;

        if (!PlayerPrefs.HasKey(key))
            return false;

        var json = PlayerPrefs.GetString(key);
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            save = JsonUtility.FromJson<PlayerSave>(json);
            if (save == null)
                return false;

            EnsureLists(save);

            // Normalize missing version
            if (save.version < 0) save.version = 0;

            // Migrate if needed
            if (save.version < CurrentVersion)
            {
                save = Migrate(save);
                // Don't automatically Save() here to avoid double writes;
                // caller can Save() after success, but it's fine either way.
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveSystem] Failed to parse save from {key}. Error: {e.Message}");
            return false;
        }
    }

    private static PlayerSave NewSave()
    {
        var now = DateTime.UtcNow.ToString("o");

        return new PlayerSave
        {
            version = CurrentVersion,
            createdUtc = now,
            lastSavedUtc = now,

            gold = 100,
            spores = 50,
            accountLevel = 1,
            sporeEssence = 0,
            coreFragments = 0,
            primeSpores = 0,

            tutorialCompleted = false,
            tutorialStep = 0,
            tutorialTickets = 1,

            units = new List<OwnedUnit>(),
            activeTeam = new List<string>(),
            bannerPity = new List<StringIntEntry>(),
            bannerFeaturedGuarantee = new List<StringBoolEntry>(),
            bannerHistory = new List<BannerHistoryEntry>(),
        };
    }

    private static void EnsureLists(PlayerSave save)
    {
        if (string.IsNullOrWhiteSpace(save.createdUtc)) save.createdUtc = DateTime.UtcNow.ToString("o");
        if (save.accountLevel <= 0) save.accountLevel = 1;
        if (save.gold < 0) save.gold = 0;
        if (save.spores < 0) save.spores = 0;
        if (save.tutorialTickets < 0) save.tutorialTickets = 0;

        if (save.units == null) save.units = new List<OwnedUnit>();
        if (save.activeTeam == null) save.activeTeam = new List<string>();
        if (save.bannerPity == null) save.bannerPity = new List<StringIntEntry>();
        if (save.bannerFeaturedGuarantee == null) save.bannerFeaturedGuarantee = new List<StringBoolEntry>();
        if (save.bannerHistory == null) save.bannerHistory = new List<BannerHistoryEntry>();

        foreach (var unit in save.units)
        {
            if (unit == null) continue;
            if (unit.level <= 0) unit.level = 1;
            if (unit.stars <= 0) unit.stars = 1;
            if (unit.coreLevel <= 0) unit.coreLevel = 1;
            if (unit.gearSlots == null) unit.gearSlots = new List<GearSlotState>();
        }
    }

    private static PlayerSave Migrate(PlayerSave save)
    {
        if (save.version < 1)
        {
            save.tutorialCompleted = false;
            save.tutorialStep = 0;
            save.version = 1;
        }

        if (save.version < 2)
        {
            save.sporeEssence = Math.Max(0, save.sporeEssence);
            save.coreFragments = Math.Max(0, save.coreFragments);
            save.primeSpores = Math.Max(0, save.primeSpores);
            if (save.bannerPity == null) save.bannerPity = new List<StringIntEntry>();
            if (save.bannerFeaturedGuarantee == null) save.bannerFeaturedGuarantee = new List<StringBoolEntry>();
            if (save.bannerHistory == null) save.bannerHistory = new List<BannerHistoryEntry>();
            save.version = 2;
        }

        // After migrations, ensure lists again
        EnsureLists(save);

        return save;
    }
}
