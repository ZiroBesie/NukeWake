using System.Numerics;
using Content.Shared._CMU14.ZLevels.Core.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Shared._CMU14.ZLevels.Core.EntitySystems;

public abstract partial class CMUSharedZLevelsSystem
{
    /// <summary>
    /// Checks a prospective climb landing without moving the climber or anyone they are pulling.
    /// Uses the same floors, openings and elevated supports as falling physics.
    /// </summary>
    public bool HasClimbLanding(
        EntityUid climber,
        Entity<CMUZLevelMapComponent> map,
        Vector2 worldPosition,
        float localPosition)
    {
        var distance = QueryGroundDistance(climber, map, worldPosition, localPosition, velocity: 0f, out _, maxFloors: 1);
        if (distance < -MaxStepHeight || distance > GroundSnapDistance)
            return false;

        if (!HasComp<FixturesComponent>(climber))
            return true;

        // Test the destination before moving the climber; resolving an overlap after teleporting can trap them.
        var bounds = Box2.CenteredAround(worldPosition, Vector2.One);
        var intersecting = _lookup.GetEntitiesIntersecting(Transform(map.Owner).MapID, bounds);
        foreach (var blocker in intersecting)
        {
            if (blocker == climber ||
                !TryComp<FixturesComponent>(blocker, out var blockerFixtures))
            {
                continue;
            }

            foreach (var fixture in blockerFixtures.Fixtures.Values)
            {
                if (fixture.Hard)
                    return false;
            }
        }

        return true;
    }
}
