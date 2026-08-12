# Development

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
