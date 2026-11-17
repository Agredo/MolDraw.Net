using MolDraw.Core;

namespace MolDraw.Maui.Sample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMolDraw();

        // The chemistry engine: MolDraw.Maui itself contains no chemistry toolkit.
        builder.Services.AddMolDrawCore();

        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainPage>();

        return builder.Build();
    }
}

