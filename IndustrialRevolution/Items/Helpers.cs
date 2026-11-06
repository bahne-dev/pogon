using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;

namespace IndustrialRevolution;

public partial class ItemConstructionGlue : Item
{
    /// Turns the given blocks into one mechanism entity, whatever their shape.
    /// Returns how many blocks were glued, air positions get skipped.
    public static int GlueRegion(
        IWorldAccessor world, IEnumerable<BlockPos> positions
    )
    {
        List<BlockPos> region = positions.ToList();
        if (region.Count == 0) return 0;

        EntityProperties? type = world.GetEntityType(
            new AssetLocation("industrialrevolution:mechanism")
        );
        if (type == null)
        {
            world.Logger.Error("mechanism entity type not registered!");
            return 0;
        }

        BlockPos min = new BlockPos(
            region.Min(p => p.X), region.Min(p => p.Y), region.Min(p => p.Z)
        );
        BlockPos max = new BlockPos(
            region.Max(p => p.X), region.Max(p => p.Y), region.Max(p => p.Z)
        );

        List<StoredBlock> blocks = new List<StoredBlock>();
        IBlockAccessor ba = world.BlockAccessor;

        float width = max.X - min.X + 1;
        float length = max.Z - min.Z + 1;

        foreach (BlockPos pos in region)
        {
            Block block = ba.GetBlock(pos);
            if (block.Id == 0) continue;

            BlockEntity? be = ba.GetBlockEntity(pos);

            ITreeAttribute? beData = null;
            if (be != null)  // if is blockEntity
            {
                beData = new TreeAttribute();
                be.ToTreeAttributes(beData);  // preserve its data
            }

            float corrX = pos.X - min.X - (width / 2);
            float corrZ = pos.Z - min.Z - (length / 2);

            // y is not translated towards center because
            // an entity's position's Y, is the block its standing on,
            // rather than 0.5 blocks up in the air
            //
            // NOTE: i dont know how this doesnt mess with the physics
            // body construction/rendering
            float corrY = pos.Y - min.Y;

            blocks.Add(new StoredBlock
            {
                Code = block.Code.ToShortString(),
                RelX = corrX,
                RelY = corrY,
                RelZ = corrZ,
                BlockEntityAttributes = beData
            });

            ba.SetBlock(0, pos);
            ba.RemoveBlockEntity(pos);  // TODO: this might be redundant
        }

        if (blocks.Count == 0) return 0;

        SpawnMechanismEntity(world, type, min.ToVec3d(), max.ToVec3d(), blocks);

        return blocks.Count;
    }

    private static void SpawnMechanismEntity(
        IWorldAccessor world,
        EntityProperties type,
        Vec3d min, Vec3d max, List<StoredBlock> blocks
    )
    {
        Entity entity = world.ClassRegistry.CreateEntity(type);

        var width = max.X - min.X + 1;
        var height = max.Y - min.Y + 1;
        var length = max.Z - min.Z + 1;

        var centerPos = min.Add(width / 2, 0, length / 2);

        // TODO: fix warning
        entity.ServerPos.SetPos(centerPos);
        entity.Pos.SetFrom(entity.ServerPos);

        // build data tree to pass to entity
        ITreeAttribute blockData = new TreeAttribute();
        blockData.SetInt("count", blocks.Count);
        for (int i = 0; i < blocks.Count; i++)
        {
            ITreeAttribute blockTree = new TreeAttribute();
            blockTree.SetString("code", blocks[i].Code);
            blockTree.SetFloat("x", blocks[i].RelX);
            blockTree.SetFloat("y", blocks[i].RelY);
            blockTree.SetFloat("z", blocks[i].RelZ);

            if (blocks[i].BlockEntityAttributes != null)
            {
                blockTree["bedata"] = blocks[i].BlockEntityAttributes;
            }

            blockData[$"block_{i}"] = blockTree;
        }

        // pass the selection data to the entity
        entity.WatchedAttributes["blockData"] = blockData;
        entity.WatchedAttributes.MarkPathDirty("blockData");

        world.SpawnEntity(entity);
    }

    public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
    {
        return new WorldInteraction[]
        {
            new WorldInteraction
            {
                ActionLangCode = "Click two corners to add a box",
                MouseButton = EnumMouseButton.Right
            },
            new WorldInteraction
            {
                ActionLangCode = "Click two corners to remove a box",
                MouseButton = EnumMouseButton.Left
            },
            new WorldInteraction
            {
                ActionLangCode = "Glue the selection",
                HotKeyCode = "shift",
                MouseButton = EnumMouseButton.Right
            },
            new WorldInteraction
            {
                ActionLangCode = "Clear the selection",
                HotKeyCode = "shift",
                MouseButton = EnumMouseButton.Left
            }
        };
    }
}

public class StoredBlock
{
    public string? Code;

    // NOTE: X and Z are relative to the center of the whole entity,
    // but Y has to be relative to the center of the bottom of the entity
    public float RelX, RelY, RelZ;

    public ITreeAttribute? BlockEntityAttributes;
}
