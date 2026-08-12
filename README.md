# RightSnip for Windows

RightSnip is a one-click native Windows screenshot prototype.

Current build: **0.2.0**

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

To uninstall this prototype:

```powershell
.\scripts\Uninstall.ps1
```

## Why the installed build is separate

Development output under `bin\` can move or be replaced during compilation. The installer copies the tested self-contained executable to a stable per-user path so a pinned taskbar shortcut continues to work across source builds.

Build history and the reasons behind changes are recorded in `CHANGELOG.md`.

## Current scope

This prototype intentionally excludes Drag Snip, settings, tray features, editing, notifications, history, browser integration, hotkeys, and multiple capture modes.
