using Vintagestory.API.Common;
using Vintagestory.API.Client;

using IndustrialRevolution.util;
using IndustrialRevolution.Blocks;
using IndustrialRevolution.BlockEntities;
using IndustrialRevolution.Entities.Steam;
using IndustrialRevolution.Entities.Mechanism;

namespace IndustrialRevolution;

public class IndustrialRevolutionModSystem : ModSystem
{

    public static ModLogger? Logger;
    private ICoreClientAPI? capi; // Add this field

    public override void StartPre(ICoreAPI api)
    {
        Logger = new ModLogger(api, "industrial-revolution-main");
        Logger.Info($"Mod started on side: {api.Side}");
        api.Logger.Event($"IR Logger created on {api.Side}");
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);
        this.capi = api;

        api.RegisterEntityRendererClass(
            "mechanismrenderer",
            typeof(MechanismRenderer)
        );

        Logger?.Info("Construction entity renderer registered");
    }

    private void RegisterComplexBlocks(ICoreAPI api)
    {
        api.RegisterBlockClass(
            Mod.Info.ModID + "."
            + "blockboiler", typeof(BlockBoiler)
        );
    }

    private void RegisterComplexEntities(ICoreAPI api)
    {
        api.RegisterEntity(
            Mod.Info.ModID + "."
            + "entitymechanism",
            typeof(EntityMechanism)
        );

        api.RegisterEntity(
            Mod.Info.ModID + "."
            + "entitysteam",
            typeof(EntitySteam)
        );
    }

    private void RegisterBlockEntities(ICoreAPI api)
    {
        api.RegisterBlockEntityClass(
            Mod.Info.ModID + "." +
            "entityboiler",
            typeof(BlockEntityBoiler)
        );
    }

    private void RegisterComplexItems(ICoreAPI api)
    {
        api.RegisterItemClass(
            Mod.Info.ModID + "."
            + "itemconstructionglue",
            typeof(ItemConstructionGlue)
        );
    }

    public override void Start(ICoreAPI api)
    {
        base.Start(api);

        this.RegisterComplexEntities(api);
        this.RegisterComplexBlocks(api);
        this.RegisterBlockEntities(api);
        this.RegisterComplexItems(api);

        api.World.Logger.Event("started 'Industrial Revolution' mod");
    }
}
