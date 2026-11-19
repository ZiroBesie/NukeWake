using Content.Shared.CMU14.ZLevels.Core.Components;
using Robust.Shared.Map;

namespace Content.Shared.CMU14.ZLevels.Core.EntitySystems;

public abstract partial class CMUSharedZLevelsSystem
{
    public bool TryGetOverheadEntityProjection(EntityUid uid, EntityUid viewingMap,
        out MapCoordinates coordinates, out float height)
    {
        coordinates = default;
        height = default;
        if (!HasComp<CMUZFallingComponent>(uid) ||
            !TryComp<CMUZPhysicsComponent>(uid, out var physics) ||
            !TryComp(uid, out TransformComponent? xform) ||
            xform.Anchored || !IsZPhysicsParent(xform) ||
            xform.MapUid is not { } sourceMap ||
            !TryGetZNetwork(sourceMap, out var network) ||
            !IsMapInNetwork(network.Value, viewingMap) ||
            !_zMapQuery.TryComp(sourceMap, out var sourceZ) ||
            !_zMapQuery.TryComp(viewingMap, out var targetZ) ||
            !_mapQuery.TryComp(viewingMap, out var targetMap))
        {
            return false;
        }

        var depth = (long) sourceZ.Depth - targetZ.Depth;
        if (depth <= 0 || depth > MaxZLevelsBelowRendering)
            return false;

        var position = _transform.GetWorldPosition(xform);
        var current = sourceMap;
        for (var i = 0; i < depth; i++)
        {
            if (_map.TryFindGridAt(current, position, out var gridUid, out var grid) &&
                !CMUZLevelOpeningCache.IsOpeningTile(gridUid, grid, position, _map, TilDefMan))
            {
                return false;
            }

            if (!TryMapDown(current, out var below))
                return false;

            current = below.Value.Owner;
        }

        if (current != viewingMap)
            return false;

        coordinates = new MapCoordinates(position, targetMap.MapId);
        height = (float) depth + physics.LocalPosition;
        return height > 0f;
    }
}
