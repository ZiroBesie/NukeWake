// SPDX-License-Identifier: AGPL-3.0-only
using System.Numerics;
using Content.Shared.CMU14.ZLevelBuilding;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.CMU14.ZLevelBuilding;

public sealed partial class ZStairSystem : EntitySystem
{
    [Dependency] private ZLevelBuildingSystem _building = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private ITileDefinitionManager _tileDef = default!;

    private EntityQuery<MapGridComponent> _gridQuery;
    private int _deferSetup;

    public override void Initialize()
    {
        base.Initialize();
        _gridQuery = GetEntityQuery<MapGridComponent>();
        SubscribeLocalEvent<ZStairComponent, MapInitEvent>(OnStairMapInit);
        SubscribeLocalEvent<ZStairBeamLinkComponent, EntityTerminatingEvent>(OnBeamDestroyed);
    }

    private void OnBeamDestroyed(Entity<ZStairBeamLinkComponent> ent, ref EntityTerminatingEvent args)
    {
        var link = ent.Comp;
        if (link.HasPlatform && !TerminatingOrDeleted(link.PlatformGrid) && _gridQuery.TryComp(link.PlatformGrid, out var platformGrid))
        {
            foreach (var tile in link.LaidTiles)
                _map.SetTile(link.PlatformGrid, platformGrid, tile, Tile.Empty);
        }
        if (link.Stair is { Valid: true } stair && !TerminatingOrDeleted(stair))
            QueueDel(stair);
        if (link.Companion is { Valid: true } companion && !TerminatingOrDeleted(companion))
            QueueDel(companion);
    }

    private EntityUid? PlaceBeam(EntityUid gridUid, MapGridComponent grid, Vector2 worldPos, string beam)
    {
        var tile = _map.TileIndicesFor(gridUid, grid, new MapCoordinates(worldPos, Transform(gridUid).MapID));
        EntityUid? beamUid = null;
        foreach (var anchored in _map.GetAnchoredEntities(gridUid, grid, tile))
        {
            if (MetaData(anchored).EntityPrototype?.ID == beam)
            {
                beamUid = anchored;
                break;
            }
        }
        beamUid ??= Spawn(beam, _map.GridTileToLocal(gridUid, grid, tile));
        if (TryComp<StructuralSupportComponent>(beamUid, out var support))
        {
            support.HideFromScanner = true;
            Dirty(beamUid.Value, support);
        }
        return beamUid;
    }

    private const int LandingPatchRadius = 1;
    private void LayLandingPatch(EntityUid gridUid, Vector2 worldPos, string tileId, int radius)
    {
        if (!_gridQuery.TryComp(gridUid, out var grid) || !_tileDef.TryGetDefinition(tileId, out var def))
            return;
        if (Transform(gridUid).MapUid is not { } mapUid || !TryComp<MapComponent>(mapUid, out var mapComp))
            return;
        var center = _map.TileIndicesFor(gridUid, grid, new MapCoordinates(worldPos, mapComp.MapId));
        for (var dx = -radius; dx <= radius; dx++)
        for (var dy = -radius; dy <= radius; dy++)
        {
            var tile = center + new Vector2i(dx, dy);
            if (_map.TryGetTileRef(gridUid, grid, tile, out var existing) && !existing.Tile.IsEmpty)
                continue;
            _map.SetTile(gridUid, grid, tile, new Tile(def.TileId));
        }
    }

    private List<Vector2i> LayPlatformRingWithShaft(EntityUid gridUid, Vector2 worldPos, string tileId, int radius)
    {
        var laid = new List<Vector2i>();
        if (!_gridQuery.TryComp(gridUid, out var grid) || !_tileDef.TryGetDefinition(tileId, out var def))
            return laid;
        if (Transform(gridUid).MapUid is not { } mapUid || !TryComp<MapComponent>(mapUid, out var mapComp))
            return laid;
        var center = _map.TileIndicesFor(gridUid, grid, new MapCoordinates(worldPos, mapComp.MapId));
        for (var dx = -radius; dx <= radius; dx++)
        for (var dy = -radius; dy <= radius; dy++)
        {
            var tile = center + new Vector2i(dx, dy);
            if (dx == 0 && dy == 0)
            {
                _map.SetTile(gridUid, grid, tile, Tile.Empty);
                continue;
            }
            if (_map.TryGetTileRef(gridUid, grid, tile, out var existing) && !existing.Tile.IsEmpty)
                continue;
            _map.SetTile(gridUid, grid, tile, new Tile(def.TileId));
            laid.Add(tile);
        }
        return laid;
    }
}
