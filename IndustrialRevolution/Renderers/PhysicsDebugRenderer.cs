using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace IndustrialRevolution.Renderers;

public class PhysicsDebugRenderer : IRenderer
{
    private IndustrialRevolutionModSystem system;
    private ICoreClientAPI capi;
    private MeshRef? redCubeMeshRef;
    private MeshRef? blueCubeMeshRef;

    public PhysicsDebugRenderer(IndustrialRevolutionModSystem sys, ICoreClientAPI api)
    {
        this.system = sys;
        this.capi = api;
        CreateCubeMeshes();
    }

    private void CreateCubeMeshes()
    {
        Block redBlock = capi.World.GetBlock(new AssetLocation("game:creativeglow-32"));
        Block blueBlock = capi.World.GetBlock(new AssetLocation("game:creativeglow-52"));

        if (redBlock == null || redBlock.Id == 0)
        {
            capi.Logger.Error("PhysicsDebugRenderer: Could not find creativeglow-32 block!");
            return;
        }

        if (blueBlock == null || blueBlock.Id == 0)
        {
            capi.Logger.Error("PhysicsDebugRenderer: Could not find creativeglow-52 block!");
            return;
        }

        capi.Tesselator.TesselateBlock(redBlock, out MeshData redMeshData);
        capi.Tesselator.TesselateBlock(blueBlock, out MeshData blueMeshData);

        if (redMeshData != null)
        {
            for (int i = 0; i < redMeshData.GetVerticesCount(); i++)
            {
                redMeshData.Rgba[i * 4 + 3] = 150;
            }
            redCubeMeshRef = capi.Render.UploadMesh(redMeshData);
        }

        if (blueMeshData != null)
        {
            for (int i = 0; i < blueMeshData.GetVerticesCount(); i++)
            {
                blueMeshData.Rgba[i * 4 + 3] = 150;
            }
            blueCubeMeshRef = capi.Render.UploadMesh(blueMeshData);
        }
    }

    public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
    {
        if (capi == null || redCubeMeshRef == null || blueCubeMeshRef == null)
            return;

        var debugData = this.system.GetLatestDebugData();
        if (debugData == null) return;

        var player = capi.World.Player;
        if (player?.Entity == null) return;
        var camPos = player.Entity.CameraPos;

        IRenderAPI rapi = capi.Render;
        IStandardShaderProgram prog = rapi.PreparedStandardShader(0, 0, 0);
        prog.Use();
        prog.Tex2D = capi.BlockTextureAtlas.AtlasTextures[0].TextureId;

        foreach (var body in debugData.DynamicBodies)
        {
            RenderBody(prog, rapi, body, camPos, redCubeMeshRef);
        }

        foreach (var body in debugData.StaticBodies)
        {
            RenderBody(prog, rapi, body, camPos, blueCubeMeshRef);
        }

        prog.Stop();
    }

    private void RenderBody(
        IStandardShaderProgram prog,
        IRenderAPI rapi,
        BodyDebugInfo body,
        Vec3d camPos,
        MeshRef meshRef)
    {
        if (body.IsCompound)
        {
            // TODO: have this parent quat apply to the children correctly
            var parentQuat = new System.Numerics.Quaternion(
                body.RotX, body.RotY, body.RotZ, body.RotW
            );

            foreach (var child in body.Children)
            {
                var localPos = new System.Numerics.Vector3(
                    child.LocalX, child.LocalY, child.LocalZ
                );
                var rotatedPos = System.Numerics.Vector3.Transform(localPos, parentQuat);

                float worldX = body.PosX + rotatedPos.X;
                float worldY = body.PosY + rotatedPos.Y;
                float worldZ = body.PosZ + rotatedPos.Z;

                // camera-relative
                float x = (float)(worldX - camPos.X);
                float y = (float)(worldY - camPos.Y);
                float z = (float)(worldZ - camPos.Z);

                // TODO: something isnt right here
                var (pitch, yaw, roll) = util.General.QuaternionToEuler(parentQuat);

                prog.ModelMatrix = new Matrixf()
                    .Translate(x, y, z)
                    .RotateX(pitch)
                    .RotateY(yaw)
                    .RotateZ(roll)
                    .Scale(1f, 1f, 1f)
                    .Values;

                rapi.RenderMesh(meshRef);
            }
        }
        else
        { // simple box case
            float x = (float)(body.PosX - camPos.X);
            float y = (float)(body.PosY - camPos.Y);
            float z = (float)(body.PosZ - camPos.Z);

            var q = new System.Numerics.Quaternion(
                body.RotX, body.RotY, body.RotZ, body.RotW
            );
            var (pitch, yaw, roll) = util.General.QuaternionToEuler(q);

            prog.ModelMatrix = new Matrixf()
                .Translate(x, y, z)
                .RotateX(pitch)
                .RotateY(yaw)
                .RotateZ(roll)
                .Scale(body.Width, body.Height, body.Length)
                .Values;

            rapi.RenderMesh(meshRef);
        }
    }

    public double RenderOrder => 0.9;
    public int RenderRange => 999;

    public void Dispose()
    {
        redCubeMeshRef?.Dispose();
        blueCubeMeshRef?.Dispose();
    }
}
