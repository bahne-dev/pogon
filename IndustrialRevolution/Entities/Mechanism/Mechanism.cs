using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Client;

using IndustrialRevolution.util;

namespace IndustrialRevolution.Entities.Mechanism;

public partial class EntityMechanism : Entity
{
    private List<StoredBlock> blockDataList = new List<StoredBlock>();
    private MeshRef[] blockMeshes = [];
    private Vec3f[] blockOffsets = [];
    private bool meshesGenerated = false;

    private ModLogger? log = IndustrialRevolutionModSystem.Logger;

    public float RotationX { get; set; } = 0f;
    public float RotationY { get; set; } = 0f;
    public float RotationZ { get; set; } = 0f;

    public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
    {
        base.Initialize(properties, api, InChunkIndex3d);

        LoadBlockData();

        if (api.Side == EnumAppSide.Client)
        {
            blockDataList.ToList().ForEach(b => log?.Debug("blk: " + b.Code));
            blockMeshes.ToList().ForEach(m => log?.Debug("mesh: " + m.ToString()));
            blockOffsets.ToList().ForEach(o => log?.Debug("offset: " + o.ToString()));

            GenerateMeshes();
        }
    }

    private void LoadBlockData()
    {
        blockDataList = new List<StoredBlock>();

        if (!WatchedAttributes.HasAttribute("blockData"))
        {
            this.Api.Logger.Warning("EntityConstruction has no blockData!");
            return;
        }

        ITreeAttribute blockDataTree = WatchedAttributes
            .GetTreeAttribute("blockData");
        int count = blockDataTree.GetInt("count");

        for (int i = 0; i < count; i++)
        {
            ITreeAttribute blockTree = blockDataTree
                .GetTreeAttribute($"block_{i}");
            if (blockTree == null) continue;

            StoredBlock data = new StoredBlock
            {
                Code = blockTree.GetString("code"),
                RelX = blockTree.GetInt("x"),
                RelY = blockTree.GetInt("y"),
                RelZ = blockTree.GetInt("z"),
                BlockEntityAttributes = blockTree.GetTreeAttribute("bedata")
            };

            blockDataList.Add(data);
        }

        this.Api.Logger.Notification(
            $"EntityConstruction loaded with {blockDataList.Count} blocks"
        );
    }

    public override void OnGameTick(float dt)
    {
        base.OnGameTick(dt);

        // rotation demo
        RotationY += 0.02f;
        RotationX += 0.01f;
        RotationZ += 0.005f;

        // TODO: add physics shit here later
    }

    private void GenerateMeshes()
    {
        if (
            meshesGenerated ||
            blockDataList == null ||
            blockDataList.Count == 0
        )
            return;

        ICoreClientAPI capi = Api as ICoreClientAPI;
        if (capi == null) return;

        blockMeshes = new MeshRef[blockDataList.Count];
        blockOffsets = new Vec3f[blockDataList.Count];

        for (int i = 0; i < blockDataList.Count; i++)
        {
            StoredBlock blockData = blockDataList[i];

            Block block = Api.World.GetBlock(new AssetLocation(blockData.Code));
            if (block == null || block.Id == 0)
            {
                log?.Warning($"Failed to get block: {blockData.Code}");
                continue;
            }

            capi.Tesselator.TesselateBlock(block, out MeshData meshData);

            if (meshData != null)
            {
                blockMeshes[i] = capi.Render.UploadMesh(meshData);
                blockOffsets[i] = new Vec3f(
                    blockData.RelX, blockData.RelY, blockData.RelZ
                );
            }
        }

        meshesGenerated = true;
        log?.Info($"Generated {blockDataList.Count} block meshes");
    }

    public MeshRef[] GetBlockMeshes() => blockMeshes;
    public Vec3f[] GetBlockOffsets() => blockOffsets;
}
