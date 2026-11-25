namespace Content.Shared._CMU14.ZLevels.Core;

[ByRefEvent]
public readonly record struct CMUZPairedEvent(EntityUid Twin);

[ByRefEvent]
public readonly record struct CMUZUnpairedEvent(EntityUid FormerTwin);