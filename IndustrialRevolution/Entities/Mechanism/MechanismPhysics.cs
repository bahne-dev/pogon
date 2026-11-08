using System;
using System.Numerics;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

using BepuPhysics;
using BepuPhysics.Collidables;

using IndustrialRevolution.Entities.Mechanism.Physics;

namespace IndustrialRevolution.Entities.Mechanism;

public partial class EntityMechanism
{
    private PhysicsSimulation? physicsSimulation;
    private BodyHandle physicsBodyHandle;
    private List<StaticHandle> nearbyStaticHandles = new List<StaticHandle>();
    private bool physicsInitialized = false;

    // TODO: change all tuples to vec3's maybe? maybe it becomes faster cuz
    // SIMD or whatever
    private HashSet<(int x, int y, int z)> visitedCoords =
        new HashSet<(int x, int y, int z)>();

    // FIXME: why do i have this
    private PhysicsSimulation? clientSim;

    // kept to compute the current bounding box every tick
    private Compound? bodyShape;

    public Vector3 centerOfMass
    {
        get => new Vector3(
            WatchedAttributes.GetFloat("x", 0f),
            WatchedAttributes.GetFloat("y", 0f),
            WatchedAttributes.GetFloat("z", 0f)
        );
        set
        {
            WatchedAttributes.SetFloat("x", value.X);
            WatchedAttributes.SetFloat("y", value.Y);
            WatchedAttributes.SetFloat("z", value.Z);
        }
    }

    private void InitializePhysics(PhysicsSimulation sharedSimulation)
    {
        if (physicsInitialized) return;

        physicsSimulation = sharedSimulation;

        this.CreatePhysicsBody();
        this.DetectAndAddNearbyBlocks();

        physicsInitialized = true;
    }

    private void CreatePhysicsBody()
    {
        if (this.blockDataList == null || this.blockDataList.Count == 0) return;
        if (this.physicsSimulation == null) return;

        var simulation = physicsSimulation.Simulation;
        var pool = simulation.BufferPool;

        TypedIndex shapeIndex;
        BodyInertia inertia;
        float mass = blockDataList.Count * 10f;

        // TODO: add different constructions here. Box for simple shapes,
        // compound for small custom shapes, big compound for big custom shapes
        var builder = new CompoundBuilder(pool, simulation.Shapes, this.blockDataList.Count);

        Vector3 centroid = Vector3.Zero;

        foreach (var blk in blockDataList)
        {
            var box = new Box(1f, 1f, 1f);

            // NOTE: Rel* is the block's corner, bepu boxes are centered
            var childPose = new RigidPose(
                new System.Numerics.Vector3(
                    blk.RelX + 0.5f,
                    blk.RelY + 0.5f,
                    blk.RelZ + 0.5f
                ),
                System.Numerics.Quaternion.Identity
            );

            builder.Add(box, childPose, 10f); // TODO: have block masses n shit
            centroid = Vector3.Add(centroid, new Vector3(blk.RelX, blk.RelY, blk.RelZ));
        }

        centroid /= blockDataList.Count;

        builder.BuildDynamicCompound(
            out var children,
            out var bodyInertia,
            out var centerOfMass
        );

        log?.Debug($"com: {this.centerOfMass}");
        this.centerOfMass = centerOfMass;
        log?.Debug($"com: {this.centerOfMass}");

        inertia = bodyInertia;

        builder.Dispose();

        var compoundShape = new Compound(children);

        shapeIndex = simulation.Shapes.Add(compoundShape);
        this.bodyShape = compoundShape;

        RigidPose pose;

        if (this.TryGetBodyPose(out var px, out var py, out var pz, out var rot))
        {
            // if body has stored data already just restore it
            pose = new RigidPose(new Vector3((float)px, (float)py, (float)pz), rot);
        }
        else
        {
            // if no stored data its freshly glued:
            // Pos is the bottom center of the upright mechanism and
            // centerOfMass the offset from there to the body's center
            pose = new RigidPose(new Vector3(
                (float)this.Pos.X + centerOfMass.X,
                (float)this.Pos.Y + centerOfMass.Y,
                (float)this.Pos.Z + centerOfMass.Z
            ));
        }

        var bodyDescription = BodyDescription.CreateDynamic(
            pose,
            inertia,
            new CollidableDescription(shapeIndex, 0.1f),
            // new BodyActivityDescription(0.001f)
            new BodyActivityDescription(sleepThreshold: -1)
        );

        this.physicsBodyHandle = simulation.Bodies.Add(bodyDescription);

        var bodyRef = simulation.Bodies.GetBodyReference(this.physicsBodyHandle);
    }

