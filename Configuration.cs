using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Configuration;
using Newtonsoft.Json;

namespace XLinUI;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public bool Enabled { get; set; } = true;
    public bool ShowTop { get; set; } = true;
    public bool ShowChat { get; set; } = true;
    public bool ShowSkills { get; set; } = true;
    public bool Rounded { get; set; } = true;
    public bool Glass { get; set; } = true;
    public bool Compact { get; set; }
    public bool NativeInteractions { get; set; }
    public float Opacity { get; set; } = .88f;
    public Vector3 Tint { get; set; } = new(.39f, .77f, .73f);
    public int Hotbar { get; set; }
    public float SkillSize { get; set; } = 44;
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public List<Shortcut> Shortcuts { get; set; } = DefaultShortcuts();
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public List<Channel> Channels { get; set; } = DefaultChannels();

    public void Normalize()
    {
        Opacity = float.IsFinite(Opacity) ? Math.Clamp(Opacity, .05f, 1) : .88f;
        SkillSize = float.IsFinite(SkillSize) ? Math.Clamp(SkillSize, 32, 72) : 44;
        Hotbar = Math.Clamp(Hotbar, 0, 9);
        Tint = new Vector3(Clamp(Tint.X), Clamp(Tint.Y), Clamp(Tint.Z));
        Shortcuts ??= DefaultShortcuts();
        Channels ??= DefaultChannels();
        Channels.RemoveAll(c => c is null);
        if (Channels.Count == 0) Channels = DefaultChannels();
        foreach (var c in Channels)
        {
            c.Id = string.IsNullOrWhiteSpace(c.Id) ? Guid.NewGuid().ToString("N") : c.Id;
            c.Name = string.IsNullOrWhiteSpace(c.Name) ? "未命名" : c.Name;
            c.Types ??= [];
        }
    }

    private static float Clamp(float value) => float.IsFinite(value) ? Math.Clamp(value, 0, 1) : .5f;
    public static List<Shortcut> DefaultShortcuts() =>
    [
        new("角色", 2), new("背包", 10), new("任务", 4),
        new("地图", 16), new("传送", 35), new("好友", 13),
    ];
    public static List<Channel> DefaultChannels() =>
    [
        new("全部", []), new("小队", [14, 15]),
        new("社交", [24, 16, 17, 18, 19, 20, 21, 22, 23, 37, 101, 102, 103, 104, 105, 106, 107]),
        new("私信", [12, 13]), new("系统", [2, 3, 56, 57, 58, 59, 60, 62, 69, 70]),
    ];
}

[Serializable]
public sealed class Shortcut(string name, uint command)
{
    public string Name { get; set; } = name;
    public uint Command { get; set; } = command;
    public bool Enabled { get; set; } = true;
}

[Serializable]
public sealed class Channel(string name, List<ushort> types)
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = name;
    // Empty means all received messages. These values are LogKind IDs, not send commands.
    public List<ushort> Types { get; set; } = types;
}
