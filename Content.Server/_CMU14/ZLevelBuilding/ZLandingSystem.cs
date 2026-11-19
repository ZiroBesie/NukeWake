// SPDX-License-Identifier: AGPL-3.0-only
using System.Numerics;
using Content.Shared.CMU14.ZLevelBuilding;
using Content.Shared.CMU14.ZLevels.Core.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;

namespace Content.Server.CMU14.ZLevelBuilding;

public sealed class ZLandingSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const int MaxDrop = 16;

    public EntityUid FindLandingMap(EntityUid map, Vector2 worldPos)
    {
        var current = map;
        for (var i = 0; i < MaxDrop; i++)
        {
            if (HasSolidTile(current, worldPos))
                return current;
            if (!TryComp<CMUZLevelMapComponent>(current, out var z) || z.MapBelow is not { } below || TerminatingOrDeleted(below))
                return current;
            current = below;
        }
        return current;
    }

    private readonly HashSet<Entity<ZFallenDebrisComponent>> _debris = new();
    public void GetFallenDebrisOnTile(EntityUid gridUid, MapGridComponent grid, Vector2i tile, List<EntityUid> result)
    {
        var center = _map.GridTileToWorld(gridUid, grid, tile);
        var box = Box2.CenteredAround(center.Position, new Vector2(grid.TileSize, grid.TileSize));
        _debris.Clear();
        _lookup.GetEntitiesIntersecting(center.MapId, box, _debris, LookupFlags.Uncontained | LookupFlags.Dynamic | LookupFlags.Static | LookupFlags.Sundries);
        foreach (var debris in _debris)
        {
            if (!TerminatingOrDeleted(debris) && box.Contains(_transform.GetWorldPosition(debris)))
                result.Add(debris.Owner);
        }
        _debris.Clear();
    }

    private bool HasSolidTile(EntityUid map, Vector2 worldPos)
    {
        if (!TryComp<MapComponent>(map, out var mapComp))
            return false;
        var coords = new MapCoordinates(worldPos, mapComp.MapId);
        if (!_map.TryFindGridAt(coords, out var gridUid, out var grid))
            return false;
        return !_map.GetTileRef(gridUid, grid, coords).Tile.IsEmpty;
    }

    public bool TryMarkFall(EntityUid ent, EntityUid? fromMap)
    {
        var debris = EnsureComp<ZFallenDebrisComponent>(ent);
        return fromMap is not { } map || debris.FellFrom.Add(map);
    }

    public bool LandsInsideSolid(EntityUid map, Vector2 worldPos)
    {
        if (!TryComp<MapComponent>(map, out var mapComp))
            return false;
        var coords = new MapCoordinates(worldPos, mapComp.MapId);
        if (!_map.TryFindGridAt(coords, out var gridUid, out var grid))
            return false;
        var tile = _map.TileIndicesFor(gridUid, grid, coords);
        foreach (var anchored in _map.GetAnchoredEntities(gridUid, grid, tile))
        {
            if (!TryComp<FixturesComponent>(anchored, out var fixtures)) continue;
            foreach (var fixture in fixtures.Fixtures.Values)
                if (fixture.Hard && fixture.CollisionLayer != 0)
                    return true;
        }
        return false;
    }
}
