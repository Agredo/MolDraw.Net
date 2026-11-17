using MolDraw.Depiction;
using SkiaSharp;

namespace MolDraw.Skia.Internal;

/// <summary>Builds atom labels (symbol, implicit H, charge, isotope, radicals) with <see cref="SKFont"/> metrics.</summary>
internal static class LabelLayout
{
    public const float SubscriptScale = 0.68f;

    public static AtomLabel Build(DepictionAtom atom, SKPoint p, SKTypeface typeface, float fontSize, SKColor color)
    {
        using var font = new SKFont(typeface, fontSize) { Subpixel = true, Edging = SKFontEdging.Antialias };
        using var small = new SKFont(typeface, fontSize * SubscriptScale) { Subpixel = true, Edging = SKFontEdging.Antialias };
        var label = new AtomLabel();

        var capHeight = font.Metrics.CapHeight > 0 ? font.Metrics.CapHeight : fontSize * 0.7f;
        var baseline = p.Y + (capHeight / 2);
        var supBaseline = baseline - (capHeight * 0.6f);
        var subBaseline = baseline + (capHeight * 0.35f);

        var symbol = atom.Symbol;
        var symbolWidth = font.MeasureText(symbol);
        var symbolLeft = p.X - (symbolWidth / 2);
        var symbolRight = symbolLeft + symbolWidth;

        Add(label, font, symbol, symbolLeft, baseline, color);

        // isotope directly before the symbol
        var leftEdge = symbolLeft;
        if (atom.MassNumber is { } mass)
        {
            var text = mass.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var w = small.MeasureText(text);
            leftEdge -= w;
            Add(label, small, text, leftEdge, supBaseline, color);
        }

        var rightEdge = symbolRight;
        if (atom.ImplicitHydrogens > 0)
        {
            var hWidth = font.MeasureText("H");
            var count = atom.ImplicitHydrogens > 1 ? atom.ImplicitHydrogens.ToString(System.Globalization.CultureInfo.InvariantCulture) : null;
            var countWidth = count is null ? 0 : small.MeasureText(count);
            switch (atom.HydrogenPosition)
            {
                case HydrogenPosition.Right:
                    Add(label, font, "H", rightEdge, baseline, color);
                    rightEdge += hWidth;
                    if (count is not null)
                    {
                        Add(label, small, count, rightEdge, subBaseline, color);
                        rightEdge += countWidth;
                    }

                    break;

                case HydrogenPosition.Left:
                    var start = leftEdge - hWidth - countWidth;
                    Add(label, font, "H", start, baseline, color);
                    if (count is not null)
                        Add(label, small, count, start + hWidth, subBaseline, color);
                    leftEdge = start;
                    break;

                case HydrogenPosition.Above:
                case HydrogenPosition.Below:
                    var lineHeight = font.Spacing > 0 ? font.Spacing : fontSize * 1.2f;
                    var hBaseline = atom.HydrogenPosition == HydrogenPosition.Above ? baseline - (lineHeight * 0.95f) : baseline + (lineHeight * 0.95f);
                    var hLeft = p.X - ((hWidth + countWidth) / 2);
                    Add(label, font, "H", hLeft, hBaseline, color);
                    if (count is not null)
                        Add(label, small, count, hLeft + hWidth, hBaseline + (capHeight * 0.35f), color);
                    break;
            }
        }

        if (atom.Charge != 0)
        {
            var text = ChargeText(atom.Charge, small);
            Add(label, small, text, rightEdge, supBaseline, color);
        }

        if (atom.Radicals > 0)
        {
            label.DotRadius = Math.Max(0.8f, fontSize * 0.07f);
            var y = baseline - capHeight - (fontSize * 0.25f);
            var spacing = label.DotRadius * 3;
            var x0 = p.X - ((atom.Radicals - 1) * spacing / 2);
            for (var i = 0; i < atom.Radicals; i++)
            {
                label.Dots.Add(new SKPoint(x0 + (i * spacing), y));
                label.Rects.Add(new SKRect(x0 + (i * spacing) - label.DotRadius, y - label.DotRadius, x0 + (i * spacing) + label.DotRadius, y + label.DotRadius));
            }
        }

        // padding so that bonds do not touch the glyphs
        var pad = fontSize * 0.12f;
        for (var i = 0; i < label.Rects.Count; i++)
        {
            var r = label.Rects[i];
            r.Inflate(pad, pad);
            label.Rects[i] = r;
        }

        return label;
    }

    public static string ChargeText(int charge, SKFont font)
    {
        var minus = font.GetGlyph(0x2212) != 0 ? "\u2212" : "-";
        var sign = charge > 0 ? "+" : minus;
        var magnitude = Math.Abs(charge);
        return magnitude == 1 ? sign : magnitude.ToString(System.Globalization.CultureInfo.InvariantCulture) + sign;
    }

    private static void Add(AtomLabel label, SKFont font, string text, float x, float baseline, SKColor color)
    {
        label.Runs.Add(new TextRun(text, font.Size, x, baseline, color));
        var width = font.MeasureText(text, out var glyphBounds);

        // use the full advance width horizontally and the glyph extent vertically (at least cap height)
        var capHeight = font.Metrics.CapHeight > 0 ? font.Metrics.CapHeight : font.Size * 0.7f;
        var top = Math.Min(glyphBounds.Top, -capHeight);
        var bottom = Math.Max(glyphBounds.Bottom, 0);
        label.Rects.Add(new SKRect(x, baseline + top, x + Math.Max(width, glyphBounds.Right), baseline + bottom));
    }
}
