// SPDX-License-Identifier: AGPL-3.0-only
using Content.Shared.Construction;
using Content.Shared.Construction.Conditions;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Shared.CMU14.ZLevelBuilding;

[UsedImplicitly]
[DataDefinition]
public sealed partial class ZBuildAllowed : IConstructionCondition
{
    public bool Condition(EntityUid user, EntityCoordinates location, Direction direction)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        if (!entMan.TryGetComponent(location.EntityId, out TransformComponent? xform) || xform.MapUid is not { } mapUid)
            return true;
        if (entMan.HasComponent<VehicleInteriorLinkComponent>(mapUid))
            return false;
        return !entMan.TryGetComponent(mapUid, out ZBuildableMapComponent? zb) || zb.Enabled;
    }
    public ConstructionGuideEntry GenerateGuideEntry()
    {
        return new ConstructionGuideEntry { Localization = "construction-step-condition-au14-zbuild-allowed" };
    }
}
