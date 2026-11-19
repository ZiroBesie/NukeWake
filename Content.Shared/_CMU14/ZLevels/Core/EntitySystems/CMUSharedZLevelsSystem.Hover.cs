using System.Diagnostics.CodeAnalysis;
using Content.Shared.CMU14.ZLevels.Core.Components;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Shared.CMU14.ZLevels.Core.EntitySystems;

public abstract partial class CMUSharedZLevelsSystem
{
    private const float HoverArrivalEpsilon = 0.01f;

    private EntityQuery<CMUZHoverComponent> _hoverQuery;

    private void InitHover()
    {
        _hoverQuery = GetEntityQuery<CMUZHoverComponent>();
        SubscribeLocalEvent<CMUZHoverComponent, ComponentRemove>(OnHoverRemove);
    }

    private void OnHoverRemove(Entity<CMUZHoverComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.Source is not { } source || TerminatingOrDeleted(source))
            return;

        var ev = new CMUZHoverEndedEvent(ent.Owner);
        RaiseLocalEvent(source, ref ev);
    }

    [PublicAPI]
    public bool IsZHovering(EntityUid uid) => _hoverQuery.HasComp(uid);

    [PublicAPI]
    public bool CanStartZHover(EntityUid uid, [NotNullWhen(true)] out EntityUid? targetMap)
    {
        targetMap = null;
        if (!HasComp<CMUZPhysicsComponent>(uid) ||
            !HasComp<PhysicsComponent>(uid) ||
            _hoverQuery.HasComp(uid))
        {
            return false;
        }

        var xform = Transform(uid);
        if (!IsZPhysicsParent(xform) ||
            xform.MapUid is not { } mapUid ||
            !_zMapQuery.TryComp(mapUid, out var zMap) ||
            !TryMapUp((mapUid, zMap), out var above) ||
            HasTileAbove(uid, (mapUid, zMap)))
        {
            return false;
        }

        targetMap = above.Value.Owner;
        return true;
    }

    [PublicAPI]
    public void StartZHover(EntityUid uid, EntityUid targetMap, EntityUid? source, float ascendSpeed, float descendSpeed)
    {
        if (_net.IsClient)
            return;

        var hover = EnsureComp<CMUZHoverComponent>(uid);
        hover.TargetMap = targetMap;
        hover.Source = source;
        hover.AscendSpeed = ascendSpeed;
        hover.DescendSpeed = MathF.Min(descendSpeed, ImpactVelocityLimit * 0.9f);
        hover.Descending = false;

        if (TryComp<PhysicsComponent>(uid, out var physics) &&
            physics.BodyStatus != BodyStatus.OnGround &&
            !HasComp<ThrownItemComponent>(uid))
            _physics.SetBodyStatus(uid, physics, BodyStatus.OnGround);

        EnsureComp<CMUZFallingComponent>(uid);
    }

    [PublicAPI]
    public void StopZHover(EntityUid uid)
    {
        if (_net.IsClient || !_hoverQuery.TryComp(uid, out var hover))
            return;

        hover.Descending = true;
        EnsureComp<CMUZFallingComponent>(uid);
    }

    [PublicAPI]
    public void StartZDescent(EntityUid uid, EntityUid source, float descendSpeed)
    {
        if (_net.IsClient || Transform(uid).MapUid is not { } map)
            return;

        StartZHover(uid, map, source, descendSpeed, descendSpeed);
        StopZHover(uid);
    }

    [PublicAPI]
    public void CancelZHover(EntityUid uid, EntityUid source)
    {
        if (_net.IsClient || !_hoverQuery.TryComp(uid, out var hover) || hover.Source != source)
            return;

        RemComp<CMUZHoverComponent>(uid);
        WakeZPhysics(uid);
    }

    private void UpdateZHover(
        EntityUid uid,
        CMUZPhysicsComponent zPhys,
        CMUZHoverComponent hover,
        TransformComponent xform,
        PhysicsComponent physics,
        float frameTime)
    {
        var oldVelocity = zPhys.Velocity;
        var oldHeight = zPhys.LocalPosition;

        var distanceToGround = DistanceToGround((uid, zPhys), out _);
        var groundLocal = zPhys.LocalPosition - distanceToGround;

        float target;
        if (hover.Descending)
        {
            target = groundLocal;
        }
        else
        {
            if (xform.MapUid == hover.TargetMap)
                target = hover.HoverLocalPosition;
            else if (xform.MapUid is { } mapUid &&
                     TryMapUp(mapUid, out var above) &&
                     above.Value.Owner == hover.TargetMap)
                target = 1f + hover.HoverLocalPosition;
            else
                target = groundLocal;

            target = MathF.Max(target, groundLocal);
        }

        var delta = target - zPhys.LocalPosition;
        var speed = delta >= 0f ? hover.AscendSpeed : hover.DescendSpeed;
        var step = speed * frameTime;
        if (MathF.Abs(delta) <= step)
        {
            zPhys.LocalPosition = target;
            zPhys.Velocity = 0f;
        }
        else
        {
            zPhys.Velocity = MathF.Sign(delta) * speed;
            zPhys.LocalPosition += zPhys.Velocity * frameTime;
        }

        if (physics.BodyStatus != BodyStatus.OnGround && !HasComp<ThrownItemComponent>(uid))
            _physics.SetBodyStatus(uid, physics, BodyStatus.OnGround);

        if (zPhys.LocalPosition >= 1f || ShouldProcessDownBoundary(zPhys.LocalPosition))
            TryProcessZLevelBoundary(uid, zPhys, stickyGround: true);

        if (hover.Descending &&
            MathF.Abs(DistanceToGround((uid, zPhys), out _)) <= HoverArrivalEpsilon)
        {
            zPhys.Velocity = 0f;
            DirtyZPhysics(uid, zPhys, oldVelocity, oldHeight);
            RemComp<CMUZHoverComponent>(uid);
            return;
        }

        EnsureComp<CMUZFallingComponent>(uid);
        DirtyZPhysics(uid, zPhys, oldVelocity, oldHeight);
    }
}
