using System;
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;

namespace IndustrialRevolution.Entities.Mechanism.Physics;

/// callbacks for collision detection and physics integration
public struct PhysicsCallbacks : INarrowPhaseCallbacks
{
    public PairMaterialProperties MaterialProperties;

    public void Initialize(Simulation simulation)
    {
        MaterialProperties = new PairMaterialProperties
        {
            FrictionCoefficient = 0.5f,
            MaximumRecoveryVelocity = 2f,
            SpringSettings = new SpringSettings(30, 1)
        };
    }

    public bool AllowContactGeneration(
        int workerIndex,
        CollidableReference a, CollidableReference b,
        ref float speculativeMargin
    )
    {
        // allow all collisions
        return true;
    }

    public bool AllowContactGeneration(
        int workerIndex,
        CollidablePair pair,
        int childIndexA, int childIndexB
    )
    {
        // allow all child collisions (for compound shapes)
        return true;
    }

    public bool ConfigureContactManifold<TManifold>(
        int workerIndex,
        CollidablePair pair,
        ref TManifold manifold,
        out PairMaterialProperties pairMaterial
    )
        where TManifold : unmanaged, IContactManifold<TManifold>
    {
        pairMaterial = MaterialProperties;
        return true;
    }

    public bool ConfigureContactManifold(
        int workerIndex,
        CollidablePair pair,
        int childIndexA, int childIndexB,
        ref ConvexContactManifold manifold
    )
    {
        return true;
    }

    public void Dispose() { }
}

/// callbacks for velocity integration (gravity, damping, etc)
public struct PoseIntegratorCallbacks : IPoseIntegratorCallbacks
{
    public System.Numerics.Vector3 Gravity;
    public float LinearDamping;
    public float AngularDamping;

    public void Initialize(Simulation simulation)
    {
        Gravity = new System.Numerics.Vector3(0, -9.81f, 0);
        LinearDamping = 0.03f; // translation resistance
        AngularDamping = 0.03f; // rotation resistance
    }

    public readonly AngularIntegrationMode AngularIntegrationMode
        => AngularIntegrationMode.Nonconserving;
    public readonly bool AllowSubstepsForUnconstrainedBodies
        => false;
    public readonly bool IntegrateVelocityForKinematics
        => false;

    public void PrepareForIntegration(float dt) { }

    public void IntegrateVelocity(
        Vector<int> bodyIndices,
        Vector3Wide position,
        QuaternionWide orientation,
        BodyInertiaWide localInertia,
        Vector<int> integrationMask,
        int workerIndex,
        Vector<float> dt,
        ref BodyVelocityWide velocity
    )
    {
        // apply gravity
        var gravityDt = Vector.ConditionalSelect(
            integrationMask, dt, Vector<float>.Zero
        );
        velocity.Linear.X += new Vector<float>(Gravity.X) * gravityDt;
        velocity.Linear.Y += new Vector<float>(Gravity.Y) * gravityDt;
        velocity.Linear.Z += new Vector<float>(Gravity.Z) * gravityDt;

        var linearDampingLeft = new Vector<float>(
            MathF.Pow(MathF.Max(0, 1 - LinearDamping), dt[0])
        );

        // apply per-frame damping linear
        var linearDampingMultiplier = new Vector<float>(
            1.0f - LinearDamping * dt[0]
        );
        velocity.Linear.X *= linearDampingMultiplier;
        velocity.Linear.Y *= linearDampingMultiplier;
        velocity.Linear.Z *= linearDampingMultiplier;

        var angularDampingLeft = new Vector<float>(
            MathF.Pow(MathF.Max(0, 1 - AngularDamping), dt[0])
        );

        var angularDampingMultiplier = Vector.ConditionalSelect(
            integrationMask, angularDampingLeft, Vector<float>.One
        );
        velocity.Angular.X *= angularDampingMultiplier;
        velocity.Angular.Y *= angularDampingMultiplier;
        velocity.Angular.Z *= angularDampingMultiplier;
    }
}

/// physics source of truth
public class PhysicsSimulation
{
    private Simulation simulation;
    private BufferPool bufferPool;
    private PhysicsCallbacks narrowPhaseCallbacks;
    private PoseIntegratorCallbacks poseIntegratorCallbacks;

    public Simulation Simulation => simulation;

    public PhysicsSimulation()
    {
        // buffer pool for memory stuff
        bufferPool = new BufferPool();

        narrowPhaseCallbacks = new PhysicsCallbacks();
        poseIntegratorCallbacks = new PoseIntegratorCallbacks();

        simulation = Simulation.Create(
            bufferPool,
            narrowPhaseCallbacks, poseIntegratorCallbacks,
            new SolveDescription(8, 1)
        ); // 8 velocity iterations, 1 substep

        narrowPhaseCallbacks.Initialize(simulation);
        poseIntegratorCallbacks.Initialize(simulation);

        IndustrialRevolutionModSystem.Logger?.Info("PhysicsSimulation created successfully");
    }

    public void Step(float deltaTime)
    {
        simulation.Timestep(deltaTime);
    }

    public void Dispose()
    {
        simulation.Dispose();
        bufferPool.Clear();
    }
}
