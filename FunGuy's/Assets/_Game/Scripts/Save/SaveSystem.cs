using UnityEngine;

public static class SaveSystem {
  private const string Key = "FUNGI_SAVE_V0";

  public static PlayerSave LoadOrNew() {
    if (!PlayerPrefs.HasKey(Key)) return NewSave();
    var json = PlayerPrefs.GetString(Key);
    return JsonUtility.FromJson<PlayerSave>(json) ?? NewSave();
  }

  public static void Save(PlayerSave save) {
    var json = JsonUtility.ToJson(save);
    PlayerPrefs.SetString(Key, json);
    PlayerPrefs.Save();
  }

  private static PlayerSave NewSave() {
    return new PlayerSave {
      gold = 100,
      spores = 50,
      accountLevel = 1,
      units = new System.Collections.Generic.List<OwnedUnit>(),
      activeTeam = new System.Collections.Generic.List<string>()
    };
  }
}
