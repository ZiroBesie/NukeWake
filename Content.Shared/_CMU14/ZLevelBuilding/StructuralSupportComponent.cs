// SPDX-License-Identifier: AGPL-3.0-only
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.CMU14.ZLevelBuilding;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StructuralSupportComponent : Component
{
    [DataField, AutoNetworkedField]
    public int CantileverSpan = 2;
    [DataField, AutoNetworkedField]
    public bool IsAnchor;
    [DataField, AutoNetworkedField]
    public bool IsVerticalSupport;
    [DataField, AutoNetworkedField]
    public bool HideFromScanner;
    [ViewVariables]
    public bool Supported = true;
}
