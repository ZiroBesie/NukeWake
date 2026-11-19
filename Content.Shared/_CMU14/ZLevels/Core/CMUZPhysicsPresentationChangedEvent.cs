namespace Content.Shared.CMU14.ZLevels.Core;

[ByRefEvent]
public readonly record struct CMUZPhysicsPresentationChangedEvent(EntityUid Uid, bool Elevated);
