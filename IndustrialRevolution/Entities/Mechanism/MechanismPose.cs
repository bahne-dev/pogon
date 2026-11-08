using System.Numerics;

using Vintagestory.API.Datastructures;

namespace IndustrialRevolution.Entities.Mechanism;

/// The physics body's pose (center of mass position and rotation), kept in
/// one attribute tree so it's saved with the world and synced to clients in
/// one piece, position and rotation always arriving together
public partial class EntityMechanism
{
    private const string BodyPoseKey = "bodyPose";

    private void StoreBodyPose(Vector3 position, Quaternion rotation)
    {
        var wa = this.WatchedAttributes;
        ITreeAttribute tree = wa.GetOrAddTreeAttribute(BodyPoseKey);

        tree.SetDouble("x", position.X);
        tree.SetDouble("y", position.Y);
        tree.SetDouble("z", position.Z);

        tree.SetFloat("qx", rotation.X);
        tree.SetFloat("qy", rotation.Y);
        tree.SetFloat("qz", rotation.Z);
        tree.SetFloat("qw", rotation.W);

        this.WatchedAttributes.MarkPathDirty(BodyPoseKey);
    }

    /// False before the body has run its first tick (e.g. right after gluing)
    /// There's logic that uses this behavior.
    public bool TryGetBodyPose(
        out double x, out double y, out double z, out Quaternion rotation
    )
    {
        var wa = this.WatchedAttributes;
        ITreeAttribute? tree = wa.GetTreeAttribute(BodyPoseKey);

        if (tree == null)
        {
            x = y = z = 0;
            rotation = Quaternion.Identity;
            return false;
        }

        x = tree.GetDouble("x");
        y = tree.GetDouble("y");
        z = tree.GetDouble("z");

        rotation = new Quaternion(
            tree.GetFloat("qx"),
            tree.GetFloat("qy"),
            tree.GetFloat("qz"),
            tree.GetFloat("qw", 1f)
        );

        return true;
    }
}
