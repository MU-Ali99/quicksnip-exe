# RightSnip for Windows

RightSnip is a one-click native Windows screenshot prototype.

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

For the intended experience, build or publish the executable, create a shortcut to it, and pin that shortcut to the Windows taskbar.

## Current scope

This prototype intentionally excludes Drag Snip, settings, tray features, editing, notifications, history, browser integration, hotkeys, and multiple capture modes.
