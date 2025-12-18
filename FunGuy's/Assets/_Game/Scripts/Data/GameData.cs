using System.Collections.Generic;
using System.Linq;

public class GameData {
  public Dictionary<string, CharacterDef> Characters = new();
  public Dictionary<string, EnemyDef> Enemies = new();
  public Dictionary<string, SkillDef> Skills = new();
  public Dictionary<string, StageDef> Stages = new();
  public Dictionary<string, BannerDef> Banners = new();

  public void LoadAll() {
    var cfile = JsonLoader.LoadFromResources<CharactersFile>("GameData/characters");
    var sfile = JsonLoader.LoadFromResources<SkillsFile>("GameData/skills");
    var stfile = JsonLoader.LoadFromResources<StagesFile>("GameData/stages");
    var bfile = JsonLoader.LoadFromResources<BannersFile>("GameData/banners");

    Characters = cfile.characters.ToDictionary(x => x.id, x => x);
    Enemies    = cfile.enemies.ToDictionary(x => x.id, x => x);
    Skills     = sfile.skills.ToDictionary(x => x.id, x => x);
    Stages     = stfile.stages.ToDictionary(x => x.id, x => x);
    Banners    = bfile.banners.ToDictionary(x => x.id, x => x);
  }
}
