// SPDX-License-Identifier: AGPL-3.0-only
using Robust.Shared.Serialization;

namespace Content.Shared.CMU14.ZLevelBuilding;

[Serializable, NetSerializable]
public sealed class RequestOpenZLevelTogglesEvent : EntityEventArgs { }

[Serializable, NetSerializable]
public sealed class ZLevelToggleEntry
{
    public string MapProtoId = string.Empty;
    public string MapName = string.Empty;
    public bool Enabled = true;
    public bool Loaded;
}

[Serializable, NetSerializable]
public sealed class OpenZLevelTogglesEvent : EntityEventArgs
{
    public List<ZLevelToggleEntry> Maps = new();
}

[Serializable, NetSerializable]
public sealed class SetZLevelToggleEvent : EntityEventArgs
{
    public string MapProtoId = string.Empty;
    public bool Enabled = true;
}
