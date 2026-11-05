using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace IndustrialRevolution;

public partial class IndustrialRevolutionModSystem
{
    public bool ShowBepuBodies { get; private set; } = true;
    public bool ShowGameMeshes { get; private set; } = true;

    // NOTE: this is a client command, so it uses the . prefix not /
    private void RegisterVizCommand(ICoreClientAPI capi)
    {
        var vizCmd = capi.ChatCommands.Create("viz")
            .WithDescription("Toggle mechanism render layers");

        vizCmd.BeginSubCommand("bepu")
            .WithDescription("Toggle the physics body debug renderer")
            .HandleWith(_ =>
            {
                this.ShowBepuBodies = !this.ShowBepuBodies;
                return VizResult("bepu", this.ShowBepuBodies);
            })
            .EndSubCommand();

        vizCmd.BeginSubCommand("game")
            .WithDescription("Toggle the block mesh renderer")
            .HandleWith(_ =>
            {
                this.ShowGameMeshes = !this.ShowGameMeshes;
                return VizResult("game", this.ShowGameMeshes);
            })
            .EndSubCommand();
    }

    private static TextCommandResult VizResult(string layer, bool enabled) =>
        TextCommandResult.Success($"viz {layer}: {(enabled ? "on" : "off")}");
}
