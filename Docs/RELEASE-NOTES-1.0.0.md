# ViscaCamLink 1.0.0

[Deutsch](https://github.com/misorrek/ViscaCamLink/blob/v1.0.0/Docs/RELEASE-NOTES-1.0.0-de.md)

ViscaCamLink 1.0.0 is a large update over 0.3.0: named presets in groups, customizable hotkeys, mouse control, a dark theme, automatic updates and a new installer.

## Highlights

- **More presets**: organize presets in groups of 10, with up to 255 camera slots (0–254) in total. Switch groups with a tab row; add, remove and rename groups.
- **Named presets**: double-click a preset or group name to rename it (Enter confirms, Escape cancels). Choose between numpad (default) and standard layout.
- **Custom hotkeys**: assign your own keys in Options → Hotkeys for presets, movement, zoom, camera profiles and preset groups. Conflicts are detected, and a switch turns global hotkeys on or off.
- **Mouse pan/tilt**: click and drag on the centre button. The further you move from the centre, the faster the camera moves.
- **Automatic updates**: the app checks for new versions on start, shows the changelog, downloads the update and restarts itself.
- **Themes**: light, dark, or following the Windows app mode live.

## Improvements

- Connection health check: the status shows when the camera becomes unreachable and recovers automatically once it is back.
- Preset saves now report camera failures instead of showing a success message; startup connection begins after the main window appears.
- Default numpad hotkeys are now app-local; enable global hotkeys in Options to use them across applications. Existing saved hotkey scope is unchanged.
- Clearer active state for the sidebar buttons in both themes.
- Preset area: removing a group asks for confirmation; the add-group button explains when the limit is reached; the save button tooltip reads "Cancel saving a preset" while you choose a slot; a one-time tip explains renaming.
- Options dialog: new Support section with a log level selector (applies immediately), "Open logs folder", "Show tips again" and the app version.
- Camera profile dialog: shows IP address and port, marks the active profile, asks before deleting, shows validation errors as text, and supports Enter/Escape while editing.
- The app now runs on .NET 10 and is self-contained, and writes a daily log file for support.

## Upgrading from 0.3.0

- 1.0.0 uses a new installer (`ViscaCamLink.App-win-Setup.exe`). It installs per user, without administrator rights, to `%LocalAppData%\ViscaCamLink.App\`. 0.3.0 offers the update automatically.
- On the first start, your camera IP address and port, panel visibility, speeds and language are imported from 0.3.0 automatically.
- If the old version is still installed, the app offers to uninstall it once (this needs administrator confirmation). You can also remove it later under Windows Settings → Apps. Old portable folders are left alone; delete them yourself.
- Data locations:
  - settings, presets and hotkeys: `%AppData%\ViscaCamLink\` (kept on update and uninstall)
  - logs: `%LocalAppData%\ViscaCamLink.App\logs\` (kept for 14 days)

## Requirements

- Windows 10 or 11, x64
- No .NET installation needed

## Known limitations

- A double-click on a group tab first switches the group, then starts renaming. Group limit: 25 full groups plus a 26th group with 5 presets; many groups wrap into more tab rows.
- Confirmation dialogs use the standard Windows message box and are not themed. The rename tip is shown in the main window only.
- Escape cannot be bound as a hotkey. Only presets 0–9 of the active group can have hotkeys.
- The connection check sends a small inquiry every 5 seconds; a brief outage briefly shows a connection error, which then clears by itself.
- Diagonal movement at maximum speed is not exactly 45° (pan and tilt maximum speeds differ, as on real cameras).
- The changelog in the update dialog is a GitHub page and is not themed.
- Unhandled errors are logged, but the app closes without a crash dialog.
- Iris and focus control are not part of this release.
- The app is not code-signed yet, so Windows SmartScreen may show a warning on first install.
