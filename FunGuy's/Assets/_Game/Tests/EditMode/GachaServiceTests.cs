using System.Collections.Generic;
using NUnit.Framework;

public class GachaServiceTests
{
    [Test]
    public void PullSequence_IsDeterministic_WithSameSeed()
    {
        var data = BuildDataForDeterminism();
        var saveA = NewSave();
        var saveB = NewSave();

        var gachaA = new GachaService(data, seed: 4242);
        var gachaB = new GachaService(data, seed: 4242);

        var seqA = new List<string>();
        var seqB = new List<string>();

        for (int i = 0; i < 30; i++)
        {
            seqA.Add(gachaA.PullOne(saveA, "b_test", consumeCurrency: false));
            seqB.Add(gachaB.PullOne(saveB, "b_test", consumeCurrency: false));
        }

        CollectionAssert.AreEqual(seqA, seqB);
        Assert.AreEqual(
            SaveMapUtils.GetInt(saveA.bannerPity, "b_test", 0),
            SaveMapUtils.GetInt(saveB.bannerPity, "b_test", 0));
    }

    [Test]
    public void LimitedBanner_UsesFeaturedGuaranteeCarry_AfterOffBannerLoss()
    {
        var data = new GameData();
        data.Characters = new Dictionary<string, CharacterDef>
        {
            ["featured"] = Character("featured", rarity: 6),
            ["off"] = Character("off", rarity: 6),
        };
        data.Banners = new Dictionary<string, BannerDef>
        {
            ["b_limited"] = new BannerDef
            {
                id = "b_limited",
                bannerType = "limited_character",
                currency = "spores",
                costPerPull = 1,
                rates = new List<RateEntry> { new() { rarity = 6, rate = 1f } },
                featuredCharacterIds = new List<string> { "featured" },
                featuredRateUp = 0f, // force off-banner on first pull
                carryFeaturedGuarantee = true,
                pity = new PityDef
                {
                    guaranteeRarity = 6,
                    guaranteeRarityAt = 90,
                    hardPity = 90,
                    softPityStart = 74,
                    featuredRarity = 6
                }
            }
        };

        var save = NewSave();
        var gacha = new GachaService(data, seed: 7);

        string first = gacha.PullOne(save, "b_limited", consumeCurrency: false);
        Assert.AreEqual("off", first);
        Assert.IsTrue(SaveMapUtils.GetBool(save.bannerFeaturedGuarantee, "b_limited:featured", false));

        string second = gacha.PullOne(save, "b_limited", consumeCurrency: false);
        Assert.AreEqual("featured", second);
        Assert.IsFalse(SaveMapUtils.GetBool(save.bannerFeaturedGuarantee, "b_limited:featured", true));
    }

    private static GameData BuildDataForDeterminism()
    {
        var data = new GameData();
        data.Characters = new Dictionary<string, CharacterDef>
        {
            ["c3"] = Character("c3", rarity: 3),
            ["c4"] = Character("c4", rarity: 4),
            ["c5"] = Character("c5", rarity: 5),
        };
        data.Banners = new Dictionary<string, BannerDef>
        {
            ["b_test"] = new BannerDef
            {
                id = "b_test",
                bannerType = "standard_character",
                currency = "spores",
                costPerPull = 1,
                rates = new List<RateEntry>
                {
                    new() { rarity = 3, rate = 0.8f },
                    new() { rarity = 4, rate = 0.18f },
                    new() { rarity = 5, rate = 0.02f },
                },
                pity = new PityDef
                {
                    guaranteeRarity = 5,
                    guaranteeRarityAt = 90,
                    hardPity = 90,
                    softPityStart = 74,
                    featuredRarity = 5
                },
                featuredCharacterIds = new List<string>(),
                featuredRateUp = 1f
            }
        };

        return data;
    }

    private static CharacterDef Character(string id, int rarity)
    {
        return new CharacterDef
        {
            id = id,
            name = id,
            rarity = rarity,
            rarityTier = rarity >= 6 ? "UR" : "SR",
            biome = "Forest",
            classArchetype = "DPS",
            role = "DPS",
            baseStats = new StatBlock { hp = 100, atk = 20, def = 10, spd = 100, pot = 20 },
            growth = new StatGrowth { hp = 10, atk = 2, def = 1, spd = 1, pot = 2 },
            skills = new SkillRefs { basic = "atk_basic", ult = "atk_basic" },
        };
    }

    private static PlayerSave NewSave()
    {
        return new PlayerSave
        {
            version = 2,
            gold = 1000,
            spores = 1000,
            accountLevel = 1,
            units = new List<OwnedUnit>(),
            activeTeam = new List<string>(),
            bannerPity = new List<StringIntEntry>(),
            bannerFeaturedGuarantee = new List<StringBoolEntry>(),
            bannerHistory = new List<BannerHistoryEntry>(),
            tutorialCompleted = true,
            tutorialStep = 0
        };
    }
}
