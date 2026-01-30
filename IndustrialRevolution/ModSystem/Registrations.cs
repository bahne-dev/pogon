namespace IndustrialRevolution;

using IndustrialRevolution.Entities.Mechanism;
using IndustrialRevolution.Entities.Steam;

using IndustrialRevolution.BlockEntities;
using IndustrialRevolution.Blocks;

using Vintagestory.API.Common;

public partial class IndustrialRevolutionModSystem {
    private void RegisterComplexBlocks(ICoreAPI api)
    {
        api.RegisterBlockClass(
            this.Mod.Info.ModID + "."
            + "blockboiler", typeof(BlockBoiler)
        );
    }

    private void RegisterComplexEntities(ICoreAPI api)
    {
        api.RegisterEntity(
            this.Mod.Info.ModID + "."
            + "entitymechanism",
            typeof(EntityMechanism)
        );

        api.RegisterEntity(
            this.Mod.Info.ModID + "."
            + "entitysteam",
            typeof(EntitySteam)
        );
    }

    private void RegisterBlockEntities(ICoreAPI api)
    {
        api.RegisterBlockEntityClass(
            this.Mod.Info.ModID + "." +
            "entityboiler",
            typeof(BlockEntityBoiler)
        );
    }

    private void RegisterComplexItems(ICoreAPI api)
    {
        api.RegisterItemClass(
            this.Mod.Info.ModID + "."
            + "itemconstructionglue",
            typeof(ItemConstructionGlue)
        );
    }
}
