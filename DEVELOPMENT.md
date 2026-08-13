# QuickSnip development

## Versioning

Every tested user-facing feature build receives a semantic version, documentation update, commit, tag, push, and GitHub release.

## Build 0.9.0 architecture

- `QuickSnip.csproj`: .NET 10 WPF `WinExe` project.
- `NativeScreenCapture.cs`: physical-pixel display and active-window capture.
- `ScreenCaptureService.cs`: capture output routing, clipboard retries, and optional local toast.
- `ScreenshotFileService.cs`: PNG/JPEG/WebP encoding, quality, timestamped naming, and collision handling.
- `SnipCleanupService.cs`: constrained QuickSnip-file discovery and Windows Recycle Bin moves.
- `SettingsService.cs`: persistent JSON preferences.
- `OptionsWindow`: extension-inspired QuickSnip Settings interface and output toggles.
- `JumpListService`: adaptive taskbar commands.
- `DragSnipWindow`: virtual-desktop selection overlay with cancellation and shaded outside area.
- `WindowPlacementService`: persisted, clamped window size and location.
- `CaptureToastWindow`: short-lived local confirmation; no background process or network activity.
- `HotkeyService`: Win32 global shortcut registration, conflict detection, invisible host routing, and per-user startup registration.
- `installer/QuickSnip.iss`: stable per-user upgrade/uninstall identity.

Settings are stored at `%LOCALAPPDATA%\QuickSnip\settings.json`.

Current tested rules:

- QuickSnip, Window Snip, and Drag Snip are mutually exclusive: exactly one is the left-click default.
- At least one output stays enabled.
- Selecting a capture mode immediately disables the other two.
- Inactive capture modes remain available through the taskbar Jump List.
- Trying to disable the active mode without selecting another immediately restores Drag Snip.
- Hotkeys are unassigned by default and require at least one modifier key.
- Assigned hotkeys register immediately; conflicts keep the previous valid shortcut.
- The invisible hotkey host and Windows startup entry exist only while at least one shortcut is assigned.
- A one-second cross-process cooldown ignores accidental repeated clicks.
- Window Snip uses `BitBlt` rather than `PrintWindow` because GPU-rendered apps can return a successful but black `PrintWindow` image. Maximized bounds are clipped to the monitor work area to exclude the taskbar.

## Build progression

| Build | Milestone | Engineering result |
| --- | --- | --- |
| 0.1.0 | One-click prototype | Proved silent display capture, image clipboard copy, PNG saving, and immediate exit. |
| 0.2.0 | Stable taskbar executable | Added self-contained x64 publishing, per-user installation, branding, shortcut creation, and pinning. |
| 0.3.0 | Windows Jump List | Separated the primary left-click action from folder and settings commands available on right-click. |
| 0.4.0 | Onboarding and reliability | Added first-run guidance, a capture semaphore, timestamp collision protection, logging, and DPI-safe physical monitor capture. |
| 0.5.0 | QuickSnip modes and preferences | Renamed the product, added Window Snip, persistent mode/output settings, adaptive Jump List commands, and the themed Settings/Information UI. |
| 0.6.0 | Drag Snip and scalable windows | Added a cancellable selection overlay and shared, validated window sizing and placement for Settings and Information. |
| 0.7.0 | Output and distribution | Added multi-format saving, quality choices, local toast, recovery controls, Recycle Bin cleanup, and installer/portable packages. |
| 0.8.0 | Custom hotkeys and design | Added opt-in global shortcuts, conflict-safe registration, a conditional background host/startup entry, Drag Snip defaults, and unified cobalt/indigo interfaces. |
| 0.8.1 | Drag Snip placement maintenance | Stabilized the Drag Snip instruction pill on the primary display. |
| 0.9.0 | Lock Snip and interface refinement | Added locked display/window capture, controlled scrolling, immediate per-section output, shared hotkeys, taskbar-mode visibility, compact typography, and controller polish. |

The GitHub repository is `MU-Ali99/quicksnip-exe`. The installed product, executable, namespaces, assets, AppData, logs, and new screenshot folder use QuickSnip. Existing `Pictures\RightSnip` images remain untouched.

## Build 0.8.1 maintenance

- The Drag Snip instruction pill is anchored near the top center of the primary display.
- Placement is re-applied after content rendering and DPI changes to prevent Windows from shifting it when capture starts from another monitor.
- Automatic placement on the launch display is intentionally deferred until it can be tested across more monitor layouts and scaling combinations.

## Deferred work

- Automatic pointer-monitor placement remains deferred. Saved placement is restored when valid; otherwise the window uses a comfortable primary-screen default.
- Drag Snip's instruction pill remains on the primary display while launch-display placement awaits broader multi-monitor testing.
- Editor, history, tray support, and Chrome companion integration remain outside this build.

## Build 0.9.0 Lock Snip

Build 0.9.0 promotes the tested Lock Snip prototype into the released application.

- Lock Snip appears below capture modes and above Output in Settings.
- The user can lock either a selected display or selected window.
- Capture Section saves, copies, and shows the configured toast immediately for every capture.
- Previous Position and Next Position default to `Alt+W` and `Alt+S` during an active session; Capture Section is unassigned by default.
- A compact floating controller provides Previous, Next, Capture, and Close actions.
- Middle-click closes the active Lock Snip session.
- Chrome accepts background wheel scrolling directly.
- File Explorer uses a focused input fallback that restores the previous foreground window and pointer position.
- Lock Snip is opt-in and loads only while a session is active.

Lock Snip intentionally saves each captured section immediately rather than retaining or stitching a long in-memory image. Broader application, monitor, DPI, and scrolling tests remain part of the future reliability audit.

## Build 0.7.0 safety and migration

- SkiaSharp encodes PNG, JPEG, and WebP. Clipboard output remains a WPF `BitmapSource`, independent of disk format.
- PNG is lossless; the quality selector applies to JPEG and WebP.
- Files use automatic mode-and-timestamp names and receive numeric suffixes on collision.
- Recycle cleanup only targets supported image files beginning with `quicksnip-` under the configured save folder and requires confirmation.
- Restore Defaults preserves the configured save folder, window placement, and every screenshot.
- The installer uses a stable AppId, installs under `%LOCALAPPDATA%\Programs\QuickSnip`, and leaves `%LOCALAPPDATA%\QuickSnip` plus screenshot folders untouched during uninstall.
- Inno Setup 6 builds the installer. `scripts\PackageRelease.ps1` also creates a portable ZIP.
- Release publishing must use `scripts\Publish.ps1`; its native-library extraction flag is required for the self-contained WPF executable.

## Command routing

- No arguments: run the enabled default capture mode.
- `--active-window`: capture the focused application window.
- `--drag`: open the selection overlay and capture the dragged region.
- `--open-folder`: open the configured save folder.
- `--menu`: open QuickSnip Settings.
- `--register-jump-list`: register commands without capturing.
- `--hotkey-host`: run the invisible global-hotkey message host.

## Rename migration

Build 0.5.0 changes the installed product identity from RightSnip to QuickSnip. Installation removes only `%LOCALAPPDATA%\Programs\RightSnip` and the old Start Menu shortcut. It does not touch the old source repository, AppData diagnostics, or `Pictures\RightSnip` screenshots.
