using Content.Shared.CMU14.ZLevels.Core.Components;

namespace Content.Shared.CMU14.ZLevels.Core.EntitySystems;

public abstract partial class CMUSharedZLevelsSystem
{
    private void InitializeZPhysicsPresentation()
    {
        SubscribeLocalEvent<CMUZPhysicsComponent, ComponentStartup>(OnZPhysicsPresentationStartup);
        SubscribeLocalEvent<CMUZPhysicsComponent, ComponentRemove>(OnZPhysicsPresentationRemoved);
        SubscribeLocalEvent<CMUZPhysicsComponent, AfterAutoHandleStateEvent>(OnZPhysicsPresentationState);
    }

    private void OnZPhysicsPresentationStartup(Entity<CMUZPhysicsComponent> ent, ref ComponentStartup args)
        => RaiseZPhysicsPresentationChanged(ent);

    private void OnZPhysicsPresentationState(Entity<CMUZPhysicsComponent> ent, ref AfterAutoHandleStateEvent args)
        => RaiseZPhysicsPresentationChanged(ent);

    private void OnZPhysicsPresentationRemoved(Entity<CMUZPhysicsComponent> ent, ref ComponentRemove args)
        => RaiseZPhysicsPresentationChanged(ent);

    private void RaiseZPhysicsPresentationChanged(Entity<CMUZPhysicsComponent> ent)
    {
        if (!_net.IsClient)
            return;

        var ev = new CMUZPhysicsPresentationChangedEvent(
            ent.Owner,
            ent.Comp.LifeStage <= ComponentLifeStage.Running &&
            !TerminatingOrDeleted(ent.Owner) &&
            ent.Comp.ApplySpriteElevation &&
            ent.Comp.LocalPosition != 0f);
        RaiseLocalEvent(ref ev);
    }
}
