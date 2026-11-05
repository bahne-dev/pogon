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
    // NOTE: defaults to one layer because the two draw coplanar cubes and z-fight
    public VizLayer ActiveViz { get; private set; } = VizLayer.Game;

    public bool ShowBepuBodies => this.ActiveViz != VizLayer.Game;
    public bool ShowGameMeshes => this.ActiveViz != VizLayer.Bepu;

    // NOTE: client commands use the "." prefix, so this is ".viz", not "/viz"
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
    }

    private TextCommandResult SetViz(VizLayer layer)
    {
        this.ActiveViz = layer;
        string name = layer.ToString().ToLowerInvariant();
        return TextCommandResult.Success($"viz: {name}");
    }
}
