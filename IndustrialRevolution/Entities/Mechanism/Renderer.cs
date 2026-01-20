using Vintagestory.API.Common.Entities;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace IndustrialRevolution.Entities.Mechanism;

public class MechanismRenderer : EntityRenderer
{
    private EntityMechanism entity;
    private ICoreClientAPI capi;
    private MeshRef[] blockMeshes;  // GPU mesh handles
    private Vec3f[] blockOffsets;   // relative positions

    public double RenderOrder => 0.5;
    public int RenderRange => 999;

    public MechanismRenderer(Entity entity, ICoreClientAPI api)
        : base(entity, api)
    {
        this.entity = entity as EntityMechanism;
        this.capi = api;
    }

    public override void OnEntityLoaded()
    {
        base.OnEntityLoaded();

        EntityMechanism mechanism = entity as EntityMechanism;
        blockMeshes = mechanism.GetBlockMeshes();
        blockOffsets = mechanism.GetBlockOffsets();

        capi.Logger.Notification(
            $"Renderer loaded with {blockMeshes?.Length ?? 0} meshes"
        );
    }

    public override void DoRender3DOpaque(float dt, bool isShadowPass)
    {
        if (blockMeshes == null) return;

        IRenderAPI rapi = capi.Render;
        Vec3d camPos = capi.World.Player.Entity.CameraPos;

        // get standard shader (doesnt look exactly like the regular block yet)
        IStandardShaderProgram prog = rapi.PreparedStandardShader(
            (int)entity.Pos.X, (int)entity.Pos.Y, (int)entity.Pos.Z
        );

        prog.Use();
        prog.Tex2D = capi.BlockTextureAtlas.AtlasTextures[0].TextureId;

        for (int i = 0; i < blockMeshes.Length; i++)
        {
            if (blockMeshes[i] == null) continue;

            float entityX = (float)(entity.Pos.X - camPos.X);
            float entityY = (float)(entity.Pos.Y - camPos.Y);
            float entityZ = (float)(entity.Pos.Z - camPos.Z);

            prog.ModelMatrix = new Matrixf()
                .Translate(entityX, entityY, entityZ)
                .RotateX(entity.RotationX)
                .RotateY(entity.RotationY)
                .RotateZ(entity.RotationZ)
                .Translate(
                    blockOffsets[i].X - 0.5f,
                    blockOffsets[i].Y,
                    blockOffsets[i].Z - 0.5f
                )
                .Values;

            rapi.RenderMesh(blockMeshes[i]);
        }

        prog.Stop();
    }

    public override void Dispose()
    {
        if (blockMeshes != null)
        {
            foreach (var mesh in blockMeshes)
                mesh?.Dispose();
        }
    }
}
