using System;
using System.Collections.Generic;

[Serializable] public class StatBlock {
  public int hp;
  public int atk;
  public int def;
  public int spd;
  public int pot;
}

[Serializable] public class StatGrowth {
  public float hp;
  public float atk;
  public float def;
  public float spd;
  public float pot;
}

[Serializable] public class SkillRefs { public string basic; public string ult; }

[Serializable] public class CharacterDef {
  public string id;
  public string name;
  public int rarity;
  public string rarityTier;
  public string biome;
  public string classArchetype;
  public string role;
  public string element;
  public int bst;
  public List<string> synergyTags;
  public StatBlock baseStats;
  public StatGrowth growth;
  public SkillRefs skills;
}

[Serializable] public class EnemyDef {
  public string id;
  public string name;
  public string biome;
  public string classArchetype;
  public string role;
  public StatBlock baseStats;
  public SkillRefs skills;
}

[Serializable] public class EffectDef {
  public string type;      // Damage, Heal, ApplyStatus, Shield
  public float scale;      // damage/heal scaling vs ATK
  public string status;    // Poison, Burn, Freeze, Regen, Thorns
  public float chance;     // 0..1
  public int duration;     // turns
  public float potency;    // e.g. dot % of maxHP, regen %, thorns % of incoming
}

[Serializable] public class SkillDef {
  public string id;
  public string name;
  public string target;    // EnemyFront, AllEnemies, AllAllies, RandomEnemy2, Self
  public int cooldown;     // turns
  public int energyCost;   // 0 for basic, 100 for signature by default
  public List<EffectDef> effects;
}

[Serializable] public class RewardDef { public int gold; public int spores; public int accountXp; }
[Serializable] public class WaveUnit { public string enemyId; public int level; }
[Serializable] public class StageDef {
  public string id;
  public string name;
  public int recommendedPower;
  public List<List<WaveUnit>> waves; // waves[w] = list of enemy units in that wave
  public RewardDef rewards;
}

[Serializable] public class RateEntry {
  public int rarity;
  public float rate;
}

[Serializable] public class PityDef {
  public int guaranteeRarityAt; // legacy hard pity threshold
  public int guaranteeRarity;   // legacy guaranteed rarity
  public int softPityStart;     // e.g. 74
  public int hardPity;          // e.g. 90
  public int featuredRarity;    // rarity gated by 50/50 logic
}

[Serializable] public class BannerDef {
  public string id;
  public string name;
  public string bannerType; // standard_character, limited_character
  public string currency;
  public int costPerPull;
  public List<RateEntry> rates;
  public PityDef pity;
  public float featuredRateUp; // 0.5 for Genshin-like 50/50
  public bool carryFeaturedGuarantee;
  public List<string> featuredCharacterIds;
}

[Serializable] public class CharactersFile { public List<CharacterDef> characters; public List<EnemyDef> enemies; }
[Serializable] public class SkillsFile { public List<SkillDef> skills; }
[Serializable] public class StagesFile { public List<StageDef> stages; }
[Serializable] public class BannersFile { public List<BannerDef> banners; }
