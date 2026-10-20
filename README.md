# workspaces

Requires x64 Windows 11 24H2 or later. Download **workspaces.exe** from this repository's **Releases**, put it in a permanent folder, and run it. The executable includes .NET; no runtime installation is needed.

Opening the executable shows Settings. Opening it again brings the existing Settings window forward. Right-click the three-dot tray icon for **Settings** or **Exit**.

- **Windows key + 1–9 / 0** switches desktops (0 means desktop 10).
- **Shift + Windows key + 1–9 / 0** moves the active window without switching desktops.
- Settings lets you choose **Left Alt** instead of the Windows key and a desktop count from **1–10**. Right Alt/AltGr is excluded.

Settings are saved in `%LOCALAPPDATA%\workspaces\settings.json`. Existing WorkspaceManager settings are loaded if the new file is absent.

Startup at sign-in is registered automatically for the current user. It uses `--background` to run in the tray without opening Settings. To set the same startup entry manually, replace the example path below.

PowerShell:

```powershell
$exe = 'C:\My Apps\workspaces.exe'
New-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'workspaces' -Value ('"{0}" --background' -f $exe) -PropertyType String -Force
```

CMD:

```cmd
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v workspaces /t REG_SZ /d "\"C:\My Apps\workspaces.exe\" --background" /f
```

Licensed under the [MIT License](LICENSE).
