using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Datastructures;

namespace IndustrialRevolution;

public partial class ItemConstructionGlue : Item
{
    private void ProcessSelection(IPlayer player)
    {
        ITreeAttribute? selectionData = player
            .Entity
            .WatchedAttributes
            .GetTreeAttribute(SelectionDataKey);

        if (selectionData == null) return;

        BlockPos start = new BlockPos(
            selectionData.GetInt("startX"),
            selectionData.GetInt("startY"),
            selectionData.GetInt("startZ")
        );
        BlockPos end = new BlockPos(
            selectionData.GetInt("endX"),
            selectionData.GetInt("endY"),
            selectionData.GetInt("endZ")
        );

        // align start and end pos to be (neg neg neg), (pos pos pos)
        BlockPos min = new BlockPos(
            Math.Min(start.X, end.X),
            Math.Min(start.Y, end.Y),
            Math.Min(start.Z, end.Z)
        );
        BlockPos max = new BlockPos(
            Math.Max(start.X, end.X),
            Math.Max(start.Y, end.Y),
            Math.Max(start.Z, end.Z)
        );

        // add 1 to get correct volume (coords next to each other
        // would produce 1x1 instead of 2x2 without this)
        int sizeX = max.X - min.X + 1;
        int sizeY = max.Y - min.Y + 1;
        int sizeZ = max.Z - min.Z + 1;

        (api as ICoreServerAPI)?.SendMessage(player, 0,
            $"Selection size: {sizeX}x{sizeY}x{sizeZ}",
            EnumChatType.Notification
        );

        if (
                sizeX > MaxSelectionSize ||
                sizeY > MaxSelectionSize ||
                sizeZ > MaxSelectionSize
            )
        {
            (api as ICoreServerAPI)?.SendMessage(player, 0,
                "Selection too large! " +
                $"Max {MaxSelectionSize}x{MaxSelectionSize}x{MaxSelectionSize}",
                EnumChatType.Notification
            );
            player.Entity.WatchedAttributes.RemoveAttribute(SelectionDataKey);
            return;
        }

        ConvertToEntity(player, min, max);

        player.Entity.WatchedAttributes.RemoveAttribute(SelectionDataKey);
    }

    private void ConvertToEntity(IPlayer player, BlockPos min, BlockPos max)
    {
        List<StoredBlock> blocks = new List<StoredBlock>();
        IBlockAccessor ba = api.World.BlockAccessor;

        for (int x = min.X; x <= max.X; x++)
        {
            for (int y = min.Y; y <= max.Y; y++)
            {
                for (int z = min.Z; z <= max.Z; z++)
                {
                    BlockPos pos = new BlockPos(x, y, z);
                    Block block = ba.GetBlock(pos);
                    if (block.Id == 0) continue;

                    BlockEntity? be = ba.GetBlockEntity(pos);

                    ITreeAttribute? beData = null;
                    if (be != null)
                    {
                        beData = new TreeAttribute();
                        be.ToTreeAttributes(beData);
                    }

                    blocks.Add(new StoredBlock
                    {
                        Code = block.Code.ToShortString(),
                        RelX = x - min.X,
                        RelY = y - min.Y,
                        RelZ = z - min.Z,
                        BlockEntityAttributes = beData
                    });

                    ba.SetBlock(0, pos);
                    ba.RemoveBlockEntity(pos);
                }
            }
        }

        if (blocks.Count == 0)
        {
            (api as ICoreServerAPI)?.SendMessage(player, 0,
                "No blocks in selection!", EnumChatType.Notification);
            return;
        }

        SpawnMechanismEntity(min.ToVec3d().Add(0.5, 0, 0.5), blocks);

        player.Entity.RightHandItemSlot?.Itemstack?.Collectible.DamageItem(
            api.World, player.Entity, player.Entity.RightHandItemSlot, 1);

        (api as ICoreServerAPI)?.SendMessage(player, 0,
            $"Created construction with {blocks.Count} blocks",
            EnumChatType.Notification
        );
    }

    private void SpawnMechanismEntity(Vec3d pos, List<StoredBlock> blocks)
    {
        EntityProperties? type = api.World.GetEntityType(
            new AssetLocation("industrialrevolution:mechanism")
        );
        if (type == null)
        {
            api.Logger.Error("mechanism entity type not registered!");
            return;
        }

        Entity entity = api.World.ClassRegistry.CreateEntity(type);
        entity.ServerPos.SetPos(pos);
        entity.Pos.SetFrom(entity.ServerPos);

        // build data tree to pass to entity
        ITreeAttribute blockData = new TreeAttribute();
        blockData.SetInt("count", blocks.Count);
        for (int i = 0; i < blocks.Count; i++)
        {
            ITreeAttribute blockTree = new TreeAttribute();
            blockTree.SetString("code", blocks[i].Code);
            blockTree.SetInt("x", blocks[i].RelX);
            blockTree.SetInt("y", blocks[i].RelY);
            blockTree.SetInt("z", blocks[i].RelZ);

            if (blocks[i].BlockEntityAttributes != null)
            {
                blockTree["bedata"] = blocks[i].BlockEntityAttributes;
            }

            blockData[$"block_{i}"] = blockTree;
        }

        // pass the selection data to the entity
        entity.WatchedAttributes["blockData"] = blockData;
        entity.WatchedAttributes.MarkPathDirty("blockData");

        api.World.SpawnEntity(entity);
    }

    public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
    {
        return new WorldInteraction[]
        {
            new WorldInteraction
            {
                ActionLangCode = "Click to start, click again to end selection",
                MouseButton = EnumMouseButton.Right
            }
        };
    }
}

public class StoredBlock
{
    public string? Code;
    public int RelX, RelY, RelZ;
    public ITreeAttribute? BlockEntityAttributes;
}
