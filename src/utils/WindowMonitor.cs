using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace workspaces.Utils;

internal sealed record WindowInfo(bool Visible = true, bool TopLevel = true,
    bool Owned = false, bool ToolWindow = false, bool Dialog = false, bool CanMaximize = true, bool OwnProcess = false);

internal interface IWindowMonitorApi
{
    IEnumerable<nint> EnumerateWindows();
    IDisposable Subscribe(Action<nint, bool> onWindow); // true = shown, false = destroyed
    WindowInfo? Inspect(nint window);
    void Maximize(nint window);
}

internal sealed class WindowMonitor : IDisposable
{
    private readonly IWindowMonitorApi _api;
    private readonly HashSet<nint> _seen = [];
    private readonly IDisposable _subscription;
    private bool _disposed;

    internal WindowMonitor(IWindowMonitorApi api)
    {
        _api = api;
        _subscription = api.Subscribe(OnWindow);
        try { _seen.UnionWith(api.EnumerateWindows()); }
        catch { _subscription.Dispose(); throw; }
    }

    private void OnWindow(nint window, bool shown)
    {
        if (_disposed || window == 0) return;
        if (!shown) { _seen.Remove(window); return; }
        if (!_seen.Add(window)) return;
        try
        {
            var info = _api.Inspect(window);
            if (info is { Visible: true, TopLevel: true, Owned: false, ToolWindow: false,
                Dialog: false, CanMaximize: true, OwnProcess: false })
                _api.Maximize(window);
        }
        catch
        {
            // A closing or protected process must never interrupt keyboard handling.
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _subscription.Dispose();
        _seen.Clear();
    }
}

internal sealed class NativeWindowMonitorApi : IWindowMonitorApi
{
    public IEnumerable<nint> EnumerateWindows()
    {
        var windows = new List<nint>();
        if (!EnumWindows((window, _) => { windows.Add(window); return true; }, 0))
            throw new Win32Exception(Marshal.GetLastWin32Error());
        return windows;
    }

    public IDisposable Subscribe(Action<nint, bool> onWindow) => new Subscription(onWindow);

    public WindowInfo? Inspect(nint window)
    {
        if (!IsWindow(window) || WindowsApi.GetAncestor(window, WindowsApi.GaRoot) != window) return null;
        WindowsApi.GetWindowThreadProcessId(window, out var processId);
        if (processId == Environment.ProcessId) return null;
        var style = GetWindowLongPtr(window, -16).ToInt64();
        var extendedStyle = GetWindowLongPtr(window, -20).ToInt64();
        var className = new StringBuilder(256);
        WindowsApi.GetClassName(window, className, className.Capacity);
        return new WindowInfo(WindowsApi.IsWindowVisible(window),
            (style & 0x40000000) == 0, GetWindow(window, 4) != 0,
            (extendedStyle & 0x80) != 0, className.ToString() == "#32770" || (extendedStyle & 1) != 0,
            (style & 0x10000) != 0);
    }

    public void Maximize(nint window) => _ = ShowWindowAsync(window, 3); // SW_MAXIMIZE

    private sealed class Subscription : IDisposable
    {
        private readonly WinEventProc _callback;
        private nint _hook;
        internal Subscription(Action<nint, bool> onWindow)
        {
            _callback = (_, eventId, window, objectId, childId, _, _) =>
            {
                if (objectId == 0 && childId == 0 && eventId is 0x8001 or 0x8002)
                    onWindow(window, eventId == 0x8002);
            };
            _hook = SetWinEventHook(0x8001, 0x8002, 0, _callback, 0, 0, 2);
            if (_hook == 0) throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not monitor new application windows.");
        }
        public void Dispose()
        {
            if (_hook != 0) { UnhookWinEvent(_hook); _hook = 0; }
            GC.KeepAlive(_callback);
        }
    }

    private delegate bool EnumWindowsProc(nint window, nint parameter);
    private delegate void WinEventProc(nint hook, uint eventId, nint window, int objectId, int childId, uint thread, uint time);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumWindows(EnumWindowsProc callback, nint parameter);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWinEventHook(uint minimum, uint maximum, nint module, WinEventProc callback, uint process, uint thread, uint flags);
    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(nint hook);
    [DllImport("user32.dll")]
    private static extern bool IsWindow(nint window);
    [DllImport("user32.dll")]
    private static extern nint GetWindow(nint window, uint command);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr(nint window, int index);
    [DllImport("user32.dll")]
    private static extern bool ShowWindowAsync(nint window, int command);
}