    // TODO: transfer the pos and orientation stuff to the entity because
    // this only updates based on the entity not the physics body
    private void DetectAndAddNearbyBlocks()
    {
        if (this.physicsSimulation == null) return;

        (int x, int y, int z) pos = (
            this.Pos.AsBlockPos.X,
            this.Pos.AsBlockPos.Y,
            this.Pos.AsBlockPos.Z
        );

        if (visitedCoords.Contains(pos)) return;

        if (this.physicsSimulation?.Simulation == null) return;
        if (this.Api?.World?.BlockAccessor == null) return;
        if (this.Pos?.AsBlockPos == null) return;

        var simulation = this.physicsSimulation.Simulation;

        var body = this
            .physicsSimulation
            .Simulation.Bodies.GetBodyReference(this.physicsBodyHandle);

        var shape = this.physicsSimulation.Simulation.Shapes.GetShape<Box>(body.Collidable.Shape.Index);

        int range = (int)Math.Ceiling(
            Math.Max(shape.Width, Math.Max(shape.Height, shape.Length))
        ) + 1;

        BlockPos minPos = new BlockPos(
            pos.x - range,
            pos.y - range,
            pos.z - range
        );
        BlockPos maxPos = new BlockPos(
            pos.x + range,
            pos.y + range,
            pos.z + range
        );

        int blocksAdded = 0;

        for (int x = minPos.X; x <= maxPos.X; x++)
        {
            for (int y = minPos.Y; y <= maxPos.Y; y++)
            {
                for (int z = minPos.Z; z <= maxPos.Z; z++)
                {
                    BlockPos blkPos = new BlockPos(x, y, z);
                    if (this.Api == null) return;

                    Block? block = this.Api.World.BlockAccessor.GetBlock(blkPos);

                    if (block == null || block.Id == 0) continue;

                    var staticBox = new Box(1f, 1f, 1f);
                    var staticBoxIndex = simulation.Shapes.Add(staticBox);
                    // bepu boxes are centered, block positions are corners
                    var staticPose = new RigidPose(
                        new System.Numerics.Vector3(x + 0.5f, y + 0.5f, z + 0.5f)
                    );
                    var staticDescription = new StaticDescription(staticPose, staticBoxIndex);

                    var staticHandle = simulation.Statics.Add(staticDescription);
                    this.nearbyStaticHandles.Add(staticHandle);
                    blocksAdded++;
                }
            }
        }

        this.visitedCoords.Add(pos);
    }

    private int updateCounter = 0;

    private void UpdatePhysics(float deltaTime)
    {
        if (!physicsInitialized || physicsSimulation?.Simulation == null) return;

        this.DetectAndAddNearbyBlocks();

        var simulation = physicsSimulation.Simulation;
        var bodyRef = simulation.Bodies.GetBodyReference(physicsBodyHandle);
        var pose = bodyRef.Pose;
        var velocity = bodyRef.Velocity;
        var angularVel = bodyRef.Velocity.Angular;

        // we treat Pos as the bottom center of the current bounding box, the
        // same "feet" any other entity has, so the hitbox and anything else
        // the game puts at Pos lines up however the mechanism is rotated.

        // we compute the min and max of the bounding box using the following
        // method which takes all the shapes of the simulation and looks up the
        // ones it needs using the indices stored in bodyShape
        this.bodyShape!.Value.ComputeBounds(
            pose.Orientation, simulation.Shapes, out var min, out var max
        );

        this.ServerPos.X = pose.Position.X + (min.X + max.X) / 2;
        this.ServerPos.Y = pose.Position.Y + min.Y;
        this.ServerPos.Z = pose.Position.Z + (min.Z + max.Z) / 2;

        this.Pos.SetPos(this.ServerPos);

        // the renderer draws from this, and a reload restores the body from it
        this.StoreBodyPose(pose.Position, pose.Orientation);
    }

    private void CleanupPhysics()
    {
        if (!physicsInitialized || physicsSimulation == null) return;

        var simulation = physicsSimulation.Simulation;

        foreach (var handle in nearbyStaticHandles)
        {
            simulation.Statics.Remove(handle);
        }
        nearbyStaticHandles.Clear();

        if (simulation.Bodies.BodyExists(physicsBodyHandle))
        {
            simulation.Bodies.Remove(physicsBodyHandle);
        }

        physicsInitialized = false;
    }
}
