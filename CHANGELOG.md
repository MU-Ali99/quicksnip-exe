# Changelog

RightSnip uses semantic build versions. Each tested feature build receives a new version before it is pushed and tagged.

## 0.3.0 - 2026-08-12

### Taskbar commands

- Added **Open Snips Folder** to the Windows taskbar Jump List.
- Added **RightSnip Options** to open a small branded quick-actions window.
- Kept normal left-click behavior as immediate capture, copy, save, and exit.
- Added installation-time Jump List registration without taking a screenshot.

### Why

RightSnip's main action should remain one-click, while secondary actions need a discoverable home that does not turn every launch into a normal application window. The Windows Jump List provides those actions through the existing taskbar right-click experience.

## 0.2.0 - 2026-08-12

### Taskbar-ready build

- Added a self-contained Windows x64 publish process so RightSnip does not depend on a separately installed .NET runtime.
- Added a stable per-user installation path at `%LOCALAPPDATA%\Programs\RightSnip`.
- Added a Start Menu shortcut that can be pinned to the Windows taskbar.
- Added the established RightSnip icon at multiple Windows icon sizes.
- Added product, company, description, and executable version metadata.
- Added explicit publish, install, and uninstall scripts.

### Why

The initial prototype proved native screen capture, PNG saving, and image clipboard copying. This build turns that proof into a stable taskbar test: the pinned target no longer points into a temporary Debug build folder and does not require `dotnet run`.

## 0.1.0 - 2026-08-12

### One-click prototype

- Launching the executable captures the display containing the mouse pointer.
- The screenshot is copied as an image to the Windows clipboard.
- A timestamped PNG is saved to `Pictures\RightSnip`.
- The process exits without showing a window, console, popup, editor, or notification.

### Why

This build tested whether RightSnip could act as a more direct alternative to the Windows Snipping Tool and avoid the protected-page limitations encountered by the Chrome extension.
