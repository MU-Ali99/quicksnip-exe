# Development

## Versioning rule

Every tested build that adds a user-facing feature receives a new semantic build version before it is committed, tagged, and pushed. Documentation and the changelog must explain what changed and why.

## Immediate goal

Prove this exact native Windows flow:

```text
launch executable -> capture current display -> copy image -> save PNG -> exit
```

## Project structure

- `App.xaml` and `App.xaml.cs`: hidden WPF application lifecycle.
- `ScreenCaptureService.cs`: display capture, PNG saving, and clipboard copy.
- `RightSnip.csproj`: .NET Windows `WinExe` configuration.

WPF is used because it provides an STA Windows application context for reliable image clipboard access while compiling as a GUI executable with no console window.

## Reliability behavior

- Captures the display containing the mouse pointer.
- Uses a bounded retry if the Windows clipboard is temporarily busy.
- Creates `Pictures\RightSnip` when necessary.
- Exits with code `0` on success and `1` on failure.
- Does not display UI during either path.

## Deferred work

Do not add until the one-click prototype is tested:

- Drag Snip
- Settings UI
- Tray icon
- Screenshot editor
- Notifications
- History
- Browser integration
- Hotkeys
- Multiple capture modes
- Windows context-menu integration

## Taskbar packaging

- `scripts/New-AppIcon.ps1` generates a multi-size Windows icon from the established RightSnip artwork.
- `scripts/Publish.ps1` produces a self-contained single-file Windows x64 executable.
- `scripts/Install.ps1` installs it per-user and creates a Start Menu shortcut.
- `scripts/Uninstall.ps1` removes only that installed executable and shortcut.
- Pinning to the taskbar remains an explicit Windows user action.

The installed location is intentionally stable because taskbar shortcuts must not target replaceable Debug or publish output directories.
