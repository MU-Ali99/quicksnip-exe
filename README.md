# QuickSnip for Windows

**One click. One snip.**

QuickSnip is a native Windows screenshot utility designed around direct taskbar capture.

Current build: **0.5.0**

## Capture modes

- **QuickSnip** captures the display containing the mouse pointer.
- **Window Snip** captures only the focused application window.

Exactly one capture mode is the left-click default. Selecting one immediately turns the other off, and the inactive mode remains available in the taskbar right-click Jump List. If the active mode is turned off without selecting another, QuickSnip is restored immediately.

## Output preferences

- Save timestamped PNG files.
- Copy the image to the Windows clipboard.
- Choose a custom save folder.

Save and clipboard output can be enabled independently, but at least one output must remain enabled. The default folder is `Pictures\QuickSnip`.

## Taskbar behavior

- Left-click QuickSnip to run the enabled default capture mode.
- Right-click QuickSnip for Window Snip, Open Snips Folder, and QuickSnip Settings.
- QuickSnip Settings uses compact toggle rows inspired by the original browser-extension popup.

## Progress so far

QuickSnip grew through five tested Windows builds:

- **0.1.0 — Native proof:** captured the display, copied an image to the Windows clipboard, saved a PNG, and exited silently.
- **0.2.0 — Taskbar-ready:** added a self-contained executable, stable per-user installation, icon, Start Menu shortcut, and pinning workflow.
- **0.3.0 — Jump List:** added Open Snips Folder and the first settings/actions window while preserving one-click capture.
- **0.4.0 — Reliability:** added onboarding, overlap protection, collision-safe filenames, diagnostics, and physical-pixel multi-monitor capture.
- **0.5.0 — QuickSnip:** renamed RightSnip, added Window Snip, persistent output preferences, adaptive taskbar commands, and the blue extension-inspired Settings and Information design.

The original Chrome-extension investigation established why a native app was needed: Chrome can capture protected pages such as `chrome://` through extension APIs, but browser clipboard restrictions can prevent the captured image from being copied. The Windows app performs capture, clipboard, and saving outside those browser restrictions.

## Build

```powershell
cd C:\path\to\quicksnip-exe
dotnet build .\QuickSnip.csproj
```

## Publish and install

```powershell
.\scripts\Publish.ps1
.\scripts\Install.ps1
```

The self-contained executable is installed at:

```text
%LOCALAPPDATA%\Programs\QuickSnip\QuickSnip.exe
```

The installer creates a Start Menu shortcut. Search for **QuickSnip** and choose **Pin to taskbar**. Build 0.5.0 removes the superseded installed RightSnip executable and shortcut but never removes or moves screenshots from `Pictures\RightSnip`.

## Diagnostics

Failures are logged silently at:

```text
%LOCALAPPDATA%\QuickSnip\Logs\quicksnip.log
```

## Current limitation

QuickSnip Settings and Information currently open centered on the primary monitor. Opening them on the monitor where the taskbar command was invoked is deferred until monitor placement can be made reliable with the borderless themed windows. Screenshot capture itself continues to follow the mouse pointer across monitors.
