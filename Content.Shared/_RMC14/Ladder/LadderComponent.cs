using Robust.Shared.GameObjects;

namespace Content.Shared._RMC14.Ladder;

[RegisterComponent]
public sealed partial class LadderComponent : Component
{
    [DataField]
    public float Range = 1.5f;
}
