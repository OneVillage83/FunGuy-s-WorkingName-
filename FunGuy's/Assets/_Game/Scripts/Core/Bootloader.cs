using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootloader : MonoBehaviour {
  [SerializeField] private string homeSceneName = "Home";
  [SerializeField] private bool autoRouteToHome = true;

  void Awake() {
    Game.Data = new GameData();
    Game.Data.LoadAll();

    Game.Save = SaveSystem.LoadOrNew();
    Game.Gacha = new GachaService(Game.Data);

    Debug.Log("Boot complete: data loaded + save loaded.");

    if (autoRouteToHome) {
      // Boot only initializes global state; Home owns first-play routing.
      SceneManager.LoadScene(homeSceneName);
    }
  }
}
