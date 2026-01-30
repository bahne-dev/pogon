using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Server;

using IndustrialRevolution.Entities.Mechanism.Physics;

using BepuPhysics;
using BepuPhysics.Collidables;

namespace IndustrialRevolution;

public partial class IndustrialRevolutionModSystem : ModSystem
{
    public PhysicsSimulation? PhysicsSimulation { get; private set; }
    private long physicsTickListenerId;

    private PhysicsDebugData? latestDebugData;

    private IServerNetworkChannel? serverChannel;
    private IClientNetworkChannel? clientChannel;

    private int activeBodies;

    // NOTE: this should be called on server side only
    private void StartPhysics(ICoreServerAPI sapi)
    {
        this.PhysicsSimulation = new PhysicsSimulation();
        Logger?.Info("Physics simulation initialized");

        physicsTickListenerId = sapi.Event.RegisterGameTickListener(OnPhysicsTick, 16); // ~60 FPS
    }

    private void OnClientReceiveDebugData(PhysicsDebugData data)
    {
        latestDebugData = data;
    }

    public PhysicsDebugData? GetLatestDebugData() => latestDebugData;

    private void RegisterClientChannel(ICoreClientAPI capi)
    {
        this.clientChannel = capi.Network
            .RegisterChannel("industrialrevolution.physics")
            .RegisterMessageType<PhysicsDebugData>()
            .SetMessageHandler<PhysicsDebugData>(OnClientReceiveDebugData);
    }

    private void RegisterServerChannel(ICoreServerAPI sapi)
    {
        this.serverChannel = sapi.Network
            .RegisterChannel("industrialrevolution.physics")
            .RegisterMessageType<PhysicsDebugData>();
    }

    private BodyDebugInfo SerializeBody(object bodyObjRef, Shapes shapes)
    {
        TypedIndex shapeIndex;
        RigidPose pose;

        switch (bodyObjRef) {
            case BodyReference bodyRef:
                shapeIndex = bodyRef.Collidable.Shape;
                pose = bodyRef.Pose;
                break;
            default:
                StaticReference staticRef = (StaticReference)bodyObjRef;
                shapeIndex = staticRef.Shape;
                pose = staticRef.Pose;
                break;
        }

        var shape = shapes.GetShape<Box>(shapeIndex.Index);
        var shapeTypeId = shapeIndex.Type;

        var debugBody = new BodyDebugInfo
        {
            PosX = pose.Position.X,
            PosY = pose.Position.Y,
            PosZ = pose.Position.Z,

            RotX = pose.Orientation.X,
            RotY = pose.Orientation.Y,
            RotZ = pose.Orientation.Z,
            RotW = pose.Orientation.W,
        };

        if (shapeTypeId == Box.Id)
        {
            var box = shapes.GetShape<Box>(shapeIndex.Index);
            debugBody.IsCompound = false;
            debugBody.Width = box.Width;
            debugBody.Height = box.Height;
            debugBody.Length = box.Length;
        }
        else if (shapeTypeId == Compound.Id)
        {
            var compound = shapes.GetShape<Compound>(shapeIndex.Index);
            debugBody.IsCompound = true;
            for (int j = 0; j < compound.Children.Length; j++)
            {
                var child = compound.Children[j];
                debugBody.Children.Add(new CompoundChildDebug
                {
                    LocalX = child.LocalPosition.X,
                    LocalY = child.LocalPosition.Y,
                    LocalZ = child.LocalPosition.Z
                });
            }
        }
        else if (shapeTypeId == BigCompound.Id)
        {
            var bigCompound = shapes.GetShape<BigCompound>(shapeIndex.Index);
            debugBody.IsCompound = true;
            for (int j = 0; j < bigCompound.Children.Length; j++)
            {
                var child = bigCompound.Children[j];
                debugBody.Children.Add(new CompoundChildDebug
                {
                    LocalX = child.LocalPosition.X,
                    LocalY = child.LocalPosition.Y,
                    LocalZ = child.LocalPosition.Z
                });
            }
        }

        return debugBody;
    }

    public PhysicsDebugData GatherDebugData()
    {
        var data = new PhysicsDebugData();

        var sim = this.PhysicsSimulation!.Simulation;

        var activeBodiesNow = sim.Bodies.ActiveSet.Count;

        if (this.activeBodies != activeBodiesNow)
        {
            IndustrialRevolutionModSystem.Logger.Debug(
                $"active bodies changed! from: {this.activeBodies} to {activeBodiesNow}"
            );
            this.activeBodies = activeBodiesNow;
        }

        for (int i = 0; i < activeBodies; i++)
        {
            var handle = sim.Bodies.ActiveSet.IndexToHandle[i];
            var bodyRef = sim.Bodies.GetBodyReference(handle);

            var shapeIndex = bodyRef.Collidable.Shape;

            var debugBody = this.SerializeBody(bodyRef, sim.Shapes);

            data.DynamicBodies.Add(debugBody);
        }

        for (int i = 0; i < sim.Statics.Count; i++)
        {
            var handle = sim.Statics.IndexToHandle[i];
            var staticRef = sim.Statics[handle];

            var shapeIndex = staticRef.Shape;
            var shape = sim.Shapes.GetShape<Box>(shapeIndex.Index);

            var debugStatic = SerializeBody(staticRef, sim.Shapes);

            data.StaticBodies.Add(debugStatic);
        }

        return data;
    }

    private void OnPhysicsTick(float dt)
    {
        if (PhysicsSimulation == null) return;

        PhysicsSimulation.Step(dt); // <--- DT IS IN *SECONDS*

        // send debug data every tick (~bajilion times per second)
        var debugData = this.GatherDebugData();

        if (this.serverChannel == null)
        {
            IndustrialRevolutionModSystem
                .Logger
                .LogUnwantedNull("server channel");
            return;
        }

        serverChannel.BroadcastPacket(debugData);
    }

    public override void Dispose()
    {
        base.Dispose();

        if (PhysicsSimulation != null)
        {
            PhysicsSimulation.Dispose();
            PhysicsSimulation = null;
        }

        if (sapi != null && physicsTickListenerId != 0)
        {
            sapi.Event.UnregisterGameTickListener(physicsTickListenerId);
        }
    }
}
