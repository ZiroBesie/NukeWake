using Robust.Shared.GameStates;
using Robust.Shared.ViewVariables;
using Robust.Shared.Prototypes;

namespace Content.Shared._CMU14.ZLevelBuilding;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ZBuildableMapComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    [DataField]
    public int ChunkSize = 8;

    [DataField]
    public List<string> StoneFloorTiles = new() { "FloorBasalt" };

    [DataField]
    public string StoneRockEntity = "RUMCRockWall";

    [DataField]
    public string DefaultCollapseResource = "RUMCStoneItem";

    [DataField, AutoNetworkedField]
    public int MaxRoofSpan = 3;

    [DataField]
    public string RumbleSound = "/Audio/Effects/explosion3.ogg";
}
