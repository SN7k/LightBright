# LiteBright

**LiteBright** is a lightweight Windows system-tray app for controlling monitor brightness.  
It lives quietly in the taskbar, uses virtually no memory, and starts instantly — no background polling, no bloat.

---

## Features

| Feature | Details |
|---|---|
| **Per-monitor brightness sliders** | Independent control for every connected display |
| **Scroll-wheel control** | Scroll over the tray icon to adjust brightness without opening the panel |
| **Global hotkeys** | Bind any key combo to brightness up/down per monitor |
| **Contrast slider** | Optional contrast control alongside brightness |
| **Light & dark theme** | Automatically follows your Windows theme setting |
| **Windows accent colour** | UI highlights pick up your personalised accent colour |
| **Start with Windows** | Launches on login via the user registry — no admin required |
| **DDC/CI support** | Full hardware brightness control on compatible external monitors |
| **WMI support** | Reliable brightness control on laptops and built-in displays |

---

## Prerequisites

| Requirement | Download |
|---|---|
| **.NET 10 Runtime** (x64) | https://dotnet.microsoft.com/download/dotnet/10.0 |
| Windows 10 / 11 | — |

> **Note:** LiteBright targets **net10.0-windows** with a **win-x64** runtime.  
> Download and install the x64 runtime installer — `dotnet-runtime-10.x.xxx-win-x64.exe`.

---

## Releases

Download the latest installer from the [Releases](https://github.com/SN7k/LightBright/releases) page.

| Version | Date | Notes |
|---|---|---|
| **1.0.0** | Feb 2026 | Initial release |

The installer (`LiteBright-Setup-x.x.x.exe`) handles everything — no admin required, installs for the current user only.
