using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Command;
using Dalamud.Game.Chat;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace XLinUI;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface Interface { get; private set; } = null!;
    [PluginService] internal static ICommandManager Commands { get; private set; } = null!;
    [PluginService] internal static IChatGui Chat { get; private set; } = null!;
    [PluginService] internal static IObjectTable Objects { get; private set; } = null!;
    [PluginService] internal static ITextureProvider Textures { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    internal Configuration Config { get; private set; }
    internal readonly List<ChatLine> History = [];
    internal readonly Dictionary<string, int> Unread = [];
    internal string ActiveChannel;
    private readonly UserInterface ui;
    private bool wasLoggedIn;

    public Plugin()
    {
        Config = Interface.GetPluginConfig() as Configuration ?? new Configuration();
        Config.Normalize();
        ActiveChannel = Config.Channels[0].Id;
        ui = new UserInterface(this);
        Commands.AddHandler("/xlinui", new CommandInfo(OnCommand)
        { HelpMessage = "XLinUI 设置。/xlinui off 关闭全部；/xlinui on 开启。" });
        Chat.ChatMessage += OnChat;
        Interface.UiBuilder.Draw += Draw;
        Interface.UiBuilder.OpenConfigUi += OpenSettings;
        Interface.UiBuilder.OpenMainUi += OpenSettings;
    }

    private void OnCommand(string command, string args)
    {
        switch (args.Trim().ToLowerInvariant())
        {
            case "off": Config.Enabled = false; Save(); break;
            case "on": Config.Enabled = true; Save(); break;
            default: OpenSettings(); break;
        }
    }

    private void OpenSettings() => ui.SettingsOpen = true;
    internal void Save() => Interface.SavePluginConfig(Config);
    internal void Reset()
    {
        Config = new Configuration();
        ActiveChannel = Config.Channels[0].Id;
        Unread.Clear();
        Save();
    }

    private void Draw()
    {
        var loggedIn = Objects.LocalPlayer is not null;
        if (wasLoggedIn && !loggedIn)
        {
            History.Clear(); Unread.Clear(); ui.ClearInput();
        }
        wasLoggedIn = loggedIn;
        ui.Draw(loggedIn);
    }

    // The API-15 distribution's event signature is validated during compilation.
    private void OnChat(IHandleableChatMessage message)
    {
        var kind = (ushort)message.LogKind;
        // Copy strings immediately; do not retain event-owned objects or native pointers.
        History.Add(new ChatLine(kind, message.Sender.TextValue, message.Message.TextValue, DateTime.Now));
        if (History.Count > 1000) History.RemoveRange(0, History.Count - 1000);
        foreach (var channel in Config.Channels.Where(c => c.Types.Count == 0 || c.Types.Contains(kind)))
            if (channel.Id != ActiveChannel || !Config.Enabled || !Config.ShowChat)
                Unread[channel.Id] = Math.Min(999, Unread.GetValueOrDefault(channel.Id) + 1);
        // Leave handled unchanged: original chat, links and other plugins continue to work.
    }

    public void Dispose()
    {
        Chat.ChatMessage -= OnChat;
        Interface.UiBuilder.Draw -= Draw;
        Interface.UiBuilder.OpenConfigUi -= OpenSettings;
        Interface.UiBuilder.OpenMainUi -= OpenSettings;
        Commands.RemoveHandler("/xlinui");
        History.Clear(); Unread.Clear();
    }
}

internal sealed record ChatLine(ushort Type, string Sender, string Text, DateTime Time);
