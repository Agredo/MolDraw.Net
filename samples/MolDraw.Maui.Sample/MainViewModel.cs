using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MolDraw.Core;
using MolDraw.Core.Ncdk;
using MolDraw.Skia;

namespace MolDraw.Maui.Sample;

public sealed record MoleculeItem(string Name, string Smiles);

public partial class MainViewModel : ObservableObject
{
    private readonly IMoleculeLoader _loader;
    private string[] _symbols = [];

    public MainViewModel(IMoleculeLoader loader)
    {
        _loader = loader;
        SelectedMolecule = Molecules[0];
    }

    public IReadOnlyList<MoleculeItem> Molecules { get; } =
    [
        new("Caffeine", "CN1C=NC2=C1C(=O)N(C(=O)N2C)C"),
        new("Aspirin", "CC(=O)OC1=CC=CC=C1C(=O)O"),
        new("Glucose", "OC[C@H]1OC(O)[C@H](O)[C@@H](O)[C@@H]1O"),
        new("Cholesterol", "C[C@H](CCCC(C)C)[C@H]1CC[C@@H]2[C@@]1(CC[C@H]3[C@H]2CC=C4[C@@]3(CC[C@@H](C4)O)C)C"),
        new("Benzene", "c1ccccc1"),
        new("Charged / isotope", "[13CH3][N+](C)(C)CC([O-])=O"),
        new("Stereo (L-alanine)", "C[C@@H](C(=O)O)N"),
    ];

    public IReadOnlyList<Colormap> Colormaps => Colormap.BuiltIn;

    public IReadOnlyList<string> HighlightModes { get; } = ["None", "Gradient", "Random"];

    [ObservableProperty]
    private MoleculeItem? selectedMolecule;

    [ObservableProperty]
    private string smiles = string.Empty;

    /// <summary>Pushed by MoleculeView (OneWayToSource).</summary>
    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RenderOptions), nameof(Background))]
    private bool isDark;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RenderOptions))]
    private double bondWidth = 1.6;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RenderOptions))]
    private double fontSize = 14;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RenderOptions))]
    private bool aromaticCircles;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RenderOptions))]
    private bool showAtomIndices;

    [ObservableProperty]
    private string highlightMode = "Gradient";

    [ObservableProperty]
    private Colormap colormap = Colormap.Viridis;

    [ObservableProperty]
    private bool showColorBar = true;

    [ObservableProperty]
    private IReadOnlyList<float>? atomWeights;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AtomInfo))]
    private int? selectedAtomIndex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AtomInfo))]
    private int? hoveredAtomIndex;

    [ObservableProperty]
    private string lastTapped = "Tap an atom";

    public MoleculeRenderOptions RenderOptions => (IsDark ? MoleculeRenderOptions.Dark : MoleculeRenderOptions.Light) with
    {
        BondWidth = (float)BondWidth,
        FontSize = (float)FontSize,
        AromaticStyle = AromaticCircles ? AromaticStyle.Circle : AromaticStyle.Kekule,
        ShowAtomIndices = ShowAtomIndices,
    };

    public Color Background => IsDark ? Color.FromArgb("#1E1E1E") : Colors.White;

    public string AtomInfo => $"Selected: {Describe(SelectedAtomIndex)}   Hovered: {Describe(HoveredAtomIndex)}";

    partial void OnSelectedMoleculeChanged(MoleculeItem? value)
    {
        if (value is not null)
            Smiles = value.Smiles;
    }

    partial void OnSmilesChanged(string value)
    {
        // atom symbols/count for the info line and the weight demo; the view parses independently (cached)
        _symbols = _loader.TryParseSmiles(value, out var mol, out _) && mol is not null
            ? mol.Atoms.Select(a => a.Symbol).ToArray()
            : [];
        UpdateWeights();
    }

    partial void OnHighlightModeChanged(string value) => UpdateWeights();

    [RelayCommand]
    private void Randomize()
    {
        if (HighlightMode == "Random")
            UpdateWeights();
        else
            HighlightMode = "Random";
    }

    [RelayCommand]
    private void AtomTapped(int index) => LastTapped = $"Tapped atom {index} ({Symbol(index)})";

    private void UpdateWeights()
    {
        var n = _symbols.Length;
        AtomWeights = HighlightMode switch
        {
            "Gradient" => Enumerable.Range(0, n).Select(i => n > 1 ? (float)i / (n - 1) : 1f).ToArray(),
            "Random" => Enumerable.Range(0, n).Select(_ => Random.Shared.NextSingle()).ToArray(),
            _ => null,
        };
        OnPropertyChanged(nameof(AtomInfo));
    }

    private string Symbol(int index) => index >= 0 && index < _symbols.Length ? _symbols[index] : "?";

    private string Describe(int? index)
    {
        if (index is not { } i)
            return "–";
        var weight = AtomWeights is { } w && i < w.Count ? $", w={w[i]:0.00}" : string.Empty;
        return $"{i} ({Symbol(i)}{weight})";
    }
}


