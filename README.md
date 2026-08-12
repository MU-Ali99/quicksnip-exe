# RightSnip for Windows

RightSnip for Windows is a native desktop screenshot utility inspired by the RightSnip Chrome extension.

## Product goal

Bring the same simple capture experience to Windows:

- **Right Snip** captures the current display or active desktop view.
- **Drag Snip** captures a selected region.
- Captures are saved automatically to `Downloads\RightSnip`.
- Captures are copied through the native Windows clipboard.
- RightSnip can be launched from the Windows desktop and folder-background context menus.

This application is maintained separately from the Chrome extension because its platform APIs, packaging, distribution, and release lifecycle are different.

## Technology

- C#
- .NET 10
- WPF
- Win32 screen-capture and shell integration where required

## Windows context menu

The first reliable implementation will register per-user commands under `HKEY_CURRENT_USER`, so administrator privileges are not required.

On Windows 11, traditional registry commands appear under **Show more options**. Direct placement in the compact Windows 11 context menu requires a packaged shell extension and is outside the first MVP.

## Development

```powershell
dotnet build
dotnet run
```

No installer or system integration should run automatically during development. Context-menu registration must always be an explicit user action.
