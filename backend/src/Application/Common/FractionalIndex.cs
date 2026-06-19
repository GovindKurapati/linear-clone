namespace LinearClone.Application.Common;

// Minimal fractional-index helper. Phase 1 only needs "append to end" (KeyAfter).
// Phase 2 adds KeyBetween(a, b) for drag-and-drop reordering between two neighbors.
//
// The idea: sort keys are strings compared lexicographically. To place an item
// between two others you generate a string that sorts between their keys, so you
// never have to renumber siblings — you just mint a new key.
//
// This is a deliberately simple base-26 (a–z) implementation. It's enough to learn
// the concept; a production system would use a library like LexoRank.
public static class FractionalIndex
{
    private const char MinChar = 'a';
    private const char MaxChar = 'z';

    // First key in an empty column.
    public static string First() => "n"; // middle of a-z, leaves room on both sides

    // A key that sorts strictly after `prev`. Used to append to the end of a column.
    public static string KeyAfter(string? prev)
    {
        if (string.IsNullOrEmpty(prev))
            return First();

        var last = prev[^1];
        if (last < MaxChar)
        {
            // Bump the last char up one: "n" -> "o"
            return prev[..^1] + (char)(last + 1);
        }

        // Last char is already 'z' — extend with a middle char: "z" -> "zn"
        return prev + First();
    }
}