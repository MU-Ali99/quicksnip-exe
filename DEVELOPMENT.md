# Development roadmap

## Phase 0 - Foundation

- Create an independent WPF repository.
- Confirm a clean .NET build.
- Define the initial product scope and platform constraints.

## Phase 1 - Native capture engine

- Capture the display containing the mouse pointer.
- Save PNG files to `Downloads\RightSnip`.
- Copy captures using the native Windows clipboard.
- Handle clipboard contention with a short bounded retry.

## Phase 2 - Drag Snip

- Add a dimmed topmost selection overlay.
- Support click-and-drag selection.
- Support `Esc` cancellation.
- Handle DPI scaling and negative coordinates on multi-monitor desktops.
- Exclude the selection overlay from the final capture.

## Phase 3 - Application interface

- Match RightSnip's minimal blue visual identity.
- Provide Right Snip and Drag Snip actions.
- Show the screenshot location.
- Add explicit context-menu install and uninstall controls.

## Phase 4 - Windows shell integration

- Register per-user desktop and folder-background context-menu commands.
- Add `Right Snip` and `Drag Snip` subcommands.
- Verify installation and clean removal.
- Document the Windows 11 **Show more options** behavior.

## Phase 5 - Packaging and release

- Publish a self-contained Windows x64 build.
- Add application icons and version metadata.
- Create an installer with explicit shell-integration consent.
- Test clean install, upgrade, and uninstall behavior.

## Reliability rules

- Implement and test one phase at a time.
- Do not register shell commands automatically.
- Do not require administrator privileges for the MVP.
- Do not delete or manage unrelated user files.
- Preserve a working capture-and-clipboard baseline before adding UI polish.
