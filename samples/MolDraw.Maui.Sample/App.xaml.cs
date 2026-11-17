namespace MolDraw.Maui.Sample;

public partial class App : Application
{
    private readonly MainPage _page;

    public App(MainPage page)
    {
        InitializeComponent();
        _page = page;
    }

    protected override Window CreateWindow(IActivationState? activationState)
        => new(_page) { Title = "MolDraw.Net Sample", Width = 1100, Height = 720 };
}
