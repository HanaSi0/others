using System;
using System.Text;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace XLinUI;

// Uses generated bindings shipped by the active Dalamud installation, never hardcoded addresses.
internal static unsafe class GameBridge
{
    internal static bool Open(uint command)
    {
        var module = UIModule.Instance();
        if (module == null) return false;
        module->ExecuteMainCommand(command);
        return true;
    }

    internal static bool Send(string prefix, string text)
    {
        // No arbitrary slash-command execution in the text field.
        if (text.Length == 0 || text.Contains('\n') || text.Contains('\r') || text.Contains('\0')) return false;
        if (Encoding.UTF8.GetByteCount(text) > 450) return false;
        var module = UIModule.Instance();
        if (module == null) return false;
        using var value = new Utf8String();
        value.SetString(prefix + " " + text);
        module->ProcessChatBoxEntry(&value, saveToHistory: true);
        return true;
    }

    internal static SlotInfo ReadSlot(int bar, int index)
    {
        var module = RaptureHotbarModule.Instance();
        if (module == null) return default;
        var slot = module->GetSlotById((uint)bar, (uint)index);
        if (slot == null || slot->IsEmpty) return default;
        var cooldown = 0;
        slot->GetSlotActionCooldownPercentage(&cooldown);
        return new SlotInfo(slot->IconId, slot->CommandId, slot->CommandType.ToString(),
            Math.Max(0, cooldown));
    }

    internal static void ExecuteSlot(int bar, int index)
    {
        var module = RaptureHotbarModule.Instance();
        if (module == null) return;
        module->ExecuteSlotById((uint)bar, (uint)index);
    }
}

internal readonly record struct SlotInfo(uint Icon, uint Command, string? Type, int Cooldown);
