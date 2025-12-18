using System;
using System.Collections.Generic;

[Serializable]
public class OwnedUnit {
  public string charId;
  public int level;
  public int copies; // for future ascension
}

[Serializable]
public class PlayerSave {
  public int gold;
  public int spores;
  public int accountLevel;
  public List<OwnedUnit> units = new();
  public List<string> activeTeam = new(); // list of charIds
  public Dictionary<string, int> bannerPity = new(); // bannerId -> pulls since last 5+
}
