using Flax.Build;

public class AdvancedTerrainToolsTarget : GameProjectTarget
{
    /// <inheritdoc />
    public override void Init()
    {
        base.Init();

        // Reference the modules for game
        Modules.Add("AdvancedTerrainTools");
    }
}
