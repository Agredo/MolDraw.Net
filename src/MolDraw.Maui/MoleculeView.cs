using System.Windows.Input;
using MolDraw.Depiction;
using MolDraw.Skia;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace MolDraw.Maui;

/// <summary>
/// Displays a 2D chemical structure with optional per-atom highlighting and atom selection/hover.
/// Input is one of <see cref="Depiction"/> (highest precedence) or <see cref="Smiles"/>.
/// Designed for MVVM: all inputs are bindable, selection is two-way, hover and errors are pushed to the view model,
/// and taps/hover raise commands. The control never throws for invalid input; it shows and reports <see cref="ErrorMessage"/>.
/// </summary>
/// <remarks>
/// Services (<see cref="IChemistryEngine"/>, <see cref="IMoleculeRenderer"/>, <see cref="IDepictionCache"/>) are taken
/// from the constructor, otherwise from the MAUI service provider (see
/// <see cref="MolDrawMauiAppBuilderExtensions.UseMolDraw"/>). <see cref="Smiles"/> needs an <see cref="IChemistryEngine"/>:
/// register one from a package such as <c>MolDraw.Core</c> (NCDK) via <c>services.AddMolDrawCore()</c>. Without an engine,
/// <see cref="Depiction"/> still works; setting <see cref="Smiles"/> reports that in <see cref="ErrorMessage"/>.
/// Parsing and 2D layout of uncached input run on a background thread.
/// </remarks>
public class MoleculeView : ContentView
{
    /// <summary>Bindable property for <see cref="Smiles"/>.</summary>
    public static readonly BindableProperty SmilesProperty = BindableProperty.Create(
        nameof(Smiles), typeof(string), typeof(MoleculeView), null, propertyChanged: OnInputChanged);

    /// <summary>Bindable property for <see cref="Depiction"/>.</summary>
    public static readonly BindableProperty DepictionProperty = BindableProperty.Create(
        nameof(Depiction), typeof(MoleculeDepiction), typeof(MoleculeView), null, propertyChanged: OnInputChanged);

    /// <summary>Bindable property for <see cref="DepictionOptions"/>.</summary>
    public static readonly BindableProperty DepictionOptionsProperty = BindableProperty.Create(
        nameof(DepictionOptions), typeof(DepictionBuildOptions), typeof(MoleculeView), null, propertyChanged: OnInputChanged);

    /// <summary>Bindable property for <see cref="RenderOptions"/>.</summary>
    public static readonly BindableProperty RenderOptionsProperty = BindableProperty.Create(
        nameof(RenderOptions), typeof(MoleculeRenderOptions), typeof(MoleculeView), null, propertyChanged: OnVisualChanged);

    /// <summary>Bindable property for <see cref="AtomWeights"/>.</summary>
    public static readonly BindableProperty AtomWeightsProperty = BindableProperty.Create(
        nameof(AtomWeights), typeof(IReadOnlyList<float>), typeof(MoleculeView), null, propertyChanged: OnVisualChanged);

    /// <summary>Bindable property for <see cref="AtomColors"/>.</summary>
    public static readonly BindableProperty AtomColorsProperty = BindableProperty.Create(
        nameof(AtomColors), typeof(IReadOnlyDictionary<int, SKColor>), typeof(MoleculeView), null, propertyChanged: OnVisualChanged);

    /// <summary>Bindable property for <see cref="BondColors"/>.</summary>
    public static readonly BindableProperty BondColorsProperty = BindableProperty.Create(
        nameof(BondColors), typeof(IReadOnlyDictionary<int, SKColor>), typeof(MoleculeView), null, propertyChanged: OnVisualChanged);

    /// <summary>Bindable property for <see cref="Colormap"/>.</summary>
    public static readonly BindableProperty ColormapProperty = BindableProperty.Create(
        nameof(Colormap), typeof(Colormap), typeof(MoleculeView), Colormap.Viridis, propertyChanged: OnVisualChanged);

    /// <summary>Bindable property for <see cref="ShowColorBar"/>.</summary>
    public static readonly BindableProperty ShowColorBarProperty = BindableProperty.Create(
        nameof(ShowColorBar), typeof(bool), typeof(MoleculeView), false, propertyChanged: OnVisualChanged);

    /// <summary>Bindable property for <see cref="HighlightRadiusScale"/>.</summary>
    public static readonly BindableProperty HighlightRadiusScaleProperty = BindableProperty.Create(
        nameof(HighlightRadiusScale), typeof(float), typeof(MoleculeView), 1f, propertyChanged: OnVisualChanged);

