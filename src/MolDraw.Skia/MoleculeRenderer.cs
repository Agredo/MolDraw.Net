using System.Collections.Concurrent;
using MolDraw.Depiction;
using MolDraw.Skia.Internal;
using SkiaSharp;

namespace MolDraw.Skia;

/// <summary>
/// Default SkiaSharp renderer. Thread-safe; the only internal state is a cache of resolved typefaces by family name.
/// </summary>
public sealed class MoleculeRenderer : IMoleculeRenderer
{
    private const double DefaultModelBondLength = 1.5;
    private readonly ConcurrentDictionary<string, SKTypeface> _typefaces = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public RenderedMolecule Layout(SKRect bounds, MoleculeDepiction depiction, MoleculeRenderOptions options, MoleculeHighlights? highlights = null)
    {
        ArgumentNullException.ThrowIfNull(depiction);
        ArgumentNullException.ThrowIfNull(options);
        return ComputeFrame(bounds, depiction, options, highlights).Result;
    }

    /// <inheritdoc />
    public RenderedMolecule Render(SKCanvas canvas, SKRect bounds, MoleculeDepiction depiction, MoleculeRenderOptions options, MoleculeHighlights? highlights = null)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(depiction);
        ArgumentNullException.ThrowIfNull(options);
        highlights ??= MoleculeHighlights.None;

        if (options.BackgroundColor.Alpha > 0)
        {
            using var bg = new SKPaint { Color = options.BackgroundColor, Style = SKPaintStyle.Fill };
            canvas.DrawRect(bounds, bg);
        }

        var frame = ComputeFrame(bounds, depiction, options, highlights);
        if (depiction.Atoms.Count == 0 || bounds.Width <= 0 || bounds.Height <= 0)
            return frame.Result;

        canvas.Save();
        canvas.ClipRect(bounds);
        try
        {
            var labels = BuildLabels(depiction, frame, options);
            DrawHighlights(canvas, depiction, frame, options, highlights);
            DrawBonds(canvas, depiction, frame, options, labels);
            DrawLabels(canvas, frame, labels);
            if (options.ShowAtomIndices)
                DrawAtomIndices(canvas, depiction, frame, options);
            DrawSelection(canvas, depiction, frame, options, highlights);
            if (options.ShowColorBar && highlights.AtomWeights is { Count: > 0 })
                DrawColorBar(canvas, frame, options, highlights.Colormap);
        }
        finally
        {
            canvas.Restore();
        }

