using System.Text;

namespace BrightnessController.Hotkeys;

[Flags]
public enum HotkeyModifiers : uint
{
    None    = 0,
    Alt     = 0x0001,
    Ctrl    = 0x0002,
    Shift   = 0x0004,
    Win     = 0x0008,
}

public enum HotkeyAction
{
    BrightnessUp_Monitor0   = 0,
    BrightnessDown_Monitor0 = 1,
    BrightnessUp_Monitor1   = 2,
    BrightnessDown_Monitor1 = 3,
    BrightnessUp_Monitor2   = 4,
    BrightnessDown_Monitor2 = 5,
    BrightnessUp_Monitor3   = 6,
    BrightnessDown_Monitor3 = 7,
}

public sealed class HotkeyDefinition
{
    public HotkeyAction    Action    { get; set; }
    public HotkeyModifiers Modifiers { get; set; }
    public Keys            Key       { get; set; }

    public int Id => (int)Action + 1000;

    public bool IsValid => Key != Keys.None;


    public static int MonitorIndex(HotkeyAction action) => ((int)action) / 2;
    public static bool IsIncrease(HotkeyAction action)  => ((int)action) % 2 == 0;

    public override string ToString()
    {
        if (!IsValid) return "(none)";
        var sb = new StringBuilder();
        if (Modifiers.HasFlag(HotkeyModifiers.Ctrl))  sb.Append("Ctrl+");
        if (Modifiers.HasFlag(HotkeyModifiers.Alt))   sb.Append("Alt+");
        if (Modifiers.HasFlag(HotkeyModifiers.Shift)) sb.Append("Shift+");
        if (Modifiers.HasFlag(HotkeyModifiers.Win))   sb.Append("Win+");
        sb.Append(Key);
        return sb.ToString();
    }

    public HotkeyDefinition Clone() =>
        new() { Action = Action, Modifiers = Modifiers, Key = Key };
}
