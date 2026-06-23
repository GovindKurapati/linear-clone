namespace LinearClone.Application.Common;

// Sparse numeric ordering for board position. Items are ordered by a double.
// New items get a value in a large gap so inserting between two items is just
// averaging them — no renumbering of siblings. A column is rebalanced (clean
// spaced values reassigned) only if two neighbors ever get too close to halve,
// which in practice essentially never happens with human-paced editing.
public static class PositionHelper
{
    // The spacing between freshly-appended items. Large so there's room to insert.
    public const double Gap = 1024d;

    // If two neighbors are closer than this, the gap is exhausted and the column
    // should be rebalanced before inserting between them.
    public const double MinGap = 1e-6;

    // Append to the end of a column: just past the current max (or Gap if empty).
    public static double Append(double? currentMax)
        => currentMax is null ? Gap : currentMax.Value + Gap;

    // Insert at the top of a column: just before the current min (or Gap if empty).
    public static double Prepend(double? currentMin)
        => currentMin is null ? Gap : currentMin.Value - Gap;

    // Insert between two existing positions by averaging. Either bound may be null
    // meaning "open end" (top or bottom of the column).
    public static double Between(double? a, double? b)
    {
        if (a is null && b is null) return Gap;
        if (a is null) return Prepend(b);   // dropping above the first item
        if (b is null) return Append(a);    // dropping below the last item
        return (a.Value + b.Value) / 2d;
    }

    // True when the gap between two neighbors is too small to subdivide — the
    // caller should rebalance the column instead of inserting between them.
    public static bool NeedsRebalance(double a, double b) => Math.Abs(b - a) < MinGap;
}