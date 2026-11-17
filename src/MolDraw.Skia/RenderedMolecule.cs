using SkiaSharp;

namespace MolDraw.Skia;

/// <summary>
/// Screen-space result of a render pass: the model→canvas transform and atom/bond positions for hit testing and overlays.
/// Immutable and safe to use from any thread.
/// </summary>
public sealed class RenderedMolecule
{
    private readonly SKPoint[] _atoms;
    private readonly (int Begin, int End)[] _bonds;

    internal RenderedMolecule(SKMatrix transform, SKPoint[] atomPositions, (int Begin, int End)[] bonds, float atomRadius, float bondLength, float bondHitWidth, SKRect bounds)
    {
        Transform = transform;
        _atoms = atomPositions;
        _bonds = bonds;
        AtomRadius = atomRadius;
        BondLength = bondLength;
        BondHitWidth = bondHitWidth;
        Bounds = bounds;
    }

    /// <summary>Empty result (no atoms).</summary>
    public static RenderedMolecule Empty { get; } = new(SKMatrix.Identity, [], [], 0, 0, 0, SKRect.Empty);

    /// <summary>Model → canvas transform (includes the y flip).</summary>
    public SKMatrix Transform { get; }

    /// <summary>Canvas positions of the atoms, by atom index.</summary>
    public IReadOnlyList<SKPoint> AtomPositions => _atoms;

    /// <summary>Canvas positions of the bond end points (begin, end), by bond index.</summary>
    public IReadOnlyList<(SKPoint Begin, SKPoint End)> BondSegments => _bonds.Select(b => (_atoms[b.Begin], _atoms[b.End])).ToArray();

    /// <summary>Hit radius of atoms in canvas pixels (= highlight disc radius).</summary>
    public float AtomRadius { get; }

    /// <summary>Median bond length in canvas pixels.</summary>
    public float BondLength { get; }

    /// <summary>Width of the bond hit corridor in canvas pixels.</summary>
    public float BondHitWidth { get; }

    /// <summary>Canvas bounds that were used for layout.</summary>
    public SKRect Bounds { get; }

    /// <summary>Maps a model coordinate to the canvas.</summary>
    /// <param name="x">Model x.</param>
    /// <param name="y">Model y.</param>
    public SKPoint ModelToCanvas(double x, double y) => Transform.MapPoint((float)x, (float)y);

    /// <summary>Maps a canvas point to model coordinates.</summary>
    /// <param name="canvasPoint">Canvas point.</param>
    public SKPoint CanvasToModel(SKPoint canvasPoint)
        => Transform.TryInvert(out var inverse) ? inverse.MapPoint(canvasPoint) : canvasPoint;

    /// <summary>Returns the index of the closest atom within <see cref="AtomRadius"/> + <paramref name="tolerance"/>, or <see langword="null"/>.</summary>
    /// <param name="canvasPoint">Point in canvas coordinates.</param>
    /// <param name="tolerance">Additional tolerance in pixels (e.g. for touch input).</param>
    public int? HitTestAtom(SKPoint canvasPoint, float tolerance = 0)
    {
        var limit = AtomRadius + Math.Max(0, tolerance);
        var limitSq = limit * limit;
        int? best = null;
        var bestSq = float.MaxValue;
        for (var i = 0; i < _atoms.Length; i++)
        {
            var dx = _atoms[i].X - canvasPoint.X;
            var dy = _atoms[i].Y - canvasPoint.Y;
            var d = (dx * dx) + (dy * dy);
            if (d <= limitSq && d < bestSq)
            {
                bestSq = d;
                best = i;
            }
        }

        return best;
    }

    /// <summary>
    /// Returns the index of the closest bond whose segment is within half <see cref="BondHitWidth"/> + <paramref name="tolerance"/>,
    /// or <see langword="null"/>. Points on an atom (see <see cref="HitTestAtom"/>) are not considered bond hits.
    /// </summary>
    /// <param name="canvasPoint">Point in canvas coordinates.</param>
    /// <param name="tolerance">Additional tolerance in pixels.</param>
    public int? HitTestBond(SKPoint canvasPoint, float tolerance = 0)
    {
        if (HitTestAtom(canvasPoint) is not null)
            return null;
        var limit = (BondHitWidth / 2) + Math.Max(0, tolerance);
        int? best = null;
        var bestDist = float.MaxValue;
        for (var i = 0; i < _bonds.Length; i++)
        {
            var d = DistanceToSegment(canvasPoint, _atoms[_bonds[i].Begin], _atoms[_bonds[i].End]);
            if (d <= limit && d < bestDist)
            {
                bestDist = d;
                best = i;
            }
        }

        return best;
    }

    internal static float DistanceToSegment(SKPoint p, SKPoint a, SKPoint b)
    {
        var abx = b.X - a.X;
        var aby = b.Y - a.Y;
        var lenSq = (abx * abx) + (aby * aby);
        var t = lenSq <= float.Epsilon ? 0 : Math.Clamp((((p.X - a.X) * abx) + ((p.Y - a.Y) * aby)) / lenSq, 0, 1);
        var cx = a.X + (t * abx) - p.X;
        var cy = a.Y + (t * aby) - p.Y;
        return MathF.Sqrt((cx * cx) + (cy * cy));
    }
}
