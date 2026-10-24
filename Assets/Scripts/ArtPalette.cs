using UnityEngine;

// The game's steampunk palette. Builders and scripts pull colors from here so everything matches.
// To restyle the game, change these and re-run Tools > Up Up Away > Build Art Style.
public static class ArtPalette
{
    // Darks
    public static readonly Color Ink = Hex(0x1E1A1D);       // outlines, deepest shadow
    public static readonly Color IronDark = Hex(0x2B2729);
    public static readonly Color Iron = Hex(0x48413F);
    public static readonly Color Steel = Hex(0x7D7470);

    // Woods
    public static readonly Color WoodDark = Hex(0x4A2C1A);
    public static readonly Color Wood = Hex(0x7E4B29);
    public static readonly Color WoodLight = Hex(0xB57D48);

    // Metals
    public static readonly Color BrassDark = Hex(0x7A5520);
    public static readonly Color Brass = Hex(0xC4903A);
    public static readonly Color BrassLight = Hex(0xEDCB74);
    public static readonly Color Copper = Hex(0xB5562F);
    public static readonly Color Verdigris = Hex(0x4F9C8B);  // aged-copper green

    // Lights
    public static readonly Color Parchment = Hex(0xE6D3AA);
    public static readonly Color Cream = Hex(0xF6EDD6);
    public static readonly Color Canvas = Hex(0xE2CFA5);     // balloon envelope

    // Accents
    public static readonly Color Signal = Hex(0xB8392C);     // danger / stripes
    public static readonly Color Moss = Hex(0x6C9A3C);       // safe / buy
    public static readonly Color Flame = Hex(0xF08A24);
    public static readonly Color Ember = Hex(0xFFD45C);
    public static readonly Color Sky = Hex(0x8FC6E0);

    public static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private static Color Hex(int rgb)
    {
        return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
    }
}
