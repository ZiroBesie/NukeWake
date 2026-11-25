using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared._CMU14.Dropship.MultiDeck;
using Content.Shared._CMU14.ZLevels.Core.Components;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._CMU14.ZLevels.Core.EntitySystems;

// Compatibility helpers for the newer z-level partials ported from upstream that
// expect a slightly newer shared z-level core API.
public abstract partial class CMUSharedZLevelsSystem
{
    private const float GroundSnapDistance = 0.05f;
    private const float ZPhysicsSleepDistance = 0.05f;

    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;

    [PublicAPI]
    public bool IsMapInNetwork(Entity<CMUZLevelsNetworkComponent> network, EntityUid mapUid)
        => TryGetZNetwork(mapUid, out var memberNetwork) && memberNetwork.Value.Owner == network.Owner;

    protected bool IsZPhysicsParent(TransformComponent xform)
        => xform.ParentUid == xform.MapUid || HasComp<DropshipDeckComponent>(xform.ParentUid);

    private static bool ShouldProcessDownBoundary(float localPosition)
        => localPosition < -ZPhysicsSleepDistance;

    private static bool CanReachHighGroundSupport(Vector2 local)
    {
        const float reach = HighGroundEdgeSupport + 0.000001f;
        return !(local.X < -reach || local.X > 1f + reach ||
                 local.Y < -reach || local.Y > 1f + reach);
    }

    private Direction GetHighGroundGridDirection(EntityUid highGround)
        => _transform.GetWorldRotation(highGround).GetCardinalDir();

    protected bool TryResolveMovementGrid(
        EntityUid mapUid,
        Vector2 worldPosition,
        out EntityUid gridUid,
        [NotNullWhen(true)] out MapGridComponent? grid)
    {
        if (_gridQuery.TryComp(mapUid, out grid))
        {
            gridUid = mapUid;
            return true;
        }

        if (TryComp(mapUid, out TransformComponent? mapXform))
        {
            var mapId = mapXform.MapID;
            if (_mapManager.TryFindGridAt(mapId, worldPosition, out gridUid, out grid))
                return true;

            var children = mapXform.ChildEnumerator;
            while (children.MoveNext(out var child))
            {
                if (!_gridQuery.TryComp(child, out grid))
                    continue;

                gridUid = child;
                return true;
            }
        }

        gridUid = default;
        grid = null;
        return false;
    }

    /// <summary>
    /// Returns the distance to the nearest ground below the given entity.
    /// Kept for callers ported from the newer core that pass an explicit map and position.
    /// </summary>
    private float QueryGroundDistance(
        EntityUid target,
        EntityUid? mapUid,
        Vector2 worldPos,
        float localPosition,
        float velocity,
        out bool stickyGround,
        int maxFloors)
    {
        stickyGround = false;

        if (!TryComp<CMUZPhysicsComponent>(target, out var zPhys))
            return maxFloors;

        // The destination map differs from the entity's current map: the local height
        // already encodes the distance to the destination floor.
        if (mapUid is { } map && Transform(target).MapUid != map)
            return localPosition;

        return DistanceToGround((target, zPhys), out stickyGround, maxFloors);
    }

    private bool HasCurrentTileHighGround(EntityUid uid)
    {
        var xform = Transform(uid);
        var tile = _transform.GetGridOrMapTilePosition(uid);
        if (xform.MapUid is not { } mapUid ||
            !TryGetZLevelGrid(mapUid, xform.GridUid, out var mapGrid))
        {
            return false;
        }

        var query = _map.GetAnchoredEntitiesEnumerator(mapUid, mapGrid, tile);
        while (query.MoveNext(out var anchoredUid))
        {
            if (_highgroundQuery.HasComp(anchoredUid))
                return true;
        }

        return false;
    }

    private void TryProcessZLevelBoundary(EntityUid uid, CMUZPhysicsComponent zPhys, bool stickyGround)
    {
        if (zPhys.LocalPosition < 0f && !ShouldProcessDownBoundary(zPhys.LocalPosition))
            return;

        if (zPhys.LocalPosition < 0) // We wanna fall down on the Z-level below.
        {
            if (CanProcessZLevelTransition(uid, -1))
            {
                var moved = TryMoveDownOrChasm(uid);
                if (moved)
                {
                    zPhys.LocalPosition += 1;

                    if (!stickyGround)
                    {
                        var fallEv = new CMUZLevelFallEvent();
                        RaiseLocalEvent(uid, fallEv);
                    }
                }
            }
            else
            {
                zPhys.LocalPosition = 0;
            }

            return;
        }

        if (zPhys.LocalPosition < 1)
            return;

        if (HasTileAbove(uid) && !HasCurrentTileHighGround(uid)) // Hit roof.
        {
            if (MathF.Abs(zPhys.Velocity) >= ImpactVelocityLimit)
            {
                RaiseLocalEvent(uid, new CMUZLevelHitEvent(MathF.Abs(zPhys.Velocity)));
                var land = new LandEvent(null, true);
                RaiseLocalEvent(uid, ref land);
            }

            zPhys.LocalPosition = 1;
            zPhys.Velocity = -zPhys.Velocity * zPhys.Bounciness;
            return;
        }

        if (CanProcessZLevelTransition(uid, 1))
        {
            if (TryMoveUp(uid))
                zPhys.LocalPosition -= 1;
        }
        else
        {
            zPhys.LocalPosition = 1;
        }
    }
}
