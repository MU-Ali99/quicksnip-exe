# QuickSnip development

## Versioning

Every tested user-facing feature build receives a semantic version, documentation update, commit, tag, push, and GitHub release.

## Build 0.5.0 architecture

- `QuickSnip.csproj`: .NET 10 WPF `WinExe` project.
- `NativeScreenCapture.cs`: physical-pixel display and active-window capture.
- `ScreenCaptureService.cs`: output routing for PNG and clipboard.
- `SettingsService.cs`: persistent JSON preferences.
- `OptionsWindow`: extension-inspired QuickSnip Settings interface and output toggles.
- `JumpListService`: adaptive taskbar commands.

Settings are stored at `%LOCALAPPDATA%\QuickSnip\settings.json`.

Current tested rules:

- Capture modes are mutually exclusive: exactly one is the left-click default.
- At least one output stays enabled.
- Selecting QuickSnip immediately disables Window Snip; selecting Window Snip immediately disables QuickSnip.
- The inactive capture mode remains available through the taskbar Jump List.
- Trying to disable the active mode without selecting another immediately restores QuickSnip.
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

The source folder and GitHub repository retain the historical `rightsnip-exe` name for this 0.5.0 commit. The installed product, executable, namespaces, assets, AppData, logs, and new screenshot folder use QuickSnip. Existing `Pictures\RightSnip` images remain untouched.

## Deferred work

- Settings and Information intentionally use stable primary-work-area centering. A pointer-monitor placement experiment was rolled back because the borderless WPF windows could appear against the top edge or on the wrong monitor.
- Drag Snip, editor, history, tray support, hotkeys, notifications, and Chrome companion integration remain outside this build.
- The local/GitHub repository rename can be handled separately after the 0.5.0 state is safely pushed.

## Command routing

- No arguments: run the enabled default capture mode.
- `--active-window`: capture the focused application window.
- `--open-folder`: open the configured save folder.
- `--menu`: open QuickSnip Settings.
- `--register-jump-list`: register commands without capturing.

## Rename migration

Build 0.5.0 changes the installed product identity from RightSnip to QuickSnip. Installation removes only `%LOCALAPPDATA%\Programs\RightSnip` and the old Start Menu shortcut. It does not touch the old source repository, AppData diagnostics, or `Pictures\RightSnip` screenshots.
