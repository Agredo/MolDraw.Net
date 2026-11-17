using MolDraw.Core.Internal;
using MolDraw.Depiction;
using NCDK;
using NCDK.Aromaticities;
using NCDK.Graphs;
using NCDK.Layout;
using NCDK.Tools;
using NCDK.Tools.Manipulator;
using BondOrder = MolDraw.Depiction.BondOrder;
using NcdkBondOrder = NCDK.BondOrder;

namespace MolDraw.Core.Ncdk;

/// <summary>
/// NCDK based <see cref="IDepictionBuilder"/>: 2D layout (<c>StructureDiagramGenerator</c>) when coordinates are missing,
/// aromaticity and ring perception, ring-aware double bond placement, wedge/hash bonds, implicit hydrogen placement.
/// Stateless and thread-safe; works on a copy of the input.
/// </summary>
public sealed class DepictionBuilder : IDepictionBuilder
{
    /// <inheritdoc />
    public MoleculeDepiction Build(IAtomContainer molecule, DepictionBuildOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(molecule);
        options ??= DepictionBuildOptions.Default;
        if (molecule.Atoms.Count == 0)
            return MoleculeDepiction.Empty;

        IAtomContainer mol;
        try
        {
            mol = molecule.Clone();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            throw new DepictionException("The molecule could not be copied.", ex);
        }

        EnsureImplicitHydrogens(mol);

        var needsLayout = options.ForceLayout || NeedsLayout(mol);
        if (needsLayout)
        {
            try
            {
                new StructureDiagramGenerator().GenerateCoordinates(mol);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                throw new DepictionException($"2D layout failed: {ex.Message}", ex);
            }
        }

        Cycles.MarkRingAtomsAndBonds(mol);
        PerceiveAromaticity(mol, options);
        var rings = PerceiveRings(mol);

        Dictionary<int, (BondStereoDisplay Display, int NarrowEnd)>? assignedWedges = null;
        if (!needsLayout && options.AssignMissingWedges && !mol.Bonds.Any(HasWedgeDisplay))
            assignedWedges = WedgeAssigner.Assign(mol);

        var atoms = BuildAtoms(mol, options);
        var bonds = BuildBonds(mol, rings, assignedWedges);
        var depictionRings = rings.Select(r => new DepictionRing(r, IsAromaticRing(mol, r))).ToArray();
        return new MoleculeDepiction(atoms, bonds) { Rings = depictionRings };
    }

    private static bool NeedsLayout(IAtomContainer mol)
    {
        if (mol.Atoms.Any(a => a.Point2D is null))
            return true;
        if (mol.Atoms.Count < 2)
            return false;

        // all atoms on one point (e.g. molfiles with zero coordinates)
        var p0 = mol.Atoms[0].Point2D!.Value;
        return mol.Atoms.All(a => Math.Abs(a.Point2D!.Value.X - p0.X) < 1e-6 && Math.Abs(a.Point2D!.Value.Y - p0.Y) < 1e-6);
    }

    private static void EnsureImplicitHydrogens(IAtomContainer mol)
    {
        if (mol.Atoms.All(a => a.ImplicitHydrogenCount is not null))
            return;
        try
        {
            AtomContainerManipulator.PercieveAtomTypesAndConfigureUnsetProperties(mol);
            CDKHydrogenAdder.GetInstance().AddImplicitHydrogens(mol);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // fall back to "no implicit hydrogens" for atoms that could not be typed
        }

        foreach (var a in mol.Atoms)
            a.ImplicitHydrogenCount ??= 0;
    }

    private static void PerceiveAromaticity(IAtomContainer mol, DepictionBuildOptions options)
    {
        var hasFlags = mol.Atoms.Any(a => a.IsAromatic) || mol.Bonds.Any(b => b.IsAromatic);
        if (options.PerceiveAromaticity && !hasFlags)
        {
            try
            {
                var aromaticity = new Aromaticity(ElectronDonation.DaylightModel, Cycles.Or(Cycles.AllSimpleFinder, Cycles.RelevantFinder));
                aromaticity.Apply(mol);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                // aromaticity is cosmetic for depiction; keep Kekulé form
            }
        }

        // aromatic bonds without Kekulé orders (e.g. molfile bond type 4): try to assign a Kekulé structure
        if (mol.Bonds.Any(b => b.Order is NcdkBondOrder.Unset))
        {
            try
            {
                Kekulization.Kekulize(mol);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                // leave unset; rendered as aromatic
            }
        }
    }

