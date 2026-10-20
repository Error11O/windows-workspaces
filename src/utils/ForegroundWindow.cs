using System.Text;

namespace workspaces.Utils;

public static class ForegroundWindow
{
    private static readonly HashSet<string> ShellWindowClasses = new(StringComparer.Ordinal)
    {
        "Shell_TrayWnd",
        "Progman",
        "WorkerW",
    };

    public static IntPtr GetMovableTopLevelWindow()
    {
        var foreground = WindowsApi.GetForegroundWindow();
        if (foreground == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        var root = WindowsApi.GetAncestor(foreground, WindowsApi.GaRoot);
        if (root == IntPtr.Zero || !WindowsApi.IsWindowVisible(root))
        {
            return IntPtr.Zero;
        }

        WindowsApi.GetWindowThreadProcessId(root, out var processId);
        if (processId == Environment.ProcessId || IsShellWindow(root))
        {
            return IntPtr.Zero;
        }

        return root;
    }

    private static bool IsShellWindow(IntPtr window)
    {
        var className = new StringBuilder(256);
        _ = WindowsApi.GetClassName(window, className, className.Capacity);
        return ShellWindowClasses.Contains(className.ToString());
    }
}
