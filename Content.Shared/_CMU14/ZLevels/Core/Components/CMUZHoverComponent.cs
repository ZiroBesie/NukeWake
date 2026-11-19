using Content.Shared.CMU14.ZLevels.Core.EntitySystems;

namespace Content.Shared.CMU14.ZLevels.Core.Components;

[RegisterComponent, Access(typeof(CMUSharedZLevelsSystem))]
public sealed partial class CMUZHoverComponent : Component
{
    [DataField]
    public EntityUid TargetMap;

    [DataField]
    public float HoverLocalPosition;

    [DataField]
    public float AscendSpeed = 2.5f;

    [DataField]
    public float DescendSpeed = 1.5f;

    [DataField]
    public bool Descending;

    [DataField]
    public EntityUid? Source;
}

[ByRefEvent]
public readonly record struct CMUZHoverEndedEvent(EntityUid Hoverer);
