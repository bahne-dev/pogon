using System.Linq;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Client;

using System;

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

    // NOTE: "true" as in represenets the dimensions of the rendered selection
    // as opposed to being the width = length dimensions of the bounding box
    private float trueWidth;
    private float trueHeight;
    private float trueLength;

    private IndustrialRevolutionModSystem? modSystem;

    // TODO: this only happens when first selection more in X or more in Z than second selection
    // this only happens when the selections are made using the camera,
    //     if the second selection is made by moving the player, it doesnt happen
    private void SetMaximalBoundingBox()
    {
        if (blockDataList == null || blockDataList.Count == 0)
        {
            log?.Error("SetMaximalBoundingBox: blockDataList is empty!");
            return;
        }

        float minX = blockDataList.Min(p => p.RelX);
        float minY = blockDataList.Min(p => p.RelY);
        float minZ = blockDataList.Min(p => p.RelZ);

        float maxX = blockDataList.Max(p => p.RelX);
        float maxY = blockDataList.Max(p => p.RelY);
        float maxZ = blockDataList.Max(p => p.RelZ);

        float sizeX = maxX - minX + 1;
        float sizeY = maxY - minY + 1;
        float sizeZ = maxZ - minZ + 1;

        this.trueWidth = sizeX;
        this.trueHeight = sizeY;
        this.trueLength = sizeZ;

        var max = Math.Max(sizeX, Math.Max(sizeY, sizeZ));

        SetSelectionBox(max, max);
        SetCollisionBox(max, max);
    }

    public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
    {
        base.Initialize(properties, api, InChunkIndex3d);

        this.LoadBlockData();
        this.SetMaximalBoundingBox();

        if (api.Side == EnumAppSide.Client)
        {
            GenerateMeshes();
        }
        else
        {
            this.modSystem = api.ModLoader.GetModSystem<IndustrialRevolutionModSystem>();

            if (modSystem?.PhysicsSimulation != null)
            {
                InitializePhysics(modSystem.PhysicsSimulation);
            }
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
                RelX = blockTree.GetFloat("x"),
                RelY = blockTree.GetFloat("y"),
                RelZ = blockTree.GetFloat("z"),
                BlockEntityAttributes = blockTree.GetTreeAttribute("bedata")
            };

            blockDataList.Add(data);
        }

        this.Api.Logger.Debug(
            $"EntityConstruction loaded with {blockDataList.Count} blocks"
        );
    }

    public override void OnGameTick(float dt)
    {
        base.OnGameTick(dt);

        // rotation demo
        // RotationY += 0.02f;
        // RotationX += 0.01f;
        // RotationZ += 0.005f;

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

            if (meshData == null) return;

            blockMeshes[i] = capi.Render.UploadMesh(meshData);
            blockOffsets[i] = new Vec3f(
                blockData.RelX, blockData.RelY, blockData.RelZ
            );
        }

        meshesGenerated = true;
        log?.Info($"Generated {blockDataList.Count} block meshes");
    }

    public MeshRef[] GetBlockMeshes() => blockMeshes;
    public Vec3f[] GetBlockOffsets() => blockOffsets;
}
