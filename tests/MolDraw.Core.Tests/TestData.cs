using System.Globalization;
using System.Text;

namespace MolDraw.Core.Tests;

internal static class TestData
{
    public static string EthanolMolV2000 { get; } = Mol(
        [("C", 0.0, 0.0), ("C", 1.299, 0.75), ("O", 2.598, 0.0)],
        [(1, 2, 1), (2, 3, 1)]);

    public static string MethanolWithHydrogensMol { get; } = Mol(
        [("C", 0, 0), ("O", 1.5, 0), ("H", -0.5, 0.9), ("H", -0.5, -0.9), ("H", -1, 0), ("H", 2, 0.9)],
        [(1, 2, 1), (1, 3, 1), (1, 4, 1), (1, 5, 1), (2, 6, 1)]);

    public static Stream Stream(string text) => new MemoryStream(Encoding.UTF8.GetBytes(text));

    public static string Mol(IReadOnlyList<(string Symbol, double X, double Y)> atoms, IReadOnlyList<(int A, int B, int Order)> bonds)
    {
        var sb = new StringBuilder();
        sb.Append("test\n  MolDraw\n\n");
        sb.Append(CultureInfo.InvariantCulture, $"{atoms.Count,3}{bonds.Count,3}  0  0  0  0  0  0  0  0999 V2000\n");
        foreach (var (symbol, x, y) in atoms)
            sb.Append(CultureInfo.InvariantCulture, $"{x,10:F4}{y,10:F4}{0.0,10:F4} {symbol,-3} 0  0  0  0  0  0  0  0  0  0  0  0\n");
        foreach (var (a, b, order) in bonds)
            sb.Append(CultureInfo.InvariantCulture, $"{a,3}{b,3}{order,3}  0  0  0  0\n");
        sb.Append("M  END\n");
        return sb.ToString();
    }
}
