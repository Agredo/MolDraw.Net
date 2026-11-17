using MolDraw.Depiction;
using NCDK;

namespace MolDraw.Core.Internal;

/// <summary>
/// Minimal wedge assignment for molecules that already have 2D coordinates and tetrahedral stereo elements but no
/// wedge/hash bonds (NCDK's own <c>NonplanarBonds</c> is internal and only runs during coordinate generation).
/// One bond per stereocentre is wedged; the type (wedge/hash) is chosen so that the 2D geometry reproduces the
/// stored configuration.
/// </summary>
internal static class WedgeAssigner
{
    /// <summary>
    /// Orientation sign (see <see cref="DepictionGeometry.Orientation"/>) of <see cref="TetrahedralStereo.Clockwise"/>
    /// ligands. Verified against NCDK's own wedge assignment in the tests.
    /// </summary>
    internal const int ClockwiseOrientation = 1;

    /// <summary>Computes the orientation if <paramref name="raised"/> is lifted by <paramref name="z"/> out of the plane.</summary>
    internal static int OrientationWithRaised(IAtom focus, IReadOnlyList<IAtom> ligands, IAtom raised, double z)
    {
        Span<(double X, double Y, double Z)> pts = stackalloc (double, double, double)[4];
        var f = focus.Point2D!.Value;
        for (var i = 0; i < 4; i++)
        {
            var l = ligands[i];
            if (SameAtom(l, focus))
            {
                // implicit hydrogen: points opposite to the raised ligand
                pts[i] = (f.X, f.Y, -z);
            }
            else
            {
                var p = l.Point2D!.Value;
                pts[i] = (p.X, p.Y, SameAtom(l, raised) ? z : 0);
            }
        }

        return DepictionGeometry.Orientation(pts);
    }

    /// <summary>NCDK wraps atoms in container-specific references (no reference equality); all atoms here belong to one molecule, so compare by index.</summary>
    internal static bool SameAtom(IAtom a, IAtom b)
        => ReferenceEquals(a, b) || (a.Index >= 0 && a.Index == b.Index);

    /// <summary>Expected orientation for a configuration.</summary>
    internal static int ExpectedOrientation(TetrahedralStereo stereo)
        => stereo == TetrahedralStereo.Clockwise ? ClockwiseOrientation : -ClockwiseOrientation;

    /// <summary>
    /// Assigns wedges. Result maps bond index → display and the atom index of the narrow end (the stereocentre).
    /// </summary>
    public static Dictionary<int, (BondStereoDisplay Display, int NarrowEnd)> Assign(IAtomContainer molecule)
    {
        var result = new Dictionary<int, (BondStereoDisplay, int)>();
        var centres = molecule.StereoElements.OfType<ITetrahedralChirality>().ToList();
        if (centres.Count == 0)
            return result;
        var centreAtoms = new HashSet<int>(centres.Select(c => c.ChiralAtom.Index));

        foreach (var tc in centres)
        {
            var focus = tc.ChiralAtom;
            var ligands = tc.Ligands;
            if (ligands.Count != 4 || tc.Stereo is not (TetrahedralStereo.Clockwise or TetrahedralStereo.AntiClockwise))
                continue;
            if (focus.Point2D is null || ligands.Any(l => l.Point2D is null))
                continue;

            IBond? best = null;
            IAtom? bestLigand = null;
            var bestScore = int.MaxValue;
            foreach (var l in ligands)
            {
                if (SameAtom(l, focus))
                    continue;
                var bond = molecule.GetBond(focus, l);
                if (bond is null || bond.Order != NCDK.BondOrder.Single || result.ContainsKey(bond.Index))
                    continue;
                var score = (bond.IsInRing ? 100 : 0) + (centreAtoms.Contains(l.Index) ? 50 : 0) + (l.AtomicNumber == 1 ? 0 : 5) + molecule.GetConnectedBonds(l).Count();
                if (score < bestScore)
                {
                    bestScore = score;
                    best = bond;
                    bestLigand = l;
                }
            }

            if (best is null || bestLigand is null)
                continue;
            var orientation = OrientationWithRaised(focus, ligands, bestLigand, 1.0);
            if (orientation == 0)
                continue;
            var display = orientation == ExpectedOrientation(tc.Stereo) ? BondStereoDisplay.Wedge : BondStereoDisplay.Hash;
            result[best.Index] = (display, focus.Index);
        }

        return result;
    }
}



