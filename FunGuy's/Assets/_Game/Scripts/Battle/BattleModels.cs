using System.Collections.Generic;

public enum TeamSide { Player, Enemy }

public class CombatUnit {
  public TeamSide side;
  public string id; // charId or enemyId
  public string name;
  public int level;

  public int maxHp;
  public int hp;
  public int atk;
  public int def;
  public int spd;

  public string basicSkillId;
  public string ultSkillId;

  public int ultCdRemaining;

  public List<StatusInstance> statuses = new();
  public int shield; // simple flat shield for MVP
}

public class StatusInstance {
  public string status; // Poison, Burn, Freeze, Regen, Thorns
  public int remainingTurns;
  public float potency;
}
