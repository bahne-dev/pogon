using System.Collections.Generic;
using System.Linq;

using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

using IndustrialRevolution.util;

namespace IndustrialRevolution;

public partial class ItemConstructionGlue : Item
{
    private const int HighlightSlotId = 7410;  // random number

    private ModLogger? log = IndustrialRevolutionModSystem.Logger;

    // NOTE: server side only, keyed by player uid
    // In memory on purpose, an unfinished selection isn't worth saving
    private readonly Dictionary<string, GlueSelection> selections = new();

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        // without this crouch+right click on a chest etc would use the block
        this.HeldPriorityInteract = true;
    }

    public override void OnHeldInteractStart(
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel,
        bool firstEvent,
        ref EnumHandHandling handling
    )
    {
        // crouching confirms, so it works when aiming at nothing
        bool confirm = byEntity.Controls.ShiftKey;

        if (blockSel == null && !confirm)
        {
            handling = EnumHandHandling.NotHandled;
            return;
        }

        handling = EnumHandHandling.PreventDefault;

        // only handle on server, and once per click
        if (api.Side != EnumAppSide.Server || !firstEvent) return;

        IPlayer? player = (byEntity as EntityPlayer)?.Player;
        if (player == null)
        {
            handling = EnumHandHandling.NotHandled;
            return;
        }

        if (confirm)
        {
            this.Confirm(player, slot);
        }
        else
        {
            this.Edit(player, blockSel!.Position, GlueEdit.Add);
        }
    }

    public override void OnHeldAttackStart(
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel,
        ref EnumHandHandling handling
    )
    {
        bool doClear = byEntity.Controls.ShiftKey;

        if (blockSel == null && !doClear)
        {
            base.OnHeldAttackStart(
                slot, byEntity, blockSel, entitySel, ref handling
            );
            return;
        }

        // also stops the click from breaking the block
        handling = EnumHandHandling.PreventDefault;

        if (api.Side != EnumAppSide.Server) return;

        IPlayer? player = (byEntity as EntityPlayer)?.Player;
        if (player == null) return;

        // if shift held when left click, wipe whole selection
        if (doClear)
        {
            this.Selection(player).Clear();
            this.ShowSelection(player);
            this.Notify(player, "Selection cleared");
        }
        else
        {
            this.Edit(player, blockSel!.Position, GlueEdit.Remove);
        }
    }

    public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity,
        BlockSelection blockSel, EntitySelection entitySel)
    {
        return false;
    }

    public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity,
        BlockSelection blockSel, EntitySelection entitySel)
    {
        return;
    }

    private GlueSelection Selection(IPlayer player)
    {
        if (!this.selections.TryGetValue(player.PlayerUID, out var selection))
        {
            selection = new GlueSelection();
            this.selections[player.PlayerUID] = selection;
        }

        return selection;
    }

    private void Edit(IPlayer player, BlockPos pos, GlueEdit edit)
    {
        var selection = this.Selection(player);
        var ba = api.World.BlockAccessor;

        var result = selection.Click(
            (pos.X, pos.Y, pos.Z),
            edit,
            p => ba.GetBlock(new BlockPos(p.X, p.Y, p.Z)).Id != 0
        );

        this.ShowSelection(player);

        string verb = edit == GlueEdit.Add ? "add" : "remove";
        string max = $"{GlueSelection.MaxSpan}";

        this.Notify(player, result switch
        {
            GlueClickResult.CornerSet =>
                $"Corner set at {pos}, click the opposite corner to {verb}",
            GlueClickResult.TooLarge =>
                $"Selection too large! Max {max}x{max}x{max}",
            _ => $"Selected {selection.Blocks.Count} blocks",
        });
    }

    private void Confirm(IPlayer player, ItemSlot slot)
    {
        var selection = this.Selection(player);

        int glued = GlueRegion(
            api.World,
            selection.Blocks.Select(b => new BlockPos(b.X, b.Y, b.Z))
        );

        if (glued == 0)
        {
            this.Notify(player, "No blocks in selection!");
            return;
        }

        selection.Clear();
        this.ShowSelection(player);

        DamageItem(api.World, player.Entity, slot);

        this.Notify(player, $"Created construction with {glued} blocks");
    }

    private void ShowSelection(IPlayer player)
    {
        var selection = this.Selection(player);

        int selected = ColorUtil.ToRgba(90, 80, 220, 120);
        int corner =  ColorUtil.ToRgba(140, 240, 200, 60);

        var positions = selection.Blocks
            .Select(b => new BlockPos(b.X, b.Y, b.Z))
            .ToList();
        var colors = Enumerable.Repeat(selected, positions.Count).ToList();

        // if PendingCorner then we are halfway selected and blocks is empty
        if (selection.PendingCorner is (int x, int y, int z))
        {
            positions.Add(new BlockPos(x, y, z));
            colors.Add(corner);
        }

        api.World.HighlightBlocks(player, HighlightSlotId, positions, colors);
    }

    private void Notify(IPlayer player, string message)
    {
        (api as ICoreServerAPI)?.SendMessage(
            player, 0, message, EnumChatType.Notification
        );
    }
}