    private static List<int[]> PerceiveRings(IAtomContainer mol)
    {
        var rings = new List<int[]>();
        try
        {
            foreach (var path in Cycles.FindSSSR(mol).GetPaths())
            {
                // NCDK paths are closed (first == last)
                var n = path.Length > 1 && path[0] == path[^1] ? path.Length - 1 : path.Length;
                if (n >= 3)
                    rings.Add(path[..n]);
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // rings are optional (aromatic circles, double bond placement fall back to acyclic rules)
        }

        return rings;
    }

    private static bool IsAromaticRing(IAtomContainer mol, int[] ring)
    {
        for (var i = 0; i < ring.Length; i++)
        {
            var b = mol.GetBond(mol.Atoms[ring[i]], mol.Atoms[ring[(i + 1) % ring.Length]]);
            if (b is null || !b.IsAromatic)
                return false;
        }

        return true;
    }

    private static DepictionAtom[] BuildAtoms(IAtomContainer mol, DepictionBuildOptions options)
    {
        var atoms = new DepictionAtom[mol.Atoms.Count];
        for (var i = 0; i < atoms.Length; i++)
        {
            var atom = mol.Atoms[i];
            var p = atom.Point2D!.Value;
            var neighbours = mol.GetConnectedAtoms(atom).Select(n => (n.Point2D!.Value.X, n.Point2D!.Value.Y)).ToList();
            var symbol = atom is IPseudoAtom pseudo && !string.IsNullOrEmpty(pseudo.Label) ? pseudo.Label : atom.Symbol ?? "*";
            var charge = atom.FormalCharge ?? 0;
            var radicals = mol.GetConnectedSingleElectrons(atom).Count();
            var isCarbon = atom.AtomicNumber == 6 && atom is not IPseudoAtom;
            var showLabel = !isCarbon
                || charge != 0
                || atom.MassNumber is not null
                || radicals > 0
                || neighbours.Count == 0
                || options.CarbonLabels == CarbonLabelMode.All
                || (options.CarbonLabels == CarbonLabelMode.Terminal && neighbours.Count == 1);

            atoms[i] = new DepictionAtom(
                i,
                symbol,
                p.X,
                p.Y,
                atom.ImplicitHydrogenCount ?? 0,
                charge,
                atom.MassNumber,
                showLabel,
                DepictionGeometry.HydrogenPositionFor(p.X, p.Y, neighbours, symbol))
            {
                AtomicNumber = atom.AtomicNumber,
                IsAromatic = atom.IsAromatic,
                Radicals = radicals,
            };
        }

        return atoms;
    }

    private static DepictionBond[] BuildBonds(
        IAtomContainer mol,
        List<int[]> rings,
        Dictionary<int, (BondStereoDisplay Display, int NarrowEnd)>? assignedWedges)
    {
        var bonds = new DepictionBond[mol.Bonds.Count];
        for (var i = 0; i < bonds.Length; i++)
        {
            var bond = mol.Bonds[i];
            var begin = bond.Begin.Index;
            var end = bond.End.Index;

            var kekule = bond.Order switch
            {
                NcdkBondOrder.Single => BondOrder.Single,
                NcdkBondOrder.Double => BondOrder.Double,
                NcdkBondOrder.Triple or NcdkBondOrder.Quadruple => BondOrder.Triple,
                _ => bond.IsAromatic ? BondOrder.Aromatic : BondOrder.Single,
            };
            var order = bond.IsAromatic ? BondOrder.Aromatic : kekule;

            var (stereo, swap) = MapStereo(bond.Stereo);
            if (assignedWedges is not null && assignedWedges.TryGetValue(i, out var w))
            {
                stereo = w.Display;
                swap = w.NarrowEnd != bond.Begin.Index;
            }

            if (swap)
                (begin, end) = (end, begin);

            var placement = kekule is BondOrder.Double || order is BondOrder.Double
                ? DoubleBondPlacementFor(mol, begin, end, bond.IsInRing, rings)
                : DoubleBondPlacement.Center;

            bonds[i] = new DepictionBond(begin, end, order, stereo, placement)
            {
                Index = i,
                KekuleOrder = kekule,
                IsInRing = bond.IsInRing,
            };
        }

        return bonds;
    }

    private static bool HasWedgeDisplay(IBond b) => MapStereo(b.Stereo).Display != BondStereoDisplay.None;

    private static (BondStereoDisplay Display, bool Swap) MapStereo(BondStereo stereo) => stereo switch
    {
        BondStereo.Up => (BondStereoDisplay.Wedge, false),
        BondStereo.UpInverted => (BondStereoDisplay.Wedge, true),
        BondStereo.Down => (BondStereoDisplay.Hash, false),
        BondStereo.DownInverted => (BondStereoDisplay.Hash, true),
        BondStereo.UpOrDown => (BondStereoDisplay.Wavy, false),
        BondStereo.UpOrDownInverted => (BondStereoDisplay.Wavy, true),
        _ => (BondStereoDisplay.None, false),
    };

    private static DoubleBondPlacement DoubleBondPlacementFor(IAtomContainer mol, int begin, int end, bool inRing, List<int[]> rings)
    {
        var a = mol.Atoms[begin].Point2D!.Value;
        var b = mol.Atoms[end].Point2D!.Value;

        if (inRing)
        {
            int[]? best = null;
            var bestScore = double.MaxValue;
            foreach (var ring in rings)
            {
                if (!ContainsEdge(ring, begin, end))
                    continue;

                // prefer rings with more double bonds, then 6-rings, then smaller rings
                var doubles = CountDoubleBonds(mol, ring);
                var score = (-doubles * 100) + (ring.Length == 6 ? 0 : 10) + ring.Length;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = ring;
                }
            }

            if (best is not null)
            {
                double cx = 0, cy = 0;
                foreach (var idx in best)
                {
                    cx += mol.Atoms[idx].Point2D!.Value.X;
                    cy += mol.Atoms[idx].Point2D!.Value.Y;
                }

                return DepictionGeometry.PlacementTowards(a.X, a.Y, b.X, b.Y, cx / best.Length, cy / best.Length);
            }
        }

        // cumulated double bonds (allenes, CO2) are centred
        if (IsCumulated(mol, begin) || IsCumulated(mol, end))
            return DoubleBondPlacement.Center;

        var beginSubs = Substituents(mol, begin, end);
        var endSubs = Substituents(mol, end, begin);
        return DepictionGeometry.AcyclicPlacement((a.X, a.Y), (b.X, b.Y), beginSubs, endSubs);
    }

    private static List<(double X, double Y)> Substituents(IAtomContainer mol, int atom, int exclude)
        => mol.GetConnectedAtoms(mol.Atoms[atom])
            .Where(n => n.Index != exclude)
            .Select(n => (n.Point2D!.Value.X, n.Point2D!.Value.Y))
            .ToList();

    private static bool IsCumulated(IAtomContainer mol, int atom)
        => mol.GetConnectedBonds(mol.Atoms[atom]).Count(b => b.Order == NcdkBondOrder.Double) > 1;

    private static int CountDoubleBonds(IAtomContainer mol, int[] ring)
    {
        var count = 0;
        for (var i = 0; i < ring.Length; i++)
        {
            var bond = mol.GetBond(mol.Atoms[ring[i]], mol.Atoms[ring[(i + 1) % ring.Length]]);
            if (bond?.Order == NcdkBondOrder.Double)
                count++;
        }

        return count;
    }

    private static bool ContainsEdge(int[] ring, int u, int v)
    {
        for (var i = 0; i < ring.Length; i++)
        {
            var a = ring[i];
            var b = ring[(i + 1) % ring.Length];
            if ((a == u && b == v) || (a == v && b == u))
                return true;
        }

        return false;
    }
}

