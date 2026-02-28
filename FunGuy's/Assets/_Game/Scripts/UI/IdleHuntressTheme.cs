using UnityEngine;

public enum UiTone
{
    Home,
    Summon,
    Team,
    Battle
}

public static class IdleHuntressTheme
{
    public static Color BackgroundFor(UiTone tone)
    {
        return tone switch
        {
            UiTone.Home => Hex("1D2334"),
            UiTone.Summon => Hex("261E38"),
            UiTone.Team => Hex("1E2A2D"),
            UiTone.Battle => Hex("2D1F1F"),
            _ => Hex("1D2334"),
        };
    }

    public static Color PanelFor(UiTone tone)
    {
        return tone switch
        {
            UiTone.Home => Hex("2F3C57"),
            UiTone.Summon => Hex("3D2C58"),
            UiTone.Team => Hex("2F4851"),
            UiTone.Battle => Hex("4A3131"),
            _ => Hex("2F3C57"),
        };
    }

    public static Color AccentFor(UiTone tone)
    {
        return tone switch
        {
            UiTone.Home => Hex("F2C66D"),
            UiTone.Summon => Hex("F1A9FF"),
            UiTone.Team => Hex("82E1D7"),
            UiTone.Battle => Hex("FF8E7A"),
            _ => Hex("F2C66D"),
        };
    }

    public static Color RarityColor(int rarity)
    {
        return rarity switch
        {
            6 => Hex("F7C948"), // UR / legendary gold
            5 => Hex("FF8B5D"), // mythic orange
            4 => Hex("B58CFF"), // epic violet
            3 => Hex("76C9FF"), // rare blue
            _ => Hex("E0E0E0"), // common gray
        };
    }

    public static string Stars(int rarity)
    {
        int clamped = Mathf.Clamp(rarity, 1, 6);
        return new string('★', clamped);
    }

    private static Color Hex(string hex)
    {
        if (ColorUtility.TryParseHtmlString($"#{hex}", out var parsed))
            return parsed;
        return Color.white;
    }
}
