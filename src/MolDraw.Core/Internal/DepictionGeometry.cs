using MolDraw.Depiction;

namespace MolDraw.Core.Internal;

/// <summary>Toolkit-independent 2D geometry helpers used while building depictions.</summary>
internal static class DepictionGeometry
{
    private const double Epsilon = 1e-6;

    /// <summary>Signed cross product z of (b - a) x (c - a). Positive: c is left of a→b (y up).</summary>
    public static double Cross(double ax, double ay, double bx, double by, double cx, double cy)
        => ((bx - ax) * (cy - ay)) - ((by - ay) * (cx - ax));

    /// <summary>Chooses the hydrogen label position from the directions of the neighbours (model coordinates, y up).</summary>
    public static HydrogenPosition HydrogenPositionFor(double x, double y, IReadOnlyList<(double X, double Y)> neighbours, string symbol)
    {
        if (neighbours.Count == 0)
        {
            // H2O, H2S, HCl ... are conventionally written with H first.
            return symbol is "O" or "S" or "Se" or "Te" or "F" or "Cl" or "Br" or "I" ? HydrogenPosition.Left : HydrogenPosition.Right;
        }

        bool right = false, left = false, up = false, down = false;
        foreach (var (nx, ny) in neighbours)
        {
            var dx = nx - x;
            var dy = ny - y;
            var len = Math.Sqrt((dx * dx) + (dy * dy));
            if (len < Epsilon)
                continue;
            dx /= len;
            dy /= len;
            if (dx > 0.25)
                right = true;
            if (dx < -0.25)
                left = true;
            if (dy > 0.25)
                up = true;
            if (dy < -0.25)
                down = true;
        }

        if (!right)
            return HydrogenPosition.Right;
        if (!left)
            return HydrogenPosition.Left;
        if (!down)
            return HydrogenPosition.Below;
        if (!up)
            return HydrogenPosition.Above;
        return HydrogenPosition.Right;
    }

    /// <summary>Placement of the second line of a double bond inside a ring: towards the ring centroid.</summary>
    public static DoubleBondPlacement PlacementTowards(double bx, double by, double ex, double ey, double tx, double ty)
    {
        var cross = Cross(bx, by, ex, ey, tx, ty);
        if (Math.Abs(cross) < Epsilon)
            return DoubleBondPlacement.Center;
        return cross > 0 ? DoubleBondPlacement.Left : DoubleBondPlacement.Right;
    }

    /// <summary>
    /// Placement of an acyclic double bond, based on the substituents of both ends (ACS style):
    /// terminal double bonds next to two substituents (e.g. ketones) are centred, otherwise the second line goes to the
    /// side with more substituents.
    /// </summary>
    public static DoubleBondPlacement AcyclicPlacement(
        (double X, double Y) begin,
        (double X, double Y) end,
        IReadOnlyList<(double X, double Y)> beginSubstituents,
        IReadOnlyList<(double X, double Y)> endSubstituents)
    {
        if (beginSubstituents.Count == 0 && endSubstituents.Count == 0)
            return DoubleBondPlacement.Center;

        // Terminal double bond (C=O, C=CH2): centred unless there is only one substituent on the other side.
        if (beginSubstituents.Count == 0 || endSubstituents.Count == 0)
        {
            var subs = beginSubstituents.Count == 0 ? endSubstituents : beginSubstituents;
            if (subs.Count != 1)
                return DoubleBondPlacement.Center;
        }

        var lenSq = ((end.X - begin.X) * (end.X - begin.X)) + ((end.Y - begin.Y) * (end.Y - begin.Y));
        int leftCount = 0, rightCount = 0, firstSide = 0;
        foreach (var s in beginSubstituents.Concat(endSubstituents))
        {
            var c = Cross(begin.X, begin.Y, end.X, end.Y, s.X, s.Y);
            var side = Math.Abs(c) < 0.1 * lenSq ? 0 : Math.Sign(c);
            if (side > 0)
                leftCount++;
            else if (side < 0)
                rightCount++;
            if (firstSide == 0)
                firstSide = side;
        }

        if (leftCount > rightCount)
            return DoubleBondPlacement.Left;
        if (rightCount > leftCount)
            return DoubleBondPlacement.Right;
        return firstSide switch
        {
            > 0 => DoubleBondPlacement.Left,
            < 0 => DoubleBondPlacement.Right,
            _ => DoubleBondPlacement.Center,
        };
    }

    /// <summary>
    /// Orientation of a tetrahedral centre from 3D-ish ligand positions:
    /// sign of the signed volume spanned by (l1-l0, l2-l0, l3-l0); 0 if degenerate.
    /// </summary>
    public static int Orientation(ReadOnlySpan<(double X, double Y, double Z)> l)
    {
        var (ax, ay, az) = (l[1].X - l[0].X, l[1].Y - l[0].Y, l[1].Z - l[0].Z);
        var (bx, by, bz) = (l[2].X - l[0].X, l[2].Y - l[0].Y, l[2].Z - l[0].Z);
        var (cx, cy, cz) = (l[3].X - l[0].X, l[3].Y - l[0].Y, l[3].Z - l[0].Z);
        var det = (ax * ((by * cz) - (bz * cy))) - (ay * ((bx * cz) - (bz * cx))) + (az * ((bx * cy) - (by * cx)));
        return Math.Abs(det) < 1e-12 ? 0 : Math.Sign(det);
    }
}