    /// <summary>Bindable property for <see cref="SelectedAtomIndex"/> (two-way by default).</summary>
    public static readonly BindableProperty SelectedAtomIndexProperty = BindableProperty.Create(
        nameof(SelectedAtomIndex), typeof(int?), typeof(MoleculeView), null, BindingMode.TwoWay, propertyChanged: OnVisualChanged);

    /// <summary>Bindable property for <see cref="HoveredAtomIndex"/> (one-way-to-source by default).</summary>
    public static readonly BindableProperty HoveredAtomIndexProperty = BindableProperty.Create(
        nameof(HoveredAtomIndex), typeof(int?), typeof(MoleculeView), null, BindingMode.OneWayToSource, propertyChanged: OnVisualChanged);

    /// <summary>Bindable property for <see cref="AtomTappedCommand"/>.</summary>
    public static readonly BindableProperty AtomTappedCommandProperty = BindableProperty.Create(
        nameof(AtomTappedCommand), typeof(ICommand), typeof(MoleculeView));

    /// <summary>Bindable property for <see cref="AtomHoveredCommand"/>.</summary>
    public static readonly BindableProperty AtomHoveredCommandProperty = BindableProperty.Create(
        nameof(AtomHoveredCommand), typeof(ICommand), typeof(MoleculeView));

    /// <summary>Bindable property for <see cref="ErrorMessage"/> (one-way-to-source by default).</summary>
    public static readonly BindableProperty ErrorMessageProperty = BindableProperty.Create(
        nameof(ErrorMessage), typeof(string), typeof(MoleculeView), null, BindingMode.OneWayToSource);

    /// <summary>Bindable property for <see cref="IsInteractive"/>.</summary>
    public static readonly BindableProperty IsInteractiveProperty = BindableProperty.Create(
        nameof(IsInteractive), typeof(bool), typeof(MoleculeView), true, propertyChanged: OnInteractiveChanged);

    /// <summary>Bindable property for <see cref="HitTolerance"/>.</summary>
    public static readonly BindableProperty HitToleranceProperty = BindableProperty.Create(
        nameof(HitTolerance), typeof(double), typeof(MoleculeView), 6d);

    private const string MissingEngineMessage =
        "No chemistry engine registered: bind Depiction, or register an IChemistryEngine (e.g. MolDraw.Core, then services.AddMolDrawCore()).";

    private readonly SKCanvasView _canvasView;
    private IChemistryEngine? _engine;
    private IMoleculeRenderer? _renderer;
    private IDepictionCache? _cache;
    private readonly bool _servicesFromConstructor;

    private MoleculeDepiction? _current;
    private string? _error;
    private int _inputVersion;
    private Application? _themeSource;

    /// <summary>Creates the view; services are resolved from the MAUI service provider when the handler is attached.</summary>
    public MoleculeView()
        : this(null, null, null)
    {
    }

