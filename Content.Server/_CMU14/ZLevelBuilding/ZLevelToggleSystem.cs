// SPDX-License-Identifier: AGPL-3.0-only
using System.IO;
using System.Linq;
using Content.Server.CMU14.Construction.CustomConstruction;
using Content.Server.GameTicking;
using Content.Shared.CMU14.Administration;
using Content.Shared.CMU14.ZLevelBuilding;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Robust.Shared.ContentPack;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.CMU14.ZLevelBuilding;

public sealed partial class ZLevelToggleSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IResourceManager _resource = default!;
    [Dependency] private SharedMapSystem _mapManager = default!;
    [Dependency] private CustomConstructionMenuSystem _menu = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ISharedAdminLogManager _adminLog = default!;

    private static readonly ResPath SaveFile = new("/au14_zlevel_disabled.txt");
    private readonly HashSet<string> _disabled = new();
    private readonly Dictionary<string, HashSet<EntityUid>> _loadedMaps = new();

    public override void Initialize()
    {
        base.Initialize();
        LoadDisabledSet();
        SubscribeNetworkEvent<RequestOpenZLevelTogglesEvent>(OnRequestOpen);
        SubscribeNetworkEvent<SetZLevelToggleEvent>(OnSetToggle);
        SubscribeLocalEvent<PostGameMapLoad>(OnPostGameMapLoad);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => _loadedMaps.Clear());
    }

    private void OnPostGameMapLoad(PostGameMapLoad ev)
    {
        if (!_mapManager.MapExists(ev.Map)) return;
        var mapUid = _mapManager.GetMapOrInvalid(ev.Map);
        if (!mapUid.IsValid()) return;
        var set = _loadedMaps.TryGetValue(ev.GameMap.ID, out var existing)
            ? existing : _loadedMaps[ev.GameMap.ID] = new HashSet<EntityUid>();
        set.Add(mapUid);
        if (_disabled.Contains(ev.GameMap.ID))
            ApplyToMap(mapUid, false);
    }

    private void OnRequestOpen(RequestOpenZLevelTogglesEvent msg, EntitySessionEventArgs args)
    {
        if (!_menu.CanUseTool(args.SenderSession, AU14ToolPermissions.ZLevelToggles)) return;
        var ev = new OpenZLevelTogglesEvent();
        foreach (var proto in _prototype.EnumeratePrototypes<GameMapPrototype>().OrderBy(p => p.MapName))
        {
            ev.Maps.Add(new ZLevelToggleEntry
            {
                MapProtoId = proto.ID,
                MapName = proto.MapName,
                Enabled = !_disabled.Contains(proto.ID),
                Loaded = _loadedMaps.ContainsKey(proto.ID),
            });
        }
        RaiseNetworkEvent(ev, args.SenderSession);
    }

    private void OnSetToggle(SetZLevelToggleEvent msg, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;
        if (!_menu.CanUseTool(session, AU14ToolPermissions.ZLevelToggles)) return;
        if (!_prototype.HasIndex<GameMapPrototype>(msg.MapProtoId)) return;
        var changed = msg.Enabled ? _disabled.Remove(msg.MapProtoId) : _disabled.Add(msg.MapProtoId);
        if (!changed) return;
        SaveDisabledSet();
        if (_loadedMaps.TryGetValue(msg.MapProtoId, out var maps))
        {
            foreach (var mapUid in maps)
            {
                if (!Deleted(mapUid))
                    ApplyToMap(mapUid, msg.Enabled);
            }
        }
        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{session.Name} set z-level building on map '{msg.MapProtoId}' to {(msg.Enabled ? "ALLOWED" : "DENIED")}.");
        _popup.PopupCursor(
            Loc.GetString(msg.Enabled ? "au-zlevel-toggle-enabled" : "au-zlevel-toggle-disabled", ("map", msg.MapProtoId)), session);
    }

    private void ApplyToMap(EntityUid mapUid, bool enabled)
    {
        var comp = EnsureComp<ZBuildableMapComponent>(mapUid);
        if (comp.Enabled == enabled) return;
        comp.Enabled = enabled;
        Dirty(mapUid, comp);
    }

    private void LoadDisabledSet()
    {
        try
        {
            if (!_resource.UserData.Exists(SaveFile)) return;
            using var reader = _resource.UserData.OpenText(SaveFile);
            while (reader.ReadLine() is { } line)
            {
                line = line.Trim();
                if (line.Length > 0) _disabled.Add(line);
            }
        }
        catch (Exception e) { Log.Error($"Failed to load z-level toggles: {e}"); }
    }

    private void SaveDisabledSet()
    {
        try
        {
            using var writer = _resource.UserData.OpenWriteText(SaveFile);
            foreach (var id in _disabled.OrderBy(i => i))
                writer.WriteLine(id);
        }
        catch (Exception e) { Log.Error($"Failed to save z-level toggles: {e}"); }
    }
}
