using System.Numerics;
using Content.Server.Administration;
using Content.Shared._CMU14.ZLevels.Core.Components;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._CMU14.ZLevels.Core;

[AdminCommand(AdminFlags.Debug)]
public sealed class CMUZDebugFallCommand : LocalizedCommands
{
    [Dependency] private IEntityManager _entities = default!;

    public override string Command => "zdebug-fall";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var zLevels = _entities.System<CMUZLevelsSystem>();
        var query = _entities.EntityQueryEnumerator<CMUZLevelsNetworkComponent>();
        if (!query.MoveNext(out var netUid, out var network) ||
            !zLevels.TryGetMapAtDepth((netUid, network), 1, out var above) ||
            !_entities.TryGetComponent<MapComponent>(above, out var map))
        {
            shell.WriteError("ZDEBUG no z-network level 1");
            return;
        }

        var mob = _entities.SpawnEntity("MobHuman", new MapCoordinates(new Vector2(4.5f, 12.5f), map.MapId));
        var dist = zLevels.DistanceToGround(mob, out var sticky);
        var hadFalling = _entities.HasComponent<CMUZFallingComponent>(mob);
        zLevels.WakeZPhysics(mob);
        var hasFalling = _entities.HasComponent<CMUZFallingComponent>(mob);
        shell.WriteLine($"ZDEBUG spawn {mob} on map {map.MapId} dist={dist:F3} sticky={sticky} hadFalling={hadFalling} hasFalling={hasFalling}");
    }
}
