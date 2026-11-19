using Content.Shared.CMU14.ZLevels.Core;
using Content.Server.CMU14.ZLevelBuilding;
using Content.Shared.CMU14.ZLevelBuilding;
using Content.Shared.CMU14.ZLevelBuilding.Pit;
using Content.Shared.CMU14.ZLevels.Core.Components;
using Content.Shared._RMC14.Ladder;
using Content.Shared.Atmos;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Light.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Server.CMU14.ZLevels.Core;

public sealed partial class CMUDeployableZLevelLadderSystem : EntitySystem
{
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ITileDefinitionManager _tile = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private CMUZLevelsSystem _zLevels = default!;
    [Dependency] private ZLevelBuildingSystem _building = default!;

    private static readonly TimeSpan SupportCheckInterval = TimeSpan.FromSeconds(1);

    private readonly HashSet<EntityUid> _nearbyZPhysics = new();
    private TimeSpan _nextSupportCheck;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CMUDeployableZLevelLadderComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<CMUDeployedZLevelLadderComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerbs);
        SubscribeLocalEvent<CMUDeployedZLevelLadderComponent, TimedDespawnEvent>(OnTimedDespawn);
        SubscribeLocalEvent<CMUDeployedZLevelLadderComponent, ComponentRemove>(OnDeployedLadderRemove);
        SubscribeLocalEvent<CMUDeployedZLevelLadderComponent, EntityTerminatingEvent>(OnDeployedLadderRemove);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        ShakeUnsupportedLadders();

        if (_timing.CurTime < _nextSupportCheck)
            return;

        _nextSupportCheck = _timing.CurTime + SupportCheckInterval;

        var query = EntityQueryEnumerator<CMUDeployedZLevelLadderComponent>();
        while (query.MoveNext(out var uid, out var deployed))
        {
            if (!deployed.Retractable ||
                deployed.UnsupportedCollapse ||
                HasRequiredSupport((uid, deployed)))
            {
                continue;
            }

            ScheduleUnsupportedCollapse((uid, deployed));
        }
    }

    private void OnUseInHand(Entity<CMUDeployableZLevelLadderComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        TryDeploy(ent, args.User);
    }

    private void OnGetAlternativeVerbs(Entity<CMUDeployableZLevelLadderComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !ent.Comp.Retractable)
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("cmu-zlevel-ladder-retract"),
            Act = () => TryRetract(ent, user),
            Priority = 90,
        });
    }

    private bool TryDeploy(Entity<CMUDeployableZLevelLadderComponent> ent, EntityUid user)
    {
        if (!TryGetDeployment(ent, user, out var deployment, out var popup))
        {
            if (popup is { } p)
                _popup.PopupEntity(p, ent.Owner, user);
            return false;
        }

        var lower = deployment.LowerCoordinates;
        var upper = deployment.UpperCoordinates;

        var lowerLadder = Spawn(deployment.LowerPrototype, lower);
        var upperLadder = Spawn(deployment.UpperPrototype, upper);

        _transform.AnchorEntity(lowerLadder);
        _transform.AnchorEntity(upperLadder);

        SetDeployedLadderData(
            lowerLadder,
            upperLadder,
            deployment.Packed,
            deployment.Retractable,
            deployment.SupportCollisionMask,
            deployment.UnsupportedCollapseDelay,
            deployment.ReturnPackedOnUnsupportedCollapse,
            deployment.UnsupportedShakeDegrees,
            deployment.UnsupportedShakeInterval);

        SetDeployedLadderData(
            upperLadder,
            lowerLadder,
            deployment.Packed,
            deployment.Retractable,
            deployment.SupportCollisionMask,
            deployment.UnsupportedCollapseDelay,
            deployment.ReturnPackedOnUnsupportedCollapse,
            deployment.UnsupportedShakeDegrees,
            deployment.UnsupportedShakeInterval);

        QueueDel(ent.Owner);
        return true;
    }

    private bool TryGetDeployment(Entity<CMUDeployableZLevelLadderComponent> ent, EntityUid user,
        out LadderDeployment deployment, out string? popup)
    {
        deployment = default;
        popup = null;

        if (!TryComp<TransformComponent>(ent.Owner, out var xform) ||
            xform.GridUid is not { } gridUid ||
            !TryComp<MapGridComponent>(gridUid, out var grid))
        {
            popup = "cmu-zlevel-ladder-no-grid";
            return false;
        }

        var tile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        var localBounds = new Box2(tile * grid.TileSize, (tile + Vector2i.One) * grid.TileSize);
        var bounds = _transform.GetWorldMatrix(gridUid).TransformBox(localBounds);

        var up = new EntityCoordinates(gridUid, bounds.Center + new Vector2(0, -grid.TileSize.Y / 2));
        var down = new EntityCoordinates(gridUid, bounds.Center + new Vector2(0, grid.TileSize.Y / 2));

        if (!_zLevels.TryProjectToZMap((gridUid, null), 1, bounds.Center, out var upMap, out _) ||
            !_zLevels.TryProjectToZMap((gridUid, null), -1, bounds.Center, out var downMap, out _))
        {
            popup = "cmu-zlevel-ladder-no-levels";
            return false;
        }

        deployment = new LadderDeployment(
            up,
            down,
            (gridUid, grid),
            (gridUid, grid),
            tile);

        return true;
    }

    private void TryRetract(Entity<CMUDeployableZLevelLadderComponent> ent, EntityUid user)
    {
    }

    private void OnTimedDespawn(Entity<CMUDeployedZLevelLadderComponent> ent, ref TimedDespawnEvent args)
    {
        if (ent.Comp.OtherLadder is { } other && Exists(other))
            QueueDel(other);

        QueueDel(ent.Owner);
    }

    private void OnDeployedLadderRemove(Entity<CMUDeployedZLevelLadderComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.OtherLadder is { } other && Exists(other))
            QueueDel(other);
    }

    private void OnDeployedLadderRemove(Entity<CMUDeployedZLevelLadderComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.OtherLadder is { } other && Exists(other))
            QueueDel(other);
    }

    private bool HasRequiredSupport((EntityUid Uid, CMUDeployedZLevelLadderComponent Comp) ladder)
    {
        var (uid, comp) = ladder;
        var xform = Transform(uid);
        if (xform.GridUid is not { } gridUid ||
            !TryComp<MapGridComponent>(gridUid, out var grid) ||
            !TryComp<CMUZLevelMapComponent>(xform.MapUid, out var level))
        {
            return false;
        }

        var tile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        var below = new MapCoordinates(gridUid, xform.Coordinates.Position + new Vector2(0, grid.TileSize.Y));
        var above = new MapCoordinates(gridUid, xform.Coordinates.Position - new Vector2(0, grid.TileSize.Y));

        if (level.MapBelow is { } belowMap &&
            _map.TryGetTileRef(below, out var belowTile) &&
            !CMUZLevelOpeningCache.IsFallThroughTile(belowTile.Value.Tile, _tile))
        {
            return true;
        }

        if (level.MapAbove is { } aboveMap &&
            _map.TryGetTileRef(above, out var aboveTile) &&
            !CMUZLevelOpeningCache.IsFallThroughTile(aboveTile.Value.Tile, _tile))
        {
            return true;
        }

        var anchored = _map.GetAnchoredEntities(gridUid, grid, localBounds: new Box2(
            tile * grid.TileSize, (tile + Vector2i.One) * grid.TileSize));
        while (anchored.MoveNext(out var anchorUid))
        {
            if (anchorUid == uid || anchorUid == comp.OtherLadder)
                continue;

            if (HasComp<CMUDeployedZLevelLadderComponent>(anchorUid) ||
                HasComp<CMUZLevelHighGroundComponent>(anchorUid))
            {
                return true;
            }
        }

        return false;
    }

    private void ScheduleUnsupportedCollapse((EntityUid Uid, CMUDeployedZLevelLadderComponent Comp) ladder)
    {
        var (uid, comp) = ladder;
        comp.UnsupportedCollapse = true;
        Timer.Spawn(TimeSpan.FromSeconds(comp.UnsupportedCollapseDelay), () =>
        {
            if (!Exists(uid) || !TryComp<CMUDeployedZLevelLadderComponent>(uid, out var c) || !c.UnsupportedCollapse)
                return;
            CollapseUnsupportedLadder((uid, c));
        });
    }

    private void ShakeUnsupportedLadders()
    {
        var query = EntityQueryEnumerator<CMUDeployedZLevelLadderComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.UnsupportedCollapse || comp.UnsupportedShakeDegrees <= 0)
                continue;

            ShakeUnsupportedLadder((uid, comp));
        }
    }

    private void ShakeUnsupportedLadder(Entity<CMUDeployedZLevelLadderComponent> ent)
    {
        if (_timing.CurTime < ent.Comp.NextUnsupportedShake)
            return;

        ent.Comp.NextUnsupportedShake = _timing.CurTime +
                                        TimeSpan.FromSeconds(Math.Max(0.05f, ent.Comp.UnsupportedShakeInterval));
        ent.Comp.UnsupportedShakePositive = !ent.Comp.UnsupportedShakePositive;

        var degrees = MathF.Abs(ent.Comp.UnsupportedShakeDegrees);
        if (degrees <= 0)
            return;

        var offset = ent.Comp.UnsupportedShakePositive ? degrees : -degrees;
        _transform.SetLocalRotation(ent.Owner, ent.Comp.UnsupportedOriginalRotation + Angle.FromDegrees(offset));
    }

    private void CollapseUnsupportedLadder(Entity<CMUDeployedZLevelLadderComponent> ent)
    {
        RemoveHighGroundSupport(ent.Owner);
        WakeZPhysicsNear(ent.Owner);

        if (ent.Comp.ReturnPackedOnUnsupportedCollapse)
            SpawnAtPosition(ent.Comp.PackedPrototype, Transform(ent.Owner).Coordinates);

        if (ent.Comp.OtherLadder is { } other &&
            Exists(other))
        {
            RemoveHighGroundSupport(other);
            WakeZPhysicsNear(other);

            if (!EntityManager.IsQueuedForDeletion(other))
                QueueDel(other);
        }

        _popup.PopupEntity(Loc.GetString("cmu-zlevel-ladder-collapse"), ent.Owner, PopupType.MediumCaution);
    }

    private void RemoveHighGroundSupport(EntityUid uid)
    {
        if (HasComp<CMUZLevelHighGroundComponent>(uid))
            RemComp<CMUZLevelHighGroundComponent>(uid);
    }

    private void WakeZPhysicsNear(EntityUid uid)
    {
        var xform = Transform(uid);
        if (xform.MapID == MapId.Nullspace)
            return;

        _nearbyZPhysics.Clear();
        _lookup.GetEntitiesInRange(
            xform.MapID,
            _transform.GetWorldPosition(uid),
            0.75f,
            _nearbyZPhysics,
            LookupFlags.Dynamic | LookupFlags.Sundries | LookupFlags.Uncontained);

        foreach (var nearby in _nearbyZPhysics)
        {
            if (TryComp<CMUZPhysicsComponent>(nearby, out var zPhysics))
                _zLevels.WakeZPhysics((nearby, zPhysics));
        }

        _nearbyZPhysics.Clear();
    }

    private void PrepareSpawnedLadder(EntityUid ladder, EntityCoordinates coordinates)
    {
        _transform.SetCoordinates(ladder, coordinates);
        _transform.SetLocalRotation(ladder, Angle.Zero);

        var xform = Transform(ladder);
        if (!xform.Anchored)
            _transform.AnchorEntity((ladder, xform));
    }

    private Entity<CMUDeployedZLevelLadderComponent> SetDeployedLadderData(
        EntityUid ladder,
        EntityUid otherLadder,
        EntProtoId packed,
        bool retractable,
        CollisionGroup supportCollisionMask,
        float unsupportedCollapseDelay,
        bool returnPackedOnUnsupportedCollapse,
        float unsupportedShakeDegrees,
        float unsupportedShakeInterval)
    {
        var deployed = EnsureComp<CMUDeployedZLevelLadderComponent>(ladder);
        deployed.OtherLadder = otherLadder;
        deployed.PackedPrototype = packed;
        deployed.Retractable = retractable;
        deployed.SupportCollisionMask = supportCollisionMask;
        deployed.UnsupportedCollapseDelay = unsupportedCollapseDelay;
        deployed.ReturnPackedOnUnsupportedCollapse = returnPackedOnUnsupportedCollapse;
        deployed.UnsupportedShakeDegrees = unsupportedShakeDegrees;
        deployed.UnsupportedShakeInterval = unsupportedShakeInterval;
        return (ladder, deployed);
    }

    private readonly record struct LadderDeployment(
        EntityCoordinates LowerCoordinates,
        EntityCoordinates UpperCoordinates,
        Entity<MapGridComponent> LowerGrid,
        Entity<MapGridComponent> UpperGrid,
        Vector2i Tile);
}