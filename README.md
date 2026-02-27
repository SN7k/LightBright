# Brightness Controller

A lightweight Windows system-tray brightness controller for laptop and external monitors.
Written in **C# / .NET 8 WinForms** — minimal RAM, instant startup, no background polling.

---

## Prerequisites

| Requirement | Download |
|---|---|
| **.NET 10 SDK** (x64) | <https://dotnet.microsoft.com/download/dotnet/10.0> |
| Windows 10 / 11 | — |

> **Note:** the app targets `net10.0-windows` with a `win-x64` rid.  
> Install the **x64** SDK installer (`dotnet-sdk-10.x.xxx-win-x64.exe`).

---

## Build

```powershell
# From the project folder (d:\SHOMBHU\Desktop\bglight):
dotnet restore
dotnet build -c Release
```

The binary is output to:

```
bin\Release\net10.0-windows\win-x64\BrightnessController.exe
```

## Publish (single self-contained exe — recommended)

```powershell
dotnet publish -c Release -r win-x64 --self-contained false -o publish\
```

---

## Run

Double-click `BrightnessController.exe`.  
A sun icon ☀ appears in the system tray (click **^** in the taskbar if hidden).

---

## Features

| Action | How |
|---|---|
| **Open brightness panel** | Left-click tray icon |
| **Per-monitor sliders** | Shown automatically per connected display |
| **Quick step (context menu)** | Right-click → ▲/▼ per monitor |
| **Global hotkeys** | Right-click → Settings → Hotkeys tab |
| **Contrast slider** | Settings → enable "Show contrast slider" |
| **Start with Windows** | Settings → "Start with Windows" (HKCU registry, no admin) |
| **Exit** | Right-click → Exit |

---

## How brightness is controlled

| Display type | API used |
|---|---|
| Laptop built-in panel | **WMI** `WmiMonitorBrightness` — works on any laptop |
| External monitor (DDC/CI capable) | **DDC/CI** via `dxva2.dll` — works on most modern monitors |
| External without DDC/CI | Reported as "DDC/CI N/A"; sliders are display-only |

---

## Project structure

```
BrightnessController/
├── Program.cs                  Entry point, single-instance mutex
├── TrayApplicationContext.cs   Tray icon, context menu, orchestrator
├── Native/
│   └── NativeMethods.cs        All P/Invoke declarations
├── Monitors/
│   ├── MonitorInfo.cs          Data model for a display
│   ├── MonitorManager.cs       Enumerate + read/write brightness & contrast
│   ├── WmiMonitorHelper.cs     WMI for internal/laptop displays
│   └── DdcCiHelper.cs          DDC/CI for external displays
├── Hotkeys/
│   ├── HotkeyDefinition.cs     Data model for a hotkey binding
│   └── HotkeyManager.cs        RegisterHotKey / WM_HOTKEY dispatcher
├── Settings/
│   ├── AppSettings.cs          POCO settings with defaults
│   └── SettingsManager.cs      JSON load/save to %AppData%
├── Helpers/
│   ├── StartupManager.cs       HKCU run-at-startup registry helper
│   └── IconHelper.cs           Programmatic sun tray icon (no .ico file needed)
└── UI/
    ├── BrightnessPanel.cs      Dark popup panel with per-monitor sliders
    └── SettingsForm.cs         Settings: startup, hotkeys, contrast toggle
```

---

## Settings file

Stored at: `%AppData%\BrightnessController\settings.json`

```json
{
  "StartWithWindows": true,
  "EnableContrastSlider": false,
  "BrightnessStep": 10,
  "Hotkeys": [
    {
      "Action": "BrightnessUp_Monitor0",
      "Modifiers": "Ctrl, Alt",
      "Key": "F1"
    },
    {
      "Action": "BrightnessDown_Monitor0",
      "Modifiers": "Ctrl, Alt",
      "Key": "F2"
    }
  ]
}
```

---

## Troubleshooting

| Issue | Solution |
|---|---|
| Slider has no effect on laptop | Run as the same user that owns the session; WMI needs no admin but requires the correct WMI namespace |
| External monitor slider greyed out | Monitor does not support DDC/CI; check OSD settings and enable DDC/CI |
| Hotkey not working | Another app may hold the same combination; try a different key binding |
| Two instances warning | Check the system tray — app is already running |
