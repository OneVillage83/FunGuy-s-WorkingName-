using System;
using System.Collections.Generic;
using System.Linq;

public class GachaService {
  private readonly GameData _data;
  private readonly System.Random _rng = new();

  public GachaService(GameData data) { _data = data; }

  public string PullOne(PlayerSave save, string bannerId) {
    var banner = _data.Banners[bannerId];
    if (banner.currency == "spores" && save.spores < banner.costPerPull) throw new Exception("Not enough spores.");
    if (banner.currency == "spores") save.spores -= banner.costPerPull;

    if (!save.bannerPity.ContainsKey(bannerId)) save.bannerPity[bannerId] = 0;
    save.bannerPity[bannerId]++;

    int rolledRarity = RollRarity(banner.rates);

    // pity: force guarantee rarity at threshold
    if (save.bannerPity[bannerId] >= banner.pity.guaranteeRarityAt) {
      rolledRarity = Math.Max(rolledRarity, banner.pity.guaranteeRarity);
      save.bannerPity[bannerId] = 0;
    }

    var pool = _data.Characters.Values.Where(c => c.rarity == rolledRarity).ToList();
    var chosen = pool[_rng.Next(pool.Count)].id;

    AddUnitToInventory(save, chosen);
    return chosen;
  }

  private int RollRarity(Dictionary<string, float> rates) {
    float r = (float)_rng.NextDouble();
    float cumulative = 0f;

    // order from rarest to commonest is fine, but must sum to 1.0
    foreach (var kv in rates.OrderByDescending(k => int.Parse(k.Key))) {
      cumulative += kv.Value;
      if (r <= cumulative) return int.Parse(kv.Key);
    }
    return 3; // fallback
  }

  private void AddUnitToInventory(PlayerSave save, string charId) {
    var owned = save.units.FirstOrDefault(u => u.charId == charId);
    if (owned == null) save.units.Add(new OwnedUnit { charId = charId, level = 1, copies = 1 });
    else owned.copies += 1;
  }
}
