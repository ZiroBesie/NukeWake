// SPDX-License-Identifier: AGPL-3.0-only
using Robust.Shared.GameStates;

namespace Content.Shared.CMU14.ZLevelBuilding;

[RegisterComponent, NetworkedComponent]
public sealed partial class ZLevelWallSupportComponent : Component
{
    public const int CantileverSpan = 3;
}
