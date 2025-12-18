using UnityEngine;

public class Bootloader : MonoBehaviour {
  void Awake() {
    Game.Data = new GameData();
    Game.Data.LoadAll();

    Game.Save = SaveSystem.LoadOrNew();
    Game.Gacha = new GachaService(Game.Data);

    Debug.Log("Boot complete: data loaded + save loaded.");
  }
}
