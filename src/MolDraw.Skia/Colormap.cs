using SkiaSharp;

namespace MolDraw.Skia;

/// <summary>
/// Immutable colour map that maps values in [0, 1] to colours by linear interpolation between evenly spaced stops.
/// Built-in maps (Viridis, Plasma, Inferno, Magma: CC0, Matplotlib; Turbo: Apache-2.0, Google) are sampled at 17 stops.
/// </summary>
public sealed class Colormap
{
    private readonly SKColor[] _stops;

    private Colormap(string name, SKColor[] stops)
    {
        Name = name;
        _stops = stops;
    }

    /// <summary>Perceptually uniform blue-green-yellow map (default).</summary>
    public static Colormap Viridis { get; } = FromHex("Viridis", 0x440154, 0x48186A, 0x472D7B, 0x424086, 0x3B528B, 0x33638D, 0x2C728E, 0x26828E, 0x21918C, 0x1FA088, 0x28AE80, 0x3FBC73, 0x5EC962, 0x84D44B, 0xADDC30, 0xD8E219, 0xFDE725);

    /// <summary>Perceptually uniform blue-red-yellow map.</summary>
    public static Colormap Plasma { get; } = FromHex("Plasma", 0x0D0887, 0x310597, 0x4C02A1, 0x6600A7, 0x7E03A8, 0x9511A1, 0xAA2395, 0xBC3587, 0xCC4778, 0xDA5A6A, 0xE66C5C, 0xF0804E, 0xF89540, 0xFDAC33, 0xFDC527, 0xF8DF25, 0xF0F921);

    /// <summary>Perceptually uniform black-red-yellow map.</summary>
    public static Colormap Inferno { get; } = FromHex("Inferno", 0x000004, 0x0B0724, 0x210C4A, 0x3D0965, 0x57106E, 0x71196E, 0x8A226A, 0xA32C61, 0xBC3754, 0xD24644, 0xE45A31, 0xF1731D, 0xF98E09, 0xFCAC11, 0xF9CB35, 0xF2EA69, 0xFCFFA4);

    /// <summary>Perceptually uniform black-purple-cream map.</summary>
    public static Colormap Magma { get; } = FromHex("Magma", 0x000004, 0x0A0822, 0x1D1147, 0x36106B, 0x51127C, 0x6A1C81, 0x832681, 0x9C2E7F, 0xB73779, 0xD0416F, 0xE75263, 0xF56B5C, 0xFC8961, 0xFEA772, 0xFEC488, 0xFDE2A3, 0xFCFDBF);

    /// <summary>Rainbow-like map with improved perceptual properties.</summary>
    public static Colormap Turbo { get; } = FromHex("Turbo", 0x30123B, 0x4040A2, 0x466BE3, 0x4294FF, 0x28BCEB, 0x18DDC2, 0x32F298, 0x6DFE62, 0xA4FC3C, 0xCDEC34, 0xEECF3A, 0xFDAC34, 0xFB7E21, 0xEB500E, 0xD02F05, 0xA91601, 0x7A0403);

    /// <summary>All built-in colour maps.</summary>
    public static IReadOnlyList<Colormap> BuiltIn { get; } = [Viridis, Plasma, Inferno, Magma, Turbo];

    /// <summary>Display name.</summary>
    public string Name { get; }

    /// <summary>Colour stops (evenly spaced from 0 to 1).</summary>
    public IReadOnlyList<SKColor> Stops => _stops;

    /// <summary>Creates a custom colour map from at least two evenly spaced stops.</summary>
    /// <param name="name">Display name.</param>
    /// <param name="stops">Colours for 0 … 1.</param>
    /// <exception cref="ArgumentException">Fewer than two stops.</exception>
    public static Colormap Custom(string name, params SKColor[] stops)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(stops);
        if (stops.Length < 2)
            throw new ArgumentException("A colour map needs at least two stops.", nameof(stops));
        return new Colormap(name, (SKColor[])stops.Clone());
    }

    /// <summary>Returns a colour map with reversed direction.</summary>
    public Colormap Reversed() => new(Name + " (reversed)", _stops.Reverse().ToArray());

    /// <summary>Maps a value to a colour. Values are clamped to [0, 1]; NaN maps to the first stop.</summary>
    /// <param name="value">Value in [0, 1].</param>
    public SKColor Map(float value)
    {
        if (float.IsNaN(value) || value <= 0)
            return _stops[0];
        if (value >= 1)
            return _stops[^1];
        var pos = value * (_stops.Length - 1);
        var i = (int)pos;
        var f = pos - i;
        var a = _stops[i];
        var b = _stops[i + 1];
        return new SKColor(
            (byte)Math.Round(a.Red + ((b.Red - a.Red) * f)),
            (byte)Math.Round(a.Green + ((b.Green - a.Green) * f)),
            (byte)Math.Round(a.Blue + ((b.Blue - a.Blue) * f)),
            (byte)Math.Round(a.Alpha + ((b.Alpha - a.Alpha) * f)));
    }

    /// <inheritdoc />
    public override string ToString() => Name;

    private static Colormap FromHex(string name, params uint[] rgb)
        => new(name, rgb.Select(c => new SKColor((byte)(c >> 16), (byte)(c >> 8), (byte)c)).ToArray());
}
