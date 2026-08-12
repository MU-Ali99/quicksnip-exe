# RightSnip for Windows

RightSnip is a one-click native Windows screenshot prototype.

Current build: **0.4.0**

## Prototype behavior

Launching RightSnip immediately:

1. Captures the display containing the mouse pointer.
2. Copies the screenshot as an image to the Windows clipboard.
3. Saves a timestamped PNG to `Pictures\RightSnip`.
4. Exits.

RightSnip does not open a normal window, console, editor, notification, or confirmation prompt.

Example filename:

```text
rightsnip-2026-08-12-16-45-30.png
```

## Build

```powershell
cd C:\Users\ubaid\Desktop\Projects\rightsnip-exe
dotnet build
```

## Run

```powershell
dotnet run
```

## Publish and install for taskbar use

```powershell
.\scripts\Publish.ps1
.\scripts\Install.ps1
```

The publish is a self-contained Windows x64 executable. Installation copies it to:

```text
%LOCALAPPDATA%\Programs\RightSnip\RightSnip.exe
```

It also creates a RightSnip shortcut in the current user's Start Menu. Open Start, search for **RightSnip**, right-click it, and choose **Pin to taskbar**.

Taskbar behavior:

- Left-click RightSnip to capture immediately.
- Right-click RightSnip to open its Windows Jump List.
- **Open Snips Folder** opens `Pictures\RightSnip`.
- **RightSnip Options** opens a small window with Right Snip, Open Snips Folder, and Close.

## First launch

After each fresh installation, the first normal launch opens a short guide instead of taking a screenshot. It explains left-click capture, taskbar right-click actions, pinning, saving, and clipboard paste behavior. After selecting **I understand how RightSnip works**, Continue opens the main Options window.

Later left-clicks return to immediate capture. The guide can be reopened with the information button in RightSnip Options.

RightSnip prevents overlapping captures, uses collision-resistant millisecond filenames, follows the display containing the mouse pointer, and writes silent failure details to:

```text
%LOCALAPPDATA%\RightSnip\Logs\rightsnip.log
```

To uninstall this prototype:

```powershell
.\scripts\Uninstall.ps1
```

## Why the installed build is separate

Development output under `bin\` can move or be replaced during compilation. The installer copies the tested self-contained executable to a stable per-user path so a pinned taskbar shortcut continues to work across source builds.

Build history and the reasons behind changes are recorded in `CHANGELOG.md`.

## Current scope

This prototype intentionally excludes Drag Snip, settings, tray features, editing, notifications, history, browser integration, hotkeys, and multiple capture modes.
