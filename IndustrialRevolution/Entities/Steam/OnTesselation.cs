using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using System.Collections.Generic;
using Vintagestory.API.Client;
using System;

namespace IndustrialRevolution.Entities.Steam;

// TODO: replace entityagent with entity (no npc ai needed)
internal partial class EntitySteam : EntityAgent
{
    // TODO: only draw edge voxels, somehow, maybe through toCheck or something
    // because drawing in occluded voxels for chiseled blocks could get crazy...
    public HashSet<SteamPos> GetOccupied()
    {
        byte[] posData = WatchedAttributes.GetBytes("steamOccupied");
        if (posData == null) return new HashSet<SteamPos>();

        byte[] chslData = WatchedAttributes.GetBytes("chiseledSteam");

        return SteamSerializer.Deserialize(posData, chslData);
    }

    private void bake(
        Shape parentShape,
        Shape childShape,
        string childLocationForLogging,
        string parentLocationForLogging,
        ICoreClientAPI capi
    )
    {
        parentShape.StepParentShape(
            childShape,
            childLocationForLogging,
            parentLocationForLogging,
            capi.Logger,
            (textureCode, textureLoc) =>
            {
                var ctex = new CompositeTexture(textureLoc);
                ctex.Bake(capi.Assets);

                capi.EntityTextureAtlas.GetOrInsertTexture(
                    ctex.Baked.TextureFilenames[0],
                    out int subId,
                    out _
                );

                ctex.Baked.TextureSubId = subId;
                this.Properties.Client.Textures[textureCode] = ctex;
            }
        );
    }

    private void fillVoxels(
        bool[,,] steamGrid,
        SteamPos pos, BlockPos root, Shape voxShape,
        ICoreClientAPI capi, AssetLocation voxShapeLoc,
        ref Shape entityShape, string shapePathForLogging, ref bool shapeIsCloned
    )
    {
        for (int x = 0; x < 16; x++)
        {
            for (int y = 0; y < 16; y++)
            {
                for (int z = 0; z < 16; z++)
                {
                    if (steamGrid[x, y, z] == false) continue;

                    voxShape = voxShape.Clone();

                    int offsetX = (pos.X - root.X) * 16 + x;
                    int offsetY = (pos.Y - root.Y) * 16 + y;
                    int offsetZ = (pos.Z - root.Z) * 16 + z;

                    voxShape.Elements[0].From[0] = offsetX;
                    voxShape.Elements[0].From[1] = offsetY;
                    voxShape.Elements[0].From[2] = offsetZ;

                    voxShape.Elements[0].To[0] = offsetX + 1;
                    voxShape.Elements[0].To[1] = offsetY + 1;
                    voxShape.Elements[0].To[2] = offsetZ + 1;

                    this.bake(
                        entityShape,
                        voxShape,
                        voxShapeLoc.ToString(),
                        shapePathForLogging,
                        capi
                    );
                }
            }
        }
    }

    protected override void OnTesselation(ref Shape entityShape, string shapePathForLogging, ref bool shapeIsCloned)
    {
        var occupied = GetOccupied();
        if (occupied.Count == 0)
        {
            log?.Debug("occupied is 0, skipping");
            base.OnTesselation(ref entityShape, shapePathForLogging, ref shapeIsCloned);
            return;
        }

        log?.Debug("=== OnTesselation START ===");

        if (!shapeIsCloned)
        {
            log?.Debug("Cloning shape (shapeIsCloned was false)");
            entityShape = entityShape.Clone();
            shapeIsCloned = true;
        }

        ICoreClientAPI? capi = this.Api as ICoreClientAPI;
        var voxShapeLoc = new AssetLocation("industrialrevolution:shapes/entity/steam-voxel.json");
        var root = this.Pos.AsBlockPos;

        IAsset? asset = capi?.Assets.TryGet(voxShapeLoc);
        Shape? voxShape = asset?.ToObject<Shape>();

        if (asset == null || voxShape == null || capi == null)
        {
            base.OnTesselation(ref entityShape, shapePathForLogging, ref shapeIsCloned);
            return;
        }

        foreach (var pos in occupied)
        {
            if (!pos.IsFullBlock)
            {
                if (pos.SteamGrid == null)
                {
                    log?.Warning(
                        $"non fullblock {pos.ToLocalCoords()}" +
                        " has no steam grid"
                    );
                    continue;
                }

                this.fillVoxels(
                    pos.SteamGrid, pos, root,
                    voxShape, capi, voxShapeLoc,
                    ref entityShape, shapePathForLogging, ref shapeIsCloned
                );
            }
            else
            {
                voxShape = voxShape.Clone(); // make a copy

                // position the copy
                int offsetX = (pos.X - root.X);
                int offsetY = (pos.Y - root.Y);
                int offsetZ = (pos.Z - root.Z);

                voxShape.Elements[0].From[0] = offsetX * 16;
                voxShape.Elements[0].From[1] = offsetY * 16;
                voxShape.Elements[0].From[2] = offsetZ * 16;

                voxShape.Elements[0].To[0] = (offsetX + 1) * 16;
                voxShape.Elements[0].To[1] = (offsetY + 1) * 16;
                voxShape.Elements[0].To[2] = (offsetZ + 1) * 16;

                // bake it into the parent
                this.bake(
                    entityShape,
                    voxShape,
                    voxShapeLoc.ToString(),
                    shapePathForLogging,
                    capi
                );
            }
        }

        base.OnTesselation(ref entityShape, shapePathForLogging, ref shapeIsCloned);
        log?.Debug("=== OnTesselation END ===");
    }
}
