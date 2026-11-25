using Robust.Shared.Maths;

namespace Content.Shared._CMU14.ZLevelBuilding;

[RegisterComponent]
public sealed partial class ZStairComponent : Component
{
    [DataField]
    public int Direction = 1;

    [DataField]
    public Vector2i BeamOffset = new(0, 1);

    [DataField]
    public string ReflectBeam = "AU14NavalisSupportBeamBlue1Tile";

    [DataField]
    public string ReflectFloorTile = "Plating";

    [DataField]
    public int PlatformRadius = 1;

    [DataField]
    public string PartnerProto = "AU14ZStairPure";
}
