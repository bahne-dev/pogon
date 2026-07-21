using Vintagestory.API.Common.Entities;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

using System.Numerics;

using IndustrialRevolution.Entities.Mechanism;

namespace IndustrialRevolution.Renderers;

public class MechanismRenderer : EntityRenderer
{
    private new EntityMechanism? entity;
    private new ICoreClientAPI? capi;
    private MeshRef[]? blockMeshes;  // GPU mesh handles
    private Vec3f[]? blockOffsets;   // relative positions

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

        if (entity != null)
        {
            blockMeshes = entity.GetBlockMeshes();
            blockOffsets = entity.GetBlockOffsets();

            capi?.Logger.Notification(
                $"Renderer loaded with {blockMeshes?.Length ?? 0} meshes"
            );
        }
    }

    private int joecount = 0;

    public override void DoRender3DOpaque(float dt, bool isShadowPass)
    {
        if (blockMeshes == null || capi == null || entity == null) return;

        IRenderAPI rapi = capi.Render;
        Vec3d camPos = capi.World.Player.Entity.CameraPos;

        // get standard shader (doesnt look exactly like the regular block yet)
        IStandardShaderProgram prog = rapi.PreparedStandardShader(
            (int)entity.Pos.X, (int)entity.Pos.Y, (int)entity.Pos.Z
        );

        prog.Use();
        prog.Tex2D = capi.BlockTextureAtlas.AtlasTextures[0].TextureId;

        for (int i = 0; i < this.blockMeshes.Length; i++)
        {
            if (blockMeshes[i] == null || blockOffsets == null) continue;

            if (joecount < 10) {
                IndustrialRevolutionModSystem.Logger.Debug(
                    $"pos @ rend: ({entity.Pos.X} {entity.Pos.Y} {entity.Pos.Z})"
                );
                joecount++;
            }

            float entityX = (float)(entity.Pos.X - camPos.X);
            float entityY = (float)(entity.Pos.Y - camPos.Y);
            float entityZ = (float)(entity.Pos.Z - camPos.Z);

            float comX = this.entity.centerOfMass.X;
            float comY = this.entity.centerOfMass.Y;
            float comZ = this.entity.centerOfMass.Z;

            if (this.entity.centerOfMass.X == 0) {
                IndustrialRevolutionModSystem.Logger.Debug("broo...");
            }

            prog.ModelMatrix = new Matrixf()
                .Translate(entityX, entityY, entityZ)
                .RotateX(entity.RotationX)
                .RotateY(entity.RotationY)
                .RotateZ(entity.RotationZ)
                .Translate(
                    blockOffsets[i].X - comX,
                    blockOffsets[i].Y - comY,
                    blockOffsets[i].Z - comZ
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
