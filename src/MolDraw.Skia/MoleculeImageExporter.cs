using MolDraw.Depiction;
using SkiaSharp;

namespace MolDraw.Skia;

/// <summary>Renders depictions to PNG or SVG (e.g. for server-side use, reports or snapshot tests).</summary>
/// <remarks>Thread-safe.</remarks>
public sealed class MoleculeImageExporter
{
    private readonly IMoleculeRenderer _renderer;

    /// <summary>Creates an exporter.</summary>
    /// <param name="renderer">Renderer; <see langword="null"/> uses a new <see cref="MoleculeRenderer"/>.</param>
    public MoleculeImageExporter(IMoleculeRenderer? renderer = null)
    {
        _renderer = renderer ?? new MoleculeRenderer();
    }

    /// <summary>Renders into a new bitmap (premultiplied RGBA8888). The caller owns the bitmap.</summary>
    /// <param name="depiction">Depiction.</param>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="options">Options; <see langword="null"/> uses <see cref="MoleculeRenderOptions.Light"/>.</param>
    /// <param name="highlights">Optional highlights.</param>
    /// <param name="background">Background colour (overrides <see cref="MoleculeRenderOptions.BackgroundColor"/> if not <see langword="null"/>).</param>
    public SKBitmap RenderBitmap(MoleculeDepiction depiction, int width, int height, MoleculeRenderOptions? options = null, MoleculeHighlights? highlights = null, SKColor? background = null)
    {
        ValidateSize(width, height);
        var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(background ?? SKColors.Transparent);
        _renderer.Render(canvas, new SKRect(0, 0, width, height), depiction, Resolve(options, background), highlights);
        canvas.Flush();
        return bitmap;
    }

    /// <summary>Renders to PNG bytes.</summary>
    /// <param name="depiction">Depiction.</param>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="options">Options; <see langword="null"/> uses <see cref="MoleculeRenderOptions.Light"/>.</param>
    /// <param name="highlights">Optional highlights.</param>
    /// <param name="background">Optional background colour (default: transparent).</param>
    public byte[] RenderPng(MoleculeDepiction depiction, int width, int height, MoleculeRenderOptions? options = null, MoleculeHighlights? highlights = null, SKColor? background = null)
    {
        using var bitmap = RenderBitmap(depiction, width, height, options, highlights, background);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    /// <summary>Renders to an SVG document.</summary>
    /// <param name="depiction">Depiction.</param>
    /// <param name="width">Width in SVG user units.</param>
    /// <param name="height">Height in SVG user units.</param>
    /// <param name="options">Options; <see langword="null"/> uses <see cref="MoleculeRenderOptions.Light"/>.</param>
    /// <param name="highlights">Optional highlights.</param>
    /// <param name="background">Optional background colour (default: none).</param>
    public string RenderSvg(MoleculeDepiction depiction, int width, int height, MoleculeRenderOptions? options = null, MoleculeHighlights? highlights = null, SKColor? background = null)
    {
        ValidateSize(width, height);
        using var stream = new MemoryStream();
        using (var managed = new SKManagedWStream(stream))
        {
            var bounds = new SKRect(0, 0, width, height);
            using (var canvas = SKSvgCanvas.Create(bounds, managed))
            {
                if (background is { } bg)
                {
                    using var paint = new SKPaint { Color = bg };
                    canvas.DrawRect(bounds, paint);
                }

                _renderer.Render(canvas, bounds, depiction, Resolve(options, null), highlights);
            }
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static MoleculeRenderOptions Resolve(MoleculeRenderOptions? options, SKColor? background)
    {
        var o = options ?? MoleculeRenderOptions.Light;
        return background is null ? o : o with { BackgroundColor = SKColors.Transparent };
    }

    private static void ValidateSize(int width, int height)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be positive.");
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be positive.");
    }
}
