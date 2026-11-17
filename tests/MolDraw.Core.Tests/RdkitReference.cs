namespace MolDraw.Core.Tests;

/// <summary>
/// Reference data generated with RDKit 2025.09.3 <c>Chem.MolFromSmiles</c> (default sanitisation, explicit H removal):
/// atom symbols, formal charges, isotopes and total H counts in RDKit atom-index order.
/// </summary>
internal static class RdkitReference
{
    public static readonly (string Smiles, string[] Symbols, int[] Charges, int?[] Isotopes, int[] Hydrogens)[] Cases =
    [
        (@"CN1C=NC2=C1C(=O)N(C(=O)N2C)C", ["C", "N", "C", "N", "C", "C", "C", "O", "N", "C", "O", "N", "C", "C"], [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0], [null, null, null, null, null, null, null, null, null, null, null, null, null, null], [3, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 3]),
        (@"CC(=O)OC1=CC=CC=C1C(=O)O", ["C", "C", "O", "O", "C", "C", "C", "C", "C", "C", "C", "O", "O"], [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0], [null, null, null, null, null, null, null, null, null, null, null, null, null], [3, 0, 0, 0, 0, 1, 1, 1, 1, 0, 0, 0, 1]),
        (@"OC[C@H]1OC(O)[C@H](O)[C@@H](O)[C@@H]1O", ["O", "C", "C", "O", "C", "O", "C", "O", "C", "O", "C", "O"], [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0], [null, null, null, null, null, null, null, null, null, null, null, null], [1, 2, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1]),
        (@"C[C@H](CCCC(C)C)[C@H]1CC[C@@H]2[C@@]1(CC[C@H]3[C@H]2CC=C4[C@@]3(CC[C@@H](C4)O)C)C", ["C", "C", "C", "C", "C", "C", "C", "C", "C", "C", "C", "C", "C", "C", "C", "C", "C", "C", "C", "C", "C", "C", "C", "C", "C", "O", "C", "C"], [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0], [null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null], [3, 1, 2, 2, 2, 1, 3, 3, 1, 2, 2, 1, 0, 2, 2, 1, 1, 2, 1, 0, 0, 2, 2, 1, 2, 1, 3, 3]),
        (@"c1ccccc1", ["C", "C", "C", "C", "C", "C"], [0, 0, 0, 0, 0, 0], [null, null, null, null, null, null], [1, 1, 1, 1, 1, 1]),
        (@"[13CH3][N+](C)(C)CC([O-])=O", ["C", "N", "C", "C", "C", "C", "O", "O"], [0, 1, 0, 0, 0, 0, -1, 0], [13, null, null, null, null, null, null, null], [3, 0, 3, 3, 2, 0, 0, 0]),
        (@"[H]OC(=O)c1ccccc1[C@@H](N)[13CH3]", ["O", "C", "O", "C", "C", "C", "C", "C", "C", "C", "N", "C"], [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0], [null, null, null, null, null, null, null, null, null, null, null, 13], [1, 0, 0, 0, 1, 1, 1, 1, 0, 1, 2, 3]),
        (@"[2H]C([2H])([2H])O", ["H", "C", "H", "H", "O"], [0, 0, 0, 0, 0], [2, null, 2, 2, null], [0, 0, 0, 0, 1]),
        (@"C[C@@H](C(=O)O)N", ["C", "C", "C", "O", "O", "N"], [0, 0, 0, 0, 0, 0], [null, null, null, null, null, null], [3, 1, 0, 0, 1, 2]),
        (@"OC(=O)C1=CC=CC=N1.Cl", ["O", "C", "O", "C", "C", "C", "C", "C", "N", "Cl"], [0, 0, 0, 0, 0, 0, 0, 0, 0, 0], [null, null, null, null, null, null, null, null, null, null], [1, 0, 0, 0, 1, 1, 1, 1, 0, 1]),
        (@"Brc1ccc(Cl)cc1[N+](=O)[O-]", ["Br", "C", "C", "C", "C", "Cl", "C", "C", "N", "O", "O"], [0, 0, 0, 0, 0, 0, 0, 0, 1, 0, -1], [null, null, null, null, null, null, null, null, null, null, null], [0, 0, 1, 1, 0, 0, 1, 0, 0, 0, 0]),
        (@"[H][H]", ["H", "H"], [0, 0], [null, null], [0, 0]),
        (@"C1CC1[Na]", ["C", "C", "C", "Na"], [0, 0, 0, 0], [null, null, null, null], [2, 2, 1, 0]),
        (@"c1ccc2[nH]ccc2c1", ["C", "C", "C", "C", "N", "C", "C", "C", "C"], [0, 0, 0, 0, 0, 0, 0, 0, 0], [null, null, null, null, null, null, null, null, null], [1, 1, 1, 0, 1, 1, 1, 0, 1]),
        (@"F/C=C/F", ["F", "C", "C", "F"], [0, 0, 0, 0], [null, null, null, null], [0, 1, 1, 0]),
    ];

    public static TheoryData<int> Indices()
    {
        var data = new TheoryData<int>();
        for (var i = 0; i < Cases.Length; i++)
            data.Add(i);
        return data;
    }
}
