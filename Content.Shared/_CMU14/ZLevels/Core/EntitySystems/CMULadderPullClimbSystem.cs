using Content.Shared.CMU14.ZLevels.Core.Components;
using Content.Shared.Interaction;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared._RMC14.Ladder;

namespace Content.Shared.CMU14.ZLevels.Core.EntitySystems;

public sealed partial class CMULadderPullClimbSystem : EntitySystem
{
    [Dependency] private SharedInteractionSystem _interaction = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PullableComponent, ShouldHandleVirtualItemInteractEvent>(OnPulledShouldHandle);
    }

    private void OnPulledShouldHandle(Entity<PullableComponent> ent, ref ShouldHandleVirtualItemInteractEvent args)
    {
        var interact = args.Event;
        if (!interact.CanReach ||
            interact.Target is not { } ladder ||
            ent.Comp.Puller != interact.User)
        {
            return;
        }

        if (!HasComp<CMUZLevelLadderComponent>(ladder) && !HasComp<LadderComponent>(ladder))
            return;

        _interaction.InteractionActivate(interact.User, ladder);
    }
}
