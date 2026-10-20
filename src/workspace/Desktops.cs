using System.Runtime.InteropServices;
using workspaces.Utils;

namespace workspaces.Workspace;

public sealed class Desktops : IDesktops, IDisposable
{
    private static readonly Guid ImmersiveShellClsid = new("C2F03A33-21F5-47FA-B4BB-156362A2F239");
    private static readonly Guid InternalManagerService = new("C5E0CDCA-7B6E-41B2-9FC4-D93975CC467B");

    private readonly IComServiceProvider _shell;
    private readonly IVirtualDesktopManagerInternal24H2 _internalManager;
    private readonly IApplicationViewCollection24H2 _applicationViews;
    private readonly WindowMove _windowMove;

    public Desktops()
    {
        EnsureSupportedBuild();

        try
        {
            _shell = (IComServiceProvider)(Activator.CreateInstance(GetComType(ImmersiveShellClsid))
                ?? throw new VirtualDesktopException("Windows Explorer's virtual desktop service is unavailable."));
            var internalInterfaceId = typeof(IVirtualDesktopManagerInternal24H2).GUID;
            var internalService = InternalManagerService;
            _internalManager = (IVirtualDesktopManagerInternal24H2)_shell.QueryService(ref internalService, ref internalInterfaceId);
            var applicationViewsId = typeof(IApplicationViewCollection24H2).GUID;
            var applicationViewsService = applicationViewsId;
            _applicationViews = (IApplicationViewCollection24H2)_shell.QueryService(ref applicationViewsService, ref applicationViewsId);
            _windowMove = new WindowMove(new WindowMoveBackend(this));

            _ = _internalManager.GetCount();
        }
        catch (Exception exception) when (exception is COMException or InvalidCastException or TypeInitializationException)
        {
            ReleaseComObject(_applicationViews);
            ReleaseComObject(_internalManager);
            ReleaseComObject(_shell);
            throw new VirtualDesktopException("This Windows build's virtual desktop interface is not supported by this version of workspaces.", exception);
        }
    }

    public void EnsureAtLeast(int count)
    {
        if (count < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        var desktopCount = _internalManager.GetCount();
        while (desktopCount < count)
        {
            var created = _internalManager.CreateDesktop();
            ReleaseComObject(created);
            desktopCount++;
        }
    }

    public void SwitchTo(int oneBasedWorkspace)
    {
        var desktop = GetDesktop(oneBasedWorkspace);
        try
        {
            _internalManager.SwitchDesktop(desktop);
        }
        finally
        {
            ReleaseComObject(desktop);
        }
    }

    public void MoveWindowTo(IntPtr window, int oneBasedWorkspace)
    {
        _windowMove.Execute(window, oneBasedWorkspace);
    }

    public int GetCurrentWorkspace()
    {
        var current = _internalManager.GetCurrentDesktop();
        try
        {
            var currentId = current.GetId();
            var count = _internalManager.GetCount();
            for (var index = 0; index < count; index++)
            {
                var desktop = GetDesktop(index + 1);
                try
                {
                    if (desktop.GetId() == currentId)
                    {
                        return index + 1;
                    }
                }
                finally
                {
                    ReleaseComObject(desktop);
                }
            }

            throw new VirtualDesktopException("Windows did not return the active virtual desktop in its desktop list.");
        }
        finally
        {
            ReleaseComObject(current);
        }
    }

    public void Dispose()
    {
        ReleaseComObject(_applicationViews);
        ReleaseComObject(_internalManager);
        ReleaseComObject(_shell);
    }

    private IVirtualDesktop24H2 GetDesktop(int oneBasedWorkspace)
    {
        var index = oneBasedWorkspace - 1;
        var count = _internalManager.GetCount();
        if (index < 0 || index >= count)
        {
            throw new ArgumentOutOfRangeException(nameof(oneBasedWorkspace), "That Windows workspace does not exist.");
        }

        _internalManager.GetDesktops(out var desktops);
        try
        {
            var interfaceId = typeof(IVirtualDesktop24H2).GUID;
            desktops.GetAt(index, ref interfaceId, out var desktop);
            return (IVirtualDesktop24H2)desktop;
        }
        finally
        {
            ReleaseComObject(desktops);
        }
    }

    private static void EnsureSupportedBuild()
    {
        if (!OperatingSystem.IsWindows() || !Environment.Is64BitProcess || Environment.OSVersion.Version.Build < 26100)
        {
            throw new VirtualDesktopException("workspaces requires a supported x64 Windows 11 release beginning with 24H2.");
        }

    }

    private static Type GetComType(Guid classId) => Type.GetTypeFromCLSID(classId)
        ?? throw new VirtualDesktopException("Windows did not expose the required virtual desktop COM class.");

    private sealed class WindowMoveBackend(Desktops service) : IWindowMoveBackend
    {
        public object GetDesktop(int workspace) => service.GetDesktop(workspace);

        public object? GetView(IntPtr window)
        {
            var result = service._applicationViews.GetViewForHwnd(window, out var view);
            if (result < 0)
            {
                ReleaseComObject(view);
                Marshal.ThrowExceptionForHR(result);
            }

            return view;
        }

        public bool CanMove(object view) => service._internalManager.CanViewMoveDesktops((IApplicationView24H2)view);

        public void Move(object view, object desktop) =>
            service._internalManager.MoveViewToDesktop((IApplicationView24H2)view, (IVirtualDesktop24H2)desktop);

        public void Release(object value) => ReleaseComObject(value);
    }

    private static void ReleaseComObject(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
        {
            _ = Marshal.FinalReleaseComObject(value);
        }
    }
}

public sealed class VirtualDesktopException : Exception
{
    public VirtualDesktopException(string message)
        : base(message)
    {
    }

    public VirtualDesktopException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
