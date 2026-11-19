using Robust.Shared.GameStates;

namespace Content.Shared.CMU14.ZLevels.Core.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CMUZPairedComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Offset;

    [DataField]
    public string PairKind = string.Empty;

    [ViewVariables, AutoNetworkedField]
    public EntityUid? Twin;
}