        return frame.Result;
    }

    private SKTypeface ResolveTypeface(MoleculeRenderOptions options)
    {
        if (options.Typeface is not null)
            return options.Typeface;
        if (string.IsNullOrWhiteSpace(options.FontFamily))
            return SKTypeface.Default;
        return _typefaces.GetOrAdd(options.FontFamily, static family => SKTypeface.FromFamilyName(family) ?? SKTypeface.Default);
    }

    // ---------------------------------------------------------------- layout

    private sealed class Frame
    {
        public required RenderedMolecule Result { get; init; }

        public required SKPoint[] Points { get; init; }

        public required float Scale { get; init; }

        public required float BondLengthPx { get; init; }

        public required float FontSize { get; init; }

        public required float BondWidth { get; init; }

        public required float SizeFactor { get; init; }

        public required SKTypeface Typeface { get; init; }

        public required SKRect ColorBarArea { get; init; }
    }

    private Frame ComputeFrame(SKRect bounds, MoleculeDepiction depiction, MoleculeRenderOptions options, MoleculeHighlights? highlights)
    {
        var typeface = ResolveTypeface(options);
        var radiusScale = highlights?.RadiusScale is > 0 and var rs ? rs : 1f;
        var modelBond = depiction.GetMedianBondLength() ?? DefaultModelBondLength;
        var maxBond = Math.Max(1f, options.MaxBondLength);

        var content = bounds;
        content.Inflate(-Math.Max(0, options.Padding), -Math.Max(0, options.Padding));
        var colorBarArea = SKRect.Empty;
        if (options.ShowColorBar)
        {
            var (barSpace, area) = ReserveColorBar(content, options, typeface);
            colorBarArea = area;
            if (options.ColorBarPosition == ColorBarPosition.Right)
                content.Right -= barSpace;
            else
                content.Bottom -= barSpace;
        }

        if (depiction.Atoms.Count == 0 || content.Width <= 0 || content.Height <= 0)
        {
            return new Frame
            {
                Result = RenderedMolecule.Empty, Points = [], Scale = 0, BondLengthPx = 0, FontSize = options.FontSize, BondWidth = options.BondWidth,
                SizeFactor = 1, Typeface = typeface, ColorBarArea = colorBarArea,
            };
        }

        // model extents incl. labels (measured at nominal size, converted to model units) and highlight discs
        var nominalScale = maxBond / modelBond;
        var discMargin = (options.HighlightRadius * Math.Max(1f, radiusScale) * modelBond) + (options.SelectionRingWidth / nominalScale);
        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        foreach (var atom in depiction.Atoms)
        {
            minX = Math.Min(minX, atom.X - discMargin);
            maxX = Math.Max(maxX, atom.X + discMargin);
            minY = Math.Min(minY, atom.Y - discMargin);
            maxY = Math.Max(maxY, atom.Y + discMargin);
            if (!atom.ShowLabel)
                continue;
            var label = LabelLayout.Build(atom, SKPoint.Empty, typeface, options.FontSize, SKColors.Black).Union;
            minX = Math.Min(minX, atom.X + (label.Left / nominalScale));
            maxX = Math.Max(maxX, atom.X + (label.Right / nominalScale));
            minY = Math.Min(minY, atom.Y - (label.Bottom / nominalScale));
            maxY = Math.Max(maxY, atom.Y - (label.Top / nominalScale));
        }

        var modelWidth = Math.Max(maxX - minX, 1e-6);
        var modelHeight = Math.Max(maxY - minY, 1e-6);
        var scale = (float)Math.Min(Math.Min(content.Width / modelWidth, content.Height / modelHeight), nominalScale);
        var centerX = (minX + maxX) / 2;
        var centerY = (minY + maxY) / 2;

        var transform = new SKMatrix(
            scale, 0, content.MidX - (float)(centerX * scale),
            0, -scale, content.MidY + (float)(centerY * scale),
            0, 0, 1);

        var points = new SKPoint[depiction.Atoms.Count];
        for (var i = 0; i < points.Length; i++)
            points[i] = transform.MapPoint((float)depiction.Atoms[i].X, (float)depiction.Atoms[i].Y);

        var bondLengthPx = (float)(modelBond * scale);
        var k = Math.Min(1f, bondLengthPx / maxBond);
        var fontSize = Math.Max(options.MinFontSize, options.FontSize * k);
        var bondWidth = Math.Max(0.5f, options.BondWidth * k);
        var atomRadius = Math.Max(options.HighlightRadius * radiusScale * bondLengthPx, fontSize * 0.6f);
        var bondHitWidth = Math.Max(bondWidth * 3, bondLengthPx * 0.2f);
        var bonds = depiction.Bonds.Select(b => (b.Begin, b.End)).ToArray();

        return new Frame
        {
            Result = new RenderedMolecule(transform, points, bonds, atomRadius, bondLengthPx, bondHitWidth, bounds),
            Points = points,
            Scale = scale,
            BondLengthPx = bondLengthPx,
            FontSize = fontSize,
            BondWidth = bondWidth,
            SizeFactor = k,
            Typeface = typeface,
            ColorBarArea = colorBarArea,
        };
    }

    private static (float Space, SKRect Area) ReserveColorBar(SKRect content, MoleculeRenderOptions options, SKTypeface typeface)
    {
        using var font = new SKFont(typeface, ColorBarFontSize(options));
        var labelWidth = Math.Max(font.MeasureText(options.ColorBarMinLabel), font.MeasureText(options.ColorBarMaxLabel));
        var gap = 4f;
        var thickness = Math.Max(2, options.ColorBarThickness);
        if (options.ColorBarPosition == ColorBarPosition.Right)
        {
            var space = gap + thickness + gap + labelWidth;
            var length = Math.Min(content.Height * 0.8f, 220);
            var left = content.Right - labelWidth - gap - thickness;
            var top = content.MidY - (length / 2);
            return (space + gap, new SKRect(left, top, left + thickness, top + length));
        }
        else
        {
            var textHeight = font.Size * 1.2f;
            var space = gap + thickness + gap + textHeight;
            var length = Math.Min(content.Width * 0.6f, 260);
            var top = content.Bottom - textHeight - gap - thickness;
            var left = content.MidX - (length / 2);
            return (space + gap, new SKRect(left, top, left + length, top + thickness));
        }
    }

    private static float ColorBarFontSize(MoleculeRenderOptions options) => Math.Max(8f, options.FontSize * 0.8f);

    private static AtomLabel?[] BuildLabels(MoleculeDepiction depiction, Frame frame, MoleculeRenderOptions options)
    {
        var labels = new AtomLabel?[depiction.Atoms.Count];
        for (var i = 0; i < labels.Length; i++)
        {
            var atom = depiction.Atoms[i];
            if (!atom.ShowLabel)
                continue;
            var color = options.AtomColors.GetColor(atom.AtomicNumber, options.ForegroundColor);
            labels[i] = LabelLayout.Build(atom, frame.Points[i], frame.Typeface, frame.FontSize, color);
        }

        return labels;
    }

    // ---------------------------------------------------------------- highlights

    private static void DrawHighlights(SKCanvas canvas, MoleculeDepiction depiction, Frame frame, MoleculeRenderOptions options, MoleculeHighlights highlights)
    {
        var radiusScale = highlights.RadiusScale > 0 ? highlights.RadiusScale : 1f;
        var baseRadius = options.HighlightRadius * radiusScale * frame.BondLengthPx;
        var opacity = Math.Clamp(options.HighlightOpacity, 0f, 1f);

        // a layer gives uniform opacity where discs/bands overlap
        var useLayer = !options.WeightScalesOpacity && !options.SoftHighlights && opacity < 1f;
        if (useLayer)
        {
            using var layerPaint = new SKPaint { Color = SKColors.Black.WithAlpha((byte)Math.Round(opacity * 255)) };
            canvas.SaveLayer(layerPaint);
        }

        try
        {
            if (highlights.BondColors.Count > 0)
            {
                using var band = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeWidth = baseRadius * 0.9f };
                foreach (var (bondIndex, color) in highlights.BondColors)
                {
                    if ((uint)bondIndex >= (uint)depiction.Bonds.Count)
                        continue;
                    var b = depiction.Bonds[bondIndex];
                    band.Color = useLayer ? color : color.WithAlpha((byte)(color.Alpha * opacity));
                    canvas.DrawLine(frame.Points[b.Begin], frame.Points[b.End], band);
                }
            }

            using var disc = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
            for (var i = 0; i < depiction.Atoms.Count; i++)
            {
                if (highlights.GetAtomColor(i, options.HighlightThreshold, out var weight) is not { } color)
                    continue;
                var hasWeight = !float.IsNaN(weight);
                var radius = options.WeightScalesRadius && hasWeight ? baseRadius * (0.35f + (0.65f * weight)) : baseRadius;
                var alpha = useLayer ? 1f : opacity;
                if (options.WeightScalesOpacity && hasWeight)
                    alpha *= Math.Max(0.12f, weight);
                var c = color.WithAlpha((byte)Math.Round(color.Alpha * alpha));
                var center = frame.Points[i];
                if (options.SoftHighlights)
                {
                    using var shader = SKShader.CreateRadialGradient(center, radius, [c, c, c.WithAlpha(0)], [0f, 0.45f, 1f], SKShaderTileMode.Clamp);
                    disc.Shader = shader;
                    canvas.DrawCircle(center, radius, disc);
                    disc.Shader = null;
                }
                else
                {
                    disc.Color = c;
                    canvas.DrawCircle(center, radius, disc);
                }
            }
        }
        finally
        {
            if (useLayer)
                canvas.Restore();
        }
    }

    private static void DrawSelection(SKCanvas canvas, MoleculeDepiction depiction, Frame frame, MoleculeRenderOptions options, MoleculeHighlights highlights)
    {
        var radiusScale = highlights.RadiusScale > 0 ? highlights.RadiusScale : 1f;
        var width = Math.Max(1f, options.SelectionRingWidth * frame.SizeFactor);
        var radius = Math.Max(options.HighlightRadius * radiusScale * frame.BondLengthPx, frame.FontSize * 0.75f) + (width / 2);
        using var ring = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = width };

        if (highlights.HoveredAtom is { } hovered && (uint)hovered < (uint)depiction.Atoms.Count)
        {
            ring.Color = options.HoverColor;
            var r = highlights.SelectedAtom == hovered ? radius + (width * 1.5f) : radius;
            canvas.DrawCircle(frame.Points[hovered], r, ring);
        }

        if (highlights.SelectedAtom is { } selected && (uint)selected < (uint)depiction.Atoms.Count)
        {
            ring.Color = options.SelectionColor;
            canvas.DrawCircle(frame.Points[selected], radius, ring);
        }
    }

    // ---------------------------------------------------------------- bonds

    private static void DrawBonds(SKCanvas canvas, MoleculeDepiction depiction, Frame frame, MoleculeRenderOptions options, AtomLabel?[] labels)
    {
        using var stroke = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = frame.BondWidth,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            Color = options.ForegroundColor,
        };
        using var fill = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = options.ForegroundColor };

        var aromaticRingBonds = options.AromaticStyle == AromaticStyle.Circle ? AromaticRingBonds(depiction) : null;
        var spacing = options.MultipleBondSpacing * frame.BondLengthPx;

        foreach (var bond in depiction.Bonds)
        {
            var b = frame.Points[bond.Begin];
            var e = frame.Points[bond.End];
            var beginColor = BondColor(depiction.Atoms[bond.Begin], options);
            var endColor = BondColor(depiction.Atoms[bond.End], options);
            var ctx = new BondContext(canvas, stroke, fill, labels[bond.Begin], labels[bond.End], beginColor, endColor);

            var order = bond.Order;
            if (order == BondOrder.Aromatic)
            {
                order = aromaticRingBonds is not null && aromaticRingBonds.Contains(bond.Index) ? BondOrder.Single : bond.KekuleOrder;
            }

            switch (order)
            {
                case BondOrder.Single:
                    DrawSingle(ctx, bond, b, e, frame, options);
                    break;

                case BondOrder.Double:
                    DrawDouble(ctx, bond, b, e, spacing);
                    break;

                case BondOrder.Triple:
                    DrawLine(ctx, b, e);
                    DrawLine(ctx, Offset(b, e, spacing), Offset(e, b, -spacing));
                    DrawLine(ctx, Offset(b, e, -spacing), Offset(e, b, spacing));
                    break;

                default:
                    // aromatic bond without known Kekulé form: solid + dashed line
                    DrawLine(ctx, b, e);
                    using (var dash = SKPathEffect.CreateDash([frame.BondWidth * 2.5f, frame.BondWidth * 2f], 0))
                    {
                        stroke.PathEffect = dash;
                        var side = bond.Placement == DoubleBondPlacement.Right ? -1 : 1;
                        DrawInnerLine(ctx, b, e, side * spacing, bond.IsInRing);
                        stroke.PathEffect = null;
                    }

                    break;
            }
        }

        if (aromaticRingBonds is not null)
            DrawAromaticCircles(canvas, depiction, frame, stroke, options);
    }

    private readonly record struct BondContext(SKCanvas Canvas, SKPaint Stroke, SKPaint Fill, AtomLabel? BeginLabel, AtomLabel? EndLabel, SKColor BeginColor, SKColor EndColor);

    private static SKColor BondColor(DepictionAtom atom, MoleculeRenderOptions options)
        => options.ColorBondsByAtom ? options.AtomColors.GetColor(atom.AtomicNumber, options.ForegroundColor) : options.ForegroundColor;

    private static void DrawSingle(BondContext ctx, DepictionBond bond, SKPoint b, SKPoint e, Frame frame, MoleculeRenderOptions options)
    {
        if (bond.Stereo == BondStereoDisplay.None)
        {
            DrawLine(ctx, b, e);
            return;
        }

        if (!Trim(b, e, ctx.BeginLabel, ctx.EndLabel, out var tb, out var te))
            return;
        var dx = te.X - tb.X;
        var dy = te.Y - tb.Y;
        var len = MathF.Sqrt((dx * dx) + (dy * dy));
        if (len < 0.5f)
            return;
        var nx = -dy / len;
        var ny = dx / len;
        var halfWidth = Math.Max(frame.BondWidth, options.WedgeWidth * frame.BondLengthPx) / 2;
        ctx.Fill.Color = ctx.BeginColor;
        ctx.Stroke.Color = ctx.BeginColor;

        switch (bond.Stereo)
        {
            case BondStereoDisplay.Wedge:
                using (var path = new SKPath())
                {
                    path.MoveTo(tb.X + (nx * frame.BondWidth * 0.3f), tb.Y + (ny * frame.BondWidth * 0.3f));
                    path.LineTo(te.X + (nx * halfWidth), te.Y + (ny * halfWidth));
                    path.LineTo(te.X - (nx * halfWidth), te.Y - (ny * halfWidth));
                    path.LineTo(tb.X - (nx * frame.BondWidth * 0.3f), tb.Y - (ny * frame.BondWidth * 0.3f));
                    path.Close();
                    ctx.Canvas.DrawPath(path, ctx.Fill);
                }

                break;

            case BondStereoDisplay.Hash:
                var count = Math.Max(4, (int)(len / Math.Max(2.5f, frame.BondWidth * 2.6f)));
                var oldWidth = ctx.Stroke.StrokeWidth;
                var oldCap = ctx.Stroke.StrokeCap;
                ctx.Stroke.StrokeWidth = Math.Max(0.5f, frame.BondWidth * 0.8f);
                ctx.Stroke.StrokeCap = SKStrokeCap.Butt;
                for (var i = 0; i < count; i++)
                {
                    var t = count == 1 ? 1 : (float)i / (count - 1);
                    var w = Math.Max(frame.BondWidth * 0.6f, halfWidth * t);
                    var cx = tb.X + (dx * t);
                    var cy = tb.Y + (dy * t);
                    ctx.Canvas.DrawLine(cx + (nx * w), cy + (ny * w), cx - (nx * w), cy - (ny * w), ctx.Stroke);
                }

                ctx.Stroke.StrokeWidth = oldWidth;
                ctx.Stroke.StrokeCap = oldCap;
                break;

            case BondStereoDisplay.Wavy:
                using (var path = new SKPath())
                {
                    var waves = Math.Max(3, (int)MathF.Round(len / Math.Max(4f, halfWidth * 1.6f)));
                    var amplitude = halfWidth * 0.7f;
                    path.MoveTo(tb);
                    for (var i = 0; i < waves; i++)
                    {
                        var t0 = (float)i / waves;
                        var t1 = (float)(i + 1) / waves;
                        var tm = (t0 + t1) / 2;
                        var sign = i % 2 == 0 ? 1 : -1;
                        path.QuadTo(
                            tb.X + (dx * tm) + (nx * amplitude * 2 * sign),
                            tb.Y + (dy * tm) + (ny * amplitude * 2 * sign),
                            tb.X + (dx * t1),
                            tb.Y + (dy * t1));
                    }

                    ctx.Canvas.DrawPath(path, ctx.Stroke);
                }

                break;
        }
    }

    private static void DrawDouble(BondContext ctx, DepictionBond bond, SKPoint b, SKPoint e, float spacing)
    {
        var placement = bond.Placement;
        var beginLabelled = ctx.BeginLabel is not null;
        var endLabelled = ctx.EndLabel is not null;

        // a side placement looks odd when both ends carry labels (e.g. O=N): centre it
        if (placement != DoubleBondPlacement.Center && beginLabelled && endLabelled && !bond.IsInRing)
            placement = DoubleBondPlacement.Center;

        if (placement == DoubleBondPlacement.Center)
        {
            DrawLine(ctx, Offset(b, e, spacing / 2), Offset(e, b, -spacing / 2));
            DrawLine(ctx, Offset(b, e, -spacing / 2), Offset(e, b, spacing / 2));
            return;
        }

        DrawLine(ctx, b, e);
        var side = placement == DoubleBondPlacement.Left ? 1 : -1;
        DrawInnerLine(ctx, b, e, side * spacing, bond.IsInRing);
    }

    /// <summary>Draws a line parallel to b→e on the given side (positive = left in model space), shortened at unlabelled ends.</summary>
    private static void DrawInnerLine(BondContext ctx, SKPoint b, SKPoint e, float offset, bool inRing)
    {
        var ob = Offset(b, e, offset);
        var oe = Offset(e, b, -offset);
        var dx = oe.X - ob.X;
        var dy = oe.Y - ob.Y;
        var len = MathF.Sqrt((dx * dx) + (dy * dy));
        if (len < 1)
            return;
        var shorten = Math.Abs(offset) * (inRing ? 0.85f : 0.6f);
        var ux = dx / len;
        var uy = dy / len;
        if (ctx.BeginLabel is null)
            ob = new SKPoint(ob.X + (ux * shorten), ob.Y + (uy * shorten));
        if (ctx.EndLabel is null)
            oe = new SKPoint(oe.X - (ux * shorten), oe.Y - (uy * shorten));
        DrawLine(ctx, ob, oe);
    }

    /// <summary>
    /// Offsets point <paramref name="p"/> perpendicular to the direction p→<paramref name="towards"/>.
    /// Positive values move to the left in model space (y up), i.e. the screen normal (dy, -dx).
    /// </summary>
    private static SKPoint Offset(SKPoint p, SKPoint towards, float amount)
    {
        var dx = towards.X - p.X;
        var dy = towards.Y - p.Y;
        var len = MathF.Sqrt((dx * dx) + (dy * dy));
        if (len < 1e-6f)
            return p;
        return new SKPoint(p.X + (dy / len * amount), p.Y - (dx / len * amount));
    }

    private static void DrawLine(BondContext ctx, SKPoint b, SKPoint e)
    {
        if (!Trim(b, e, ctx.BeginLabel, ctx.EndLabel, out var tb, out var te))
            return;
        if (ctx.BeginColor == ctx.EndColor)
        {
            ctx.Stroke.Color = ctx.BeginColor;
            ctx.Canvas.DrawLine(tb, te, ctx.Stroke);
            return;
        }

        var mid = new SKPoint((b.X + e.X) / 2, (b.Y + e.Y) / 2);
        ctx.Stroke.Color = ctx.BeginColor;
        ctx.Canvas.DrawLine(tb, mid, ctx.Stroke);
        ctx.Stroke.Color = ctx.EndColor;
        ctx.Canvas.DrawLine(mid, te, ctx.Stroke);
    }

    /// <summary>Clips the segment b→e against the label rectangles of its end atoms.</summary>
    internal static bool Trim(SKPoint b, SKPoint e, AtomLabel? beginLabel, AtomLabel? endLabel, out SKPoint tb, out SKPoint te)
    {
        var t0 = 0f;
        var t1 = 1f;
        if (beginLabel is not null)
        {
            foreach (var r in beginLabel.Rects)
            {
                if (SegmentRect(b, e, r, out var enter, out var exit) && enter <= 0.5f)
                    t0 = Math.Max(t0, exit);
            }
        }

        if (endLabel is not null)
        {
            foreach (var r in endLabel.Rects)
            {
                if (SegmentRect(b, e, r, out var enter, out var exit) && exit >= 0.5f)
                    t1 = Math.Min(t1, enter);
            }
        }

        tb = new SKPoint(b.X + ((e.X - b.X) * t0), b.Y + ((e.Y - b.Y) * t0));
        te = new SKPoint(b.X + ((e.X - b.X) * t1), b.Y + ((e.Y - b.Y) * t1));
        return t1 - t0 > 1e-3f;
    }

    /// <summary>Liang–Barsky: parametric interval of the segment inside the rectangle.</summary>
    private static bool SegmentRect(SKPoint a, SKPoint b, SKRect r, out float enter, out float exit)
    {
        enter = 0;
        exit = 1;
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        Span<float> p = [-dx, dx, -dy, dy];
        Span<float> q = [a.X - r.Left, r.Right - a.X, a.Y - r.Top, r.Bottom - a.Y];
        for (var i = 0; i < 4; i++)
        {
            if (Math.Abs(p[i]) < 1e-9f)
            {
                if (q[i] < 0)
                    return false;
                continue;
            }

            var t = q[i] / p[i];
            if (p[i] < 0)
                enter = Math.Max(enter, t);
            else
                exit = Math.Min(exit, t);
            if (enter > exit)
                return false;
        }

        return true;
    }

    private static HashSet<int> AromaticRingBonds(MoleculeDepiction depiction)
    {
        var set = new HashSet<int>();
        if (depiction.Rings.Count == 0)
            return set;
        var lookup = new Dictionary<(int, int), int>();
        foreach (var b in depiction.Bonds)
            lookup[(Math.Min(b.Begin, b.End), Math.Max(b.Begin, b.End))] = b.Index;
        foreach (var ring in depiction.Rings.Where(r => r.IsAromatic))
        {
            for (var i = 0; i < ring.AtomIndices.Count; i++)
            {
                var u = ring.AtomIndices[i];
                var v = ring.AtomIndices[(i + 1) % ring.AtomIndices.Count];
                if (lookup.TryGetValue((Math.Min(u, v), Math.Max(u, v)), out var idx))
                    set.Add(idx);
            }
        }

        return set;
    }

    private static void DrawAromaticCircles(SKCanvas canvas, MoleculeDepiction depiction, Frame frame, SKPaint stroke, MoleculeRenderOptions options)
    {
        stroke.Color = options.ForegroundColor;
        foreach (var ring in depiction.Rings.Where(r => r.IsAromatic))
        {
            float cx = 0, cy = 0;
            foreach (var a in ring.AtomIndices)
            {
                cx += frame.Points[a].X;
                cy += frame.Points[a].Y;
            }

            cx /= ring.AtomIndices.Count;
            cy /= ring.AtomIndices.Count;
            var center = new SKPoint(cx, cy);
            var inner = float.MaxValue;
            for (var i = 0; i < ring.AtomIndices.Count; i++)
            {
                var p = frame.Points[ring.AtomIndices[i]];
                var q = frame.Points[ring.AtomIndices[(i + 1) % ring.AtomIndices.Count]];
                inner = Math.Min(inner, RenderedMolecule.DistanceToSegment(center, p, q));
            }

            if (inner is > 1 and < float.MaxValue)
                canvas.DrawCircle(center, inner * 0.6f, stroke);
        }
    }

    // ---------------------------------------------------------------- labels

    private static void DrawLabels(SKCanvas canvas, Frame frame, AtomLabel?[] labels)
    {
        using var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
        var fonts = new Dictionary<float, SKFont>();
        try
        {
            for (var i = 0; i < labels.Length; i++)
            {
                var label = labels[i];
                if (label is null)
                    continue;
                foreach (var run in label.Runs)
                {
                    if (!fonts.TryGetValue(run.FontSize, out var font))
                    {
                        font = new SKFont(frame.Typeface, run.FontSize) { Subpixel = true, Edging = SKFontEdging.Antialias };
                        fonts[run.FontSize] = font;
                    }

                    paint.Color = run.Color;
                    canvas.DrawText(run.Text, run.X, run.Baseline, SKTextAlign.Left, font, paint);
                }

                if (label.Dots.Count > 0)
                {
                    paint.Color = label.Runs[0].Color;
                    foreach (var dot in label.Dots)
                        canvas.DrawCircle(dot, label.DotRadius, paint);
                }
            }
        }
        finally
        {
            foreach (var f in fonts.Values)
                f.Dispose();
        }
    }

    private static void DrawAtomIndices(SKCanvas canvas, MoleculeDepiction depiction, Frame frame, MoleculeRenderOptions options)
    {
        var size = Math.Max(options.MinFontSize, frame.FontSize * 0.62f);
        using var font = new SKFont(frame.Typeface, size) { Subpixel = true, Edging = SKFontEdging.Antialias };
        using var paint = new SKPaint { IsAntialias = true, Color = options.AtomIndexColor };
        var neighbours = new List<int>[depiction.Atoms.Count];
        for (var i = 0; i < neighbours.Length; i++)
            neighbours[i] = [];
        foreach (var b in depiction.Bonds)
        {
            neighbours[b.Begin].Add(b.End);
            neighbours[b.End].Add(b.Begin);
        }

        var distance = Math.Max(frame.FontSize * 0.9f, frame.BondLengthPx * 0.32f);
        for (var i = 0; i < depiction.Atoms.Count; i++)
        {
            var p = frame.Points[i];
            float vx = 0, vy = 0;
            foreach (var n in neighbours[i])
            {
                var dx = frame.Points[n].X - p.X;
                var dy = frame.Points[n].Y - p.Y;
                var len = MathF.Sqrt((dx * dx) + (dy * dy));
                if (len > 0)
                {
                    vx += dx / len;
                    vy += dy / len;
                }
            }

            var vlen = MathF.Sqrt((vx * vx) + (vy * vy));
            var (ux, uy) = vlen > 1e-3f ? (-vx / vlen, -vy / vlen) : (0.7071f, -0.7071f);
            var text = i.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var cap = font.Metrics.CapHeight > 0 ? font.Metrics.CapHeight : size * 0.7f;
            canvas.DrawText(text, p.X + (ux * distance), p.Y + (uy * distance) + (cap / 2), SKTextAlign.Center, font, paint);
        }
    }

    private static void DrawColorBar(SKCanvas canvas, Frame frame, MoleculeRenderOptions options, Colormap colormap)
    {
        var area = frame.ColorBarArea;
        if (area.IsEmpty)
            return;
        var colors = colormap.Stops.ToArray();
        var positions = Enumerable.Range(0, colors.Length).Select(i => (float)i / (colors.Length - 1)).ToArray();
        var vertical = options.ColorBarPosition == ColorBarPosition.Right;

        // low values at the bottom (vertical) or left (horizontal)
        var start = vertical ? new SKPoint(area.MidX, area.Bottom) : new SKPoint(area.Left, area.MidY);
        var end = vertical ? new SKPoint(area.MidX, area.Top) : new SKPoint(area.Right, area.MidY);
        using (var shader = SKShader.CreateLinearGradient(start, end, colors, positions, SKShaderTileMode.Clamp))
        using (var paint = new SKPaint { Shader = shader, IsAntialias = true })
            canvas.DrawRect(area, paint);

        using var frameStroke = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1, Color = options.ColorBarTextColor.WithAlpha(160) };
        canvas.DrawRect(area, frameStroke);

        using var font = new SKFont(frame.Typeface, ColorBarFontSize(options)) { Subpixel = true, Edging = SKFontEdging.Antialias };
        using var text = new SKPaint { IsAntialias = true, Color = options.ColorBarTextColor };
        var cap = font.Metrics.CapHeight > 0 ? font.Metrics.CapHeight : font.Size * 0.7f;
        if (vertical)
        {
            canvas.DrawText(options.ColorBarMaxLabel, area.Right + 4, area.Top + cap, SKTextAlign.Left, font, text);
            canvas.DrawText(options.ColorBarMinLabel, area.Right + 4, area.Bottom, SKTextAlign.Left, font, text);
        }
        else
        {
            canvas.DrawText(options.ColorBarMinLabel, area.Left, area.Bottom + 4 + cap, SKTextAlign.Left, font, text);
            canvas.DrawText(options.ColorBarMaxLabel, area.Right, area.Bottom + 4 + cap, SKTextAlign.Right, font, text);
        }
    }
}

