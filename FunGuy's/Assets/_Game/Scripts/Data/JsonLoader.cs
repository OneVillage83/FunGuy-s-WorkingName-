using UnityEngine;

public static class JsonLoader {
  public static T LoadFromResources<T>(string resourcesPathNoExt) {
    var ta = Resources.Load<TextAsset>(resourcesPathNoExt);
    if (ta == null) throw new System.Exception($"Missing TextAsset at Resources/{resourcesPathNoExt}.json");
    return JsonUtility.FromJson<T>(ta.text);
  }
}