    /// <summary>Creates the view with explicit services (e.g. for code-only UIs or tests).</summary>
    /// <param name="engine">Chemistry engine used for <see cref="Smiles"/>; <see langword="null"/> disables SMILES input.</param>
    /// <param name="renderer">Renderer.</param>
    /// <param name="cache">Depiction cache.</param>
    public MoleculeView(IChemistryEngine? engine, IMoleculeRenderer? renderer, IDepictionCache? cache)
    {
        _engine = engine;
        _renderer = renderer;
        _cache = cache;
        _servicesFromConstructor = engine is not null || renderer is not null || cache is not null;

        _canvasView = new SKCanvasView { IgnorePixelScaling = false, EnableTouchEvents = false };
        _canvasView.PaintSurface += OnPaintSurface;

        var tap = new TapGestureRecognizer();
        tap.Tapped += OnTapped;
        _canvasView.GestureRecognizers.Add(tap);

        var pointer = new PointerGestureRecognizer();
        pointer.PointerMoved += OnPointerMoved;
        pointer.PointerEntered += OnPointerMoved;
        pointer.PointerExited += OnPointerExited;
        _canvasView.GestureRecognizers.Add(pointer);

        Content = _canvasView;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <summary>SMILES input (parsed, laid out and cached by SMILES); requires a registered <see cref="IChemistryEngine"/>.</summary>
    public string? Smiles
    {
        get => (string?)GetValue(SmilesProperty);
        set => SetValue(SmilesProperty, value);
    }

    /// <summary>Precomputed depiction (takes precedence over <see cref="Smiles"/>), e.g. from a chemistry toolkit.</summary>
    public MoleculeDepiction? Depiction
    {
        get => (MoleculeDepiction?)GetValue(DepictionProperty);
        set => SetValue(DepictionProperty, value);
    }

    /// <summary>Options for building depictions from <see cref="Smiles"/>; <see langword="null"/> uses defaults.</summary>
    public DepictionBuildOptions? DepictionOptions
    {
        get => (DepictionBuildOptions?)GetValue(DepictionOptionsProperty);
        set => SetValue(DepictionOptionsProperty, value);
    }

    /// <summary>Render options; <see langword="null"/> follows the app theme (<see cref="MoleculeRenderOptions.Light"/>/<see cref="MoleculeRenderOptions.Dark"/>).</summary>
    public MoleculeRenderOptions? RenderOptions
    {
        get => (MoleculeRenderOptions?)GetValue(RenderOptionsProperty);
        set => SetValue(RenderOptionsProperty, value);
    }

    /// <summary>Per-atom weights in [0, 1] by atom index (assign a new list to update).</summary>
    public IReadOnlyList<float>? AtomWeights
    {
        get => (IReadOnlyList<float>?)GetValue(AtomWeightsProperty);
        set => SetValue(AtomWeightsProperty, value);
    }

    /// <summary>Explicit atom highlight colours by atom index (take precedence over weights).</summary>
    public IReadOnlyDictionary<int, SKColor>? AtomColors
    {
        get => (IReadOnlyDictionary<int, SKColor>?)GetValue(AtomColorsProperty);
        set => SetValue(AtomColorsProperty, value);
    }

    /// <summary>Bond highlight colours by bond index.</summary>
    public IReadOnlyDictionary<int, SKColor>? BondColors
    {
        get => (IReadOnlyDictionary<int, SKColor>?)GetValue(BondColorsProperty);
        set => SetValue(BondColorsProperty, value);
    }

    /// <summary>Colour map for <see cref="AtomWeights"/>.</summary>
    public Colormap Colormap
    {
        get => (Colormap)GetValue(ColormapProperty);
        set => SetValue(ColormapProperty, value);
    }

    /// <summary>Show a colour bar legend when weights are set.</summary>
    public bool ShowColorBar
    {
        get => (bool)GetValue(ShowColorBarProperty);
        set => SetValue(ShowColorBarProperty, value);
    }

    /// <summary>Scale factor for highlight discs (default 1).</summary>
    public float HighlightRadiusScale
    {
        get => (float)GetValue(HighlightRadiusScaleProperty);
        set => SetValue(HighlightRadiusScaleProperty, value);
    }

    /// <summary>Selected atom index; a tap selects an atom, tapping it again or empty space deselects.</summary>
    public int? SelectedAtomIndex
    {
        get => (int?)GetValue(SelectedAtomIndexProperty);
        set => SetValue(SelectedAtomIndexProperty, value);
    }

    /// <summary>Atom under the pointer (desktop), or <see langword="null"/>.</summary>
    public int? HoveredAtomIndex
    {
        get => (int?)GetValue(HoveredAtomIndexProperty);
        set => SetValue(HoveredAtomIndexProperty, value);
    }

    /// <summary>Executed with the atom index (<see cref="int"/>) when an atom is tapped.</summary>
    public ICommand? AtomTappedCommand
    {
        get => (ICommand?)GetValue(AtomTappedCommandProperty);
        set => SetValue(AtomTappedCommandProperty, value);
    }

    /// <summary>Executed with the hovered atom index (<see cref="int"/>) or <see langword="null"/> when the hover changes.</summary>
    public ICommand? AtomHoveredCommand
    {
        get => (ICommand?)GetValue(AtomHoveredCommandProperty);
        set => SetValue(AtomHoveredCommandProperty, value);
    }

    /// <summary>Error for invalid input (e.g. SMILES syntax error), <see langword="null"/> otherwise. Set by the control.</summary>
    public string? ErrorMessage
    {
        get => (string?)GetValue(ErrorMessageProperty);
        set => SetValue(ErrorMessageProperty, value);
    }

    /// <summary>Enables tap/hover handling (default <see langword="true"/>).</summary>
    public bool IsInteractive
    {
        get => (bool)GetValue(IsInteractiveProperty);
        set => SetValue(IsInteractiveProperty, value);
    }

    /// <summary>Additional hit test tolerance in device-independent units (default 6).</summary>
    public double HitTolerance
    {
        get => (double)GetValue(HitToleranceProperty);
        set => SetValue(HitToleranceProperty, value);
    }

    /// <summary>The depiction currently displayed (after asynchronous parsing/layout), or <see langword="null"/>.</summary>
    public MoleculeDepiction? CurrentDepiction => _current;

    /// <summary>Geometry of the last render pass in device-independent units (for overlays/hit testing).</summary>
    public RenderedMolecule LastRender { get; private set; } = RenderedMolecule.Empty;

    private IMoleculeRenderer Renderer => _renderer ??= new MoleculeRenderer();

    private IDepictionCache Cache => _cache ??= new DepictionCache();

    /// <inheritdoc />
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (_servicesFromConstructor || Handler?.MauiContext?.Services is not { } services)
            return;
        _engine ??= services.GetService(typeof(IChemistryEngine)) as IChemistryEngine;
        _renderer ??= services.GetService(typeof(IMoleculeRenderer)) as IMoleculeRenderer;
        _cache ??= services.GetService(typeof(IDepictionCache)) as IDepictionCache;
    }

