using System.Linq;

using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

using IndustrialRevolution.Entities.Mechanism;

namespace IndustrialRevolution.Renderers;

/// Line boxes for debugging: mechanism selection boxes and Bepu AABBs
public class DebugWireframeRenderer : IRenderer
{
    private static readonly Vec4f HitboxColor = new(1f, 0.85f, 0f, 1f);
    private static readonly Vec4f AabbColor = new(0f, 1f, 0.4f, 1f);
    private const float LineWidth = 2f;

    private readonly IndustrialRevolutionModSystem system;
    private readonly ICoreClientAPI capi;
    private readonly WireframeCube cube;

    public DebugWireframeRenderer(
        IndustrialRevolutionModSystem system, ICoreClientAPI capi
    )
    {
        this.system = system;
        this.capi = capi;
        this.cube = WireframeCube.CreateUnitCube(capi, ColorUtil.WhiteArgb);
    }

    public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
    {
        if (this.capi.HideGuis) return;

        if (this.system.ShowHitboxes) this.RenderHitboxes();
        if (this.system.ShowAabbs) this.RenderAabbs();
    }

    private void RenderHitboxes()
    {
        foreach (var entity in this.capi.World.LoadedEntities.Values)
        {
            if (entity is not EntityMechanism) continue;

            // NOTE: box corners are relative to the entity position
            Cuboidf box = entity.SelectionBox;
            var pos = entity.Pos;

            this.cube.Render(
                this.capi,
                pos.X + box.X1, pos.Y + box.Y1, pos.Z + box.Z1,
                box.XSize, box.YSize, box.ZSize,
                LineWidth, HitboxColor
            );
        }
    }

    private void RenderAabbs()
    {
        var data = this.system.GetLatestDebugData();
        if (data == null) return;

        foreach (var body in data.DynamicBodies.Concat(data.StaticBodies))
        {
            this.cube.Render(
                this.capi,
                body.AabbMinX, body.AabbMinY, body.AabbMinZ,
                body.AabbMaxX - body.AabbMinX,
                body.AabbMaxY - body.AabbMinY,
                body.AabbMaxZ - body.AabbMinZ,
                LineWidth, AabbColor
            );
        }
    }

    public double RenderOrder => 0.9;
    public int RenderRange => 999;

    public void Dispose()
    {
        this.cube.Dispose();
    }
}
