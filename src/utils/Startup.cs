using Microsoft.Win32;

namespace workspaces.Utils;

public static class Startup
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "workspaces";

    public static void EnsureCurrentExecutableStartsAtSignIn()
    {
        var executablePath = Application.ExecutablePath;
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new InvalidOperationException("Windows did not provide the executable path for startup registration.");
        }

        using var runKey = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException("The per-user Windows startup registry key could not be opened.");
        runKey.SetValue(ValueName, $"\"{executablePath}\" --background", RegistryValueKind.String);
        runKey.DeleteValue("WorkspaceManager", throwOnMissingValue: false);
    }
}