    private static void OnInputChanged(BindableObject bindable, object? oldValue, object? newValue)
        => ((MoleculeView)bindable).UpdateDepiction();

    private static void OnVisualChanged(BindableObject bindable, object? oldValue, object? newValue)
        => ((MoleculeView)bindable)._canvasView.InvalidateSurface();

    private static void OnInteractiveChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        var view = (MoleculeView)bindable;
        if (newValue is false)
            view.SetHovered(null);
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        _themeSource = Application.Current;
        if (_themeSource is not null)
            _themeSource.RequestedThemeChanged += OnThemeChanged;
        _canvasView.InvalidateSurface();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        if (_themeSource is not null)
            _themeSource.RequestedThemeChanged -= OnThemeChanged;
        _themeSource = null;
    }

    private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        if (RenderOptions is null)
            _canvasView.InvalidateSurface();
    }

    private void UpdateDepiction()
    {
        var version = ++_inputVersion;
        if (Depiction is { } depiction)
        {
            Apply(depiction, null);
            return;
        }

        var smiles = Smiles?.Trim();
        if (string.IsNullOrEmpty(smiles))
        {
            Apply(null, null);
            return;
        }

        var options = DepictionOptions ?? DepictionBuildOptions.Default;
        if (Cache.TryGet(smiles, options, out var cached))
        {
            Apply(cached, null);
            return;
        }

        if (_engine is not { } engine)
        {
            Apply(null, MissingEngineMessage);
            return;
        }

        var cache = Cache;
        _ = BuildAsync(version, () =>
        {
            var d = engine.Depict(smiles, options);
            cache.Set(smiles, options, d);
            return d;
        });
    }

    private async Task BuildAsync(int version, Func<MoleculeDepiction> work)
    {
        MoleculeDepiction? result = null;
        string? error = null;
        try
        {
            result = await Task.Run(work).ConfigureAwait(true);
        }
        catch (MoleculeParseException ex)
        {
            error = ex.Message;
        }
        catch (DepictionException ex)
        {
            error = ex.Message;
        }
#pragma warning disable CA1031 // the control must never throw into the UI
        catch (Exception ex)
        {
            error = $"Could not display the molecule: {ex.Message}";
        }
#pragma warning restore CA1031

        if (version != _inputVersion)
            return;
        if (Dispatcher.IsDispatchRequired)
            Dispatcher.Dispatch(() => ApplyIfCurrent(version, result, error));
        else
            Apply(result, error);
    }

    private void ApplyIfCurrent(int version, MoleculeDepiction? depiction, string? error)
    {
        if (version == _inputVersion)
            Apply(depiction, error);
    }

    private void Apply(MoleculeDepiction? depiction, string? error)
    {
        _current = depiction;
        _error = error;
        ErrorMessage = error;

        var count = depiction?.Atoms.Count ?? 0;
        if (SelectedAtomIndex is { } selected && selected >= count)
            SelectedAtomIndex = null;
        if (HoveredAtomIndex is { } hovered && hovered >= count)
            SetHovered(null);
        _canvasView.InvalidateSurface();
    }

    private MoleculeRenderOptions EffectiveOptions()
    {
        var options = RenderOptions ?? (Application.Current?.RequestedTheme == AppTheme.Dark ? MoleculeRenderOptions.Dark : MoleculeRenderOptions.Light);
        return ShowColorBar && !options.ShowColorBar ? options with { ShowColorBar = true } : options;
    }

    private MoleculeHighlights BuildHighlights() => new()
    {
        AtomWeights = AtomWeights,
        AtomColors = AtomColors ?? MoleculeHighlights.None.AtomColors,
        BondColors = BondColors ?? MoleculeHighlights.None.BondColors,
        Colormap = Colormap ?? Colormap.Viridis,
        SelectedAtom = SelectedAtomIndex,
        HoveredAtom = HoveredAtomIndex,
        RadiusScale = HighlightRadiusScale > 0 ? HighlightRadiusScale : 1f,
    };

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        var scale = _canvasView.Width > 0 ? (float)(e.Info.Width / _canvasView.Width) : 1f;
        if (scale <= 0 || float.IsNaN(scale) || float.IsInfinity(scale))
            scale = 1f;

        canvas.Save();
        canvas.Scale(scale);
        var bounds = new SKRect(0, 0, e.Info.Width / scale, e.Info.Height / scale);
        var options = EffectiveOptions();
        try
        {
            LastRender = _current is { Atoms.Count: > 0 } depiction
                ? Renderer.Render(canvas, bounds, depiction, options, BuildHighlights())
                : RenderedMolecule.Empty;
            if (_error is not null)
                DrawError(canvas, bounds, _error, options);
        }
