using Robust.Server.Entity;
using Robust.Server.WorldState;
using YamlDotNet.Serialization;

namespace NukeWake.Sources.Structures;

/// <summary>
/// Entity component for TileFloorSupport - floor support structure entity
/// </summary>
[YamlMember(
    SerializeAs = typeof(YamlReference),
    Name = "TileFloorSupport"
)]
public sealed class TileFloorSupportComponent : EntityComponentBase
{
}

/// <summary>
/// Entity component for StructuralSupport - wall/column support structure entity
/// </summary>
[YamlMember(
    SerializeAs = typeof(YamlReference),
    Name = "StructuralSupport"
)]
public sealed class StructuralSupportComponent : EntityComponentBase
{
}

/// <summary>
/// Entity component for StructuralScanner - scanner used to detect structural components
/// </summary>
[YamlMember(
    SerializeAs = typeof(YamlReference),
    Name = "StructuralScanner"
)]
public sealed class StructuralScannerComponent : EntityComponentBase
{
}

/// <summary>
/// Entity component for PhysicsMap - physics mapping data entity
/// </summary>
[YamlMember(
    SerializeAs = typeof(YamlReference),
    Name = "PhysicsMap"
)]
public sealed class PhysicsMapComponent : EntityComponentBase
{
}

/// <summary>
/// Entity component for MovedGrids - grid movement state entity
/// </summary>
[YamlMember(
    SerializeAs = typeof(YamlReference),
    Name = "MovedGrids"
)]
public sealed class MovedGridsComponent : EntityComponentBase
{
}
