using Robust.Shared.GameStates;

namespace Content.Shared.CMU14.ZLevels.Core.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CMUZLevelStairsComponent : Component
{
    [DataField, AutoNetworkedField]
    public Direction Direction = Direction.North;

    [DataField, AutoNetworkedField]
    public int Offset = 1;

    [DataField, AutoNetworkedField]
    public float LandingLocalPosition = 0.05f;
}
