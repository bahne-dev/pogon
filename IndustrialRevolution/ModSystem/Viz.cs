using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace IndustrialRevolution;

public enum VizLayer
{
    Game,
    Bepu,
    Both,
}

public partial class IndustrialRevolutionModSystem
{
    // NOTE: defaults to one layer because both turns into a z-fight
    public VizLayer ActiveViz { get; private set; } = VizLayer.Game;

    public bool ShowBepuBodies => this.ActiveViz != VizLayer.Game;
    public bool ShowGameMeshes => this.ActiveViz != VizLayer.Bepu;

    public bool ShowHitboxes { get; private set; }
    public bool ShowAabbs { get; private set; }

    // NOTE: this is a client command, so it uses the . prefix not /
    private void RegisterVizCommand(ICoreClientAPI capi)
    {
        var vizCmd = capi.ChatCommands.Create("viz")
            .WithDescription("Choose which mechanism render layers are shown");

        vizCmd.BeginSubCommand("bepu")
            .WithDescription("Show only the physics body debug renderer")
            .HandleWith(_ => SetViz(VizLayer.Bepu))
            .EndSubCommand();

        vizCmd.BeginSubCommand("game")
            .WithDescription("Show only the block mesh renderer")
            .HandleWith(_ => SetViz(VizLayer.Game))
            .EndSubCommand();

        vizCmd.BeginSubCommand("both")
            .WithDescription("Show both layers (they z-fight)")
            .HandleWith(_ => SetViz(VizLayer.Both))
            .EndSubCommand();

        vizCmd.BeginSubCommand("hitbox")
            .WithDescription("Toggle mechanism selection box wireframes")
            .HandleWith(_ =>
            {
                this.ShowHitboxes = !this.ShowHitboxes;
                return ToggleResult("hitbox", this.ShowHitboxes);
            })
            .EndSubCommand();

        vizCmd.BeginSubCommand("aabb")
            .WithDescription("Toggle physics body bounding box wireframes")
            .HandleWith(_ =>
            {
                this.ShowAabbs = !this.ShowAabbs;
                return ToggleResult("aabb", this.ShowAabbs);
            })
            .EndSubCommand();
    }

    private static TextCommandResult ToggleResult(string layer, bool on) =>
        TextCommandResult.Success($"viz {layer}: {(on ? "on" : "off")}");

    private TextCommandResult SetViz(VizLayer layer)
    {
        this.ActiveViz = layer;
        string name = layer.ToString().ToLowerInvariant();
        return TextCommandResult.Success($"viz: {name}");
    }
}
