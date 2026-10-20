using System.Runtime.InteropServices;

namespace workspaces.Workspace;

internal interface IWindowMoveBackend
{
    object GetDesktop(int workspace);
    object? GetView(IntPtr window);
    bool CanMove(object view);
    void Move(object view, object desktop);
    void Release(object value);
}

internal sealed class WindowMove(IWindowMoveBackend backend)
{
    internal void Execute(IntPtr window, int workspace)
    {
        if (window == IntPtr.Zero)
        {
            throw new VirtualDesktopException("There is no movable foreground application window.");
        }

        object? desktop = null;
        object? view = null;
        try
        {
            desktop = backend.GetDesktop(workspace);
            view = backend.GetView(window);
            if (view is null)
            {
                throw new VirtualDesktopException("Windows could not find an application view for this window. Select a normal application window and try again.");
            }

            if (!backend.CanMove(view))
            {
                throw new VirtualDesktopException("Windows does not allow this window to move between workspaces.");
            }

            backend.Move(view, desktop);
        }
        catch (Exception exception) when (exception is COMException or UnauthorizedAccessException)
        {
            var message = exception.HResult == unchecked((int)0x80070005)
                ? "Windows denied permission to move this window. It may be protected or running as administrator."
                : "Windows could not move this window.";
            throw new VirtualDesktopException($"{message} (HRESULT 0x{exception.HResult:X8})", exception);
        }
        finally
        {
            try
            {
                if (view is not null)
                {
                    backend.Release(view);
                }
            }
            finally
            {
                if (desktop is not null)
                {
                    backend.Release(desktop);
                }
            }
        }
    }
}
