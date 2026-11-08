using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Server;

using IndustrialRevolution.util;

using IndustrialRevolution.Renderers;

namespace IndustrialRevolution;

public partial class IndustrialRevolutionModSystem : ModSystem
{
    public static ModLogger Logger { get; private set; }

    private ICoreClientAPI? capi;
    private ICoreServerAPI? sapi;

    public override void StartPre(ICoreAPI api)
    {
        Logger = new ModLogger(api, "industrial-revolution-main");
    }

    public override void StartClientSide(ICoreClientAPI capi)
    {
        base.StartClientSide(capi);
        this.capi = capi;

        capi.RegisterEntityRendererClass(
            "mechanismrenderer",
            typeof(MechanismRenderer)
        );

        Logger?.Info("Construction entity renderer registered");

        this.RegisterClientChannel(capi);
        this.capi.Event.RegisterRenderer(
            new PhysicsDebugRenderer(this, capi), EnumRenderStage.Opaque
        );
        this.RegisterVizCommand(capi);

        // after the game's own selection box debug, which uses this stage
        this.capi.Event.RegisterRenderer(
            new DebugWireframeRenderer(this, capi),
            EnumRenderStage.AfterFinalComposition
        );
    }

    public override void StartServerSide(ICoreServerAPI sapi)
    {
        base.StartServerSide(sapi);
        this.sapi = sapi;

        this.RegisterServerChannel(sapi);
        this.StartPhysics(sapi);
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
