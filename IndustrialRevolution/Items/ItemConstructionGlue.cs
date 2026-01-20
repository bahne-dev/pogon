using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Datastructures;

using IndustrialRevolution.util;

namespace IndustrialRevolution;

public partial class ItemConstructionGlue : Item
{
    private const int MaxSelectionSize = 16;
    private const string SelectionDataKey = "mechanism_selection";
    private ModLogger? log = IndustrialRevolutionModSystem.Logger;

    public override void OnHeldInteractStart(
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel,
        bool firstEvent,
        ref EnumHandHandling handling
    )
    {
        if (blockSel == null)
        {
            handling = EnumHandHandling.NotHandled;
            return;
        }

        if (api.Side != EnumAppSide.Server)
        {
            handling = EnumHandHandling.PreventDefault;
            return;
        } // only handle on server

        IPlayer? player = (byEntity as EntityPlayer)?.Player;
        if (player == null)
        {
            handling = EnumHandHandling.NotHandled;
            return;
        }

        ITreeAttribute? selectionData = player
            .Entity
            .WatchedAttributes
            .GetTreeAttribute(SelectionDataKey);

        if (selectionData != null && selectionData.GetBool("active"))
        { // second click
            selectionData.SetInt("endX", blockSel.Position.X);
            selectionData.SetInt("endY", blockSel.Position.Y);
            selectionData.SetInt("endZ", blockSel.Position.Z);

            ProcessSelection(player);
        }
        else
        { // first click
            selectionData = new TreeAttribute();
            selectionData.SetInt("startX", blockSel.Position.X);
            selectionData.SetInt("startY", blockSel.Position.Y);
            selectionData.SetInt("startZ", blockSel.Position.Z);
            selectionData.SetBool("active", true);

            player.Entity.WatchedAttributes[SelectionDataKey] = selectionData;
            player.Entity.WatchedAttributes.MarkPathDirty(SelectionDataKey);

            (api as ICoreServerAPI)?
                .SendMessage(player, 0,
                $"Selection started at {blockSel.Position}",
                EnumChatType.Notification
            );
        }

        handling = EnumHandHandling.PreventDefault;
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
}
