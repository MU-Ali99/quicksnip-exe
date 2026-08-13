# Changelog

QuickSnip uses semantic build versions. Each tested feature build receives a new version before it is pushed and tagged.

## 0.5.0 - 2026-08-12

### QuickSnip rename, capture modes, and preferences

- Renamed the Windows product, executable, installation, shortcut, AppData, logs, screenshots, namespaces, and user interface from RightSnip to QuickSnip.
- Added Window Snip using native Windows foreground-window bounds.
- Added extension-inspired toggle rows for QuickSnip, Window Snip, Save PNG, and Copy to clipboard.
- Added persistent JSON preferences and a custom save-folder picker.
- Added mutually exclusive capture-mode toggles: the selected mode becomes the left-click default.
- Kept a disabled capture mode available as an alternate right-click Jump List action.
- Immediately restores QuickSnip when the user tries to leave all capture modes disabled.
- Added fast, animated toggle state changes and adaptive alternate-mode Jump List commands.
- Added themed, movable Settings and Information windows with custom title bars and Windows-style close-button hover feedback.
- Added the QuickSnip icon and restored the familiar bright-blue extension-style header treatment.
- Window Snip uses physical visible-window pixels for compatibility with hardware-accelerated apps and clips maximized windows to the monitor work area so the taskbar is excluded.
- Active-window discovery rejects 1×1 shell helpers, cloaked windows, untitled surfaces, and desktop/taskbar classes before selecting the top real application window.
- Added a one-second cooldown that ignores accidental repeated clicks after a completed capture.
- Preserved all existing screenshots in `Pictures\RightSnip` during migration.

### Why

QuickSnip better describes the product's promise: one click, one snip. Active-window capture adds a useful focused mode without the selection-overlay complexity of Drag Snip, while extension-style toggles keep capture and output choices compact and familiar.

## 0.4.0 - 2026-08-12

### Onboarding and reliability

- Added a first-install guide explaining taskbar pinning, left-click capture, right-click commands, save location, and clipboard paste behavior.
- Added an **I understand how RightSnip works** confirmation before continuing to the main Options page.
- Added an information button in Options to reopen the guide.
- Redesigned Options using the established RightSnip blue and layered-wave visual direction.
- Added the current build number and exact save location to Options.
- Prevented overlapping captures from rapid repeated launches.
- Added millisecond filename precision to prevent same-second collisions.
- Added silent diagnostic failure logging under `%LOCALAPPDATA%\RightSnip\Logs`.
- Replaced DPI-virtualized screen copying with native Win32 monitor bounds and `BitBlt` for accurate physical display capture on mixed-scaling desktops.

### Why

RightSnip's normal behavior is intentionally silent, so a first-run guide is necessary to explain that the taskbar icon behaves differently on left-click and right-click. The reliability changes protect this direct workflow from repeated clicks, filename collisions, clipboard failures, and multi-monitor coordinate scaling without adding notifications or interrupting successful captures.

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
