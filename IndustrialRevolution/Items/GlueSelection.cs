using System;
using System.Collections.Generic;
using System.Linq;

namespace IndustrialRevolution;

public enum GlueEdit
{
    Add,
    Remove,
}

public enum GlueClickResult
{
    CornerSet,
    Applied,
    TooLarge,
}

/// The set of selected blocks for the gluing (not glued yet)
/// Doesn't depend on any game stuff so it can be unit tested
public class GlueSelection
{
    public const int MaxSpan = 16;

    private readonly HashSet<(int X, int Y, int Z)> selectedBlocks = new();
    private GlueEdit pendingEdit;

    public (int X, int Y, int Z)? PendingCorner { get; private set; }
    public IReadOnlyCollection<(int X, int Y, int Z)> Blocks =>
        this.selectedBlocks;

    /// first click marks a corner, second one applies the edit to the cuboid
    /// switching between edit modes mid edit cancels the previous one
    public GlueClickResult Click(
        (int X, int Y, int Z) pos,
        GlueEdit edit,
        Func<(int X, int Y, int Z), bool> isSolid
    )
    {
        // if start of selection or edit changed, treat as first half of edit
        if (this.PendingCorner == null || this.pendingEdit != edit)
        {
            this.PendingCorner = pos;
            this.pendingEdit = edit;
            return GlueClickResult.CornerSet;
        }
        // everything after this is second half of edit

        var corner = this.PendingCorner.Value;
        this.PendingCorner = null;

        var min = (
            X: Math.Min(corner.X, pos.X),
            Y: Math.Min(corner.Y, pos.Y),
            Z: Math.Min(corner.Z, pos.Z)
        );
        var max = (
            X: Math.Max(corner.X, pos.X),
            Y: Math.Max(corner.Y, pos.Y),
            Z: Math.Max(corner.Z, pos.Z)
        );

        if (edit == GlueEdit.Remove)
        {
            this.selectedBlocks.RemoveWhere(b =>
                b.X >= min.X && b.X <= max.X &&
                b.Y >= min.Y && b.Y <= max.Y &&
                b.Z >= min.Z && b.Z <= max.Z
            );
            return GlueClickResult.Applied;
        }

        // check size before walking the box as corners can be arbitrarily far
        if (!IsWithinMaxSpan(this.selectedBlocks.Append(min).Append(max)))
        {
            return GlueClickResult.TooLarge;
        }

        for (int x = min.X; x <= max.X; x++)
        {
            for (int y = min.Y; y <= max.Y; y++)
            {
                for (int z = min.Z; z <= max.Z; z++)
                {
                    if (isSolid((x, y, z))) this.selectedBlocks.Add((x, y, z));
                }
            }
        }

        return GlueClickResult.Applied;
    }

    public void Clear()
    {
        this.selectedBlocks.Clear();
        this.PendingCorner = null;
    }

    // checks the length of the bounding box on each axis
    private static bool IsWithinMaxSpan(
        IEnumerable<(int X, int Y, int Z)> points
    ) =>
        points.Max(p => p.X) - points.Min(p => p.X) < MaxSpan &&
        points.Max(p => p.Y) - points.Min(p => p.Y) < MaxSpan &&
        points.Max(p => p.Z) - points.Min(p => p.Z) < MaxSpan;
}