#pragma warning disable CA1031 // never throw from the paint handler
        catch (Exception ex)
        {
            LastRender = RenderedMolecule.Empty;
            DrawError(canvas, bounds, $"Rendering failed: {ex.Message}", options);
        }
#pragma warning restore CA1031
        finally
        {
            canvas.Restore();
        }
    }

    private static void DrawError(SKCanvas canvas, SKRect bounds, string message, MoleculeRenderOptions options)
    {
        var typeface = options.Typeface ?? SKTypeface.Default;
        using var font = new SKFont(typeface, Math.Max(10, options.FontSize * 0.9f));
        using var paint = new SKPaint { IsAntialias = true, Color = options.ForegroundColor.WithAlpha(200) };
        var maxWidth = Math.Max(20, bounds.Width - (2 * options.Padding));
        var lines = Wrap(message, font, maxWidth).Take(4).ToList();
        var lineHeight = font.Spacing > 0 ? font.Spacing : font.Size * 1.2f;
        var y = bounds.MidY - ((lines.Count - 1) * lineHeight / 2) + (font.Size * 0.35f);
        foreach (var line in lines)
        {
            canvas.DrawText(line, bounds.MidX, y, SKTextAlign.Center, font, paint);
            y += lineHeight;
        }
    }

    private static IEnumerable<string> Wrap(string text, SKFont font, float maxWidth)
    {
        foreach (var paragraph in text.Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n'))
        {
            var current = string.Empty;
            foreach (var word in paragraph.Split(' '))
            {
                var candidate = current.Length == 0 ? word : current + " " + word;
                if (font.MeasureText(candidate) <= maxWidth || current.Length == 0)
                {
                    current = candidate;
                    continue;
                }

                yield return current;
                current = word;
            }

            if (current.Length > 0)
                yield return current;
        }
    }

    private void OnTapped(object? sender, TappedEventArgs e)
    {
        if (!IsInteractive || e.GetPosition(_canvasView) is not { } p)
            return;
        var hit = LastRender.HitTestAtom(new SKPoint((float)p.X, (float)p.Y), (float)HitTolerance);
        SelectedAtomIndex = hit is null || hit == SelectedAtomIndex ? null : hit;
        if (hit is { } index && AtomTappedCommand is { } command && command.CanExecute(index))
            command.Execute(index);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!IsInteractive || e.GetPosition(_canvasView) is not { } p)
            return;
        SetHovered(LastRender.HitTestAtom(new SKPoint((float)p.X, (float)p.Y), (float)HitTolerance));
    }

    private void OnPointerExited(object? sender, PointerEventArgs e) => SetHovered(null);

    private void SetHovered(int? index)
    {
        if (HoveredAtomIndex == index)
            return;
        HoveredAtomIndex = index;
        if (AtomHoveredCommand is { } command && command.CanExecute(index))
            command.Execute(index);
    }
}
