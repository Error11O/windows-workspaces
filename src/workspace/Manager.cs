using workspaces.Objects;

namespace workspaces.Workspace;

public sealed class Manager
{
    private readonly IDesktops desktops;
    public int RequiredWorkspaceCount { get; }

    public Manager(IDesktops desktops, int desktopCount = 10)
    {
        if (desktopCount is < 1 or > 10) throw new ArgumentOutOfRangeException(nameof(desktopCount));
        this.desktops = desktops;
        RequiredWorkspaceCount = desktopCount;
    }

    public Result Synchronize()
    {
        try
        {
            desktops.EnsureAtLeast(RequiredWorkspaceCount);
            return Result.Success();
        }
        catch (Exception exception)
        {
            return Result.Failure(exception.Message);
        }
    }

    public Result Execute(Command command, IntPtr foregroundWindow)
    {
        if (command.Workspace < Command.MinimumWorkspace || command.Workspace > RequiredWorkspaceCount)
        {
            return Result.Failure("The requested workspace is outside the supported range.");
        }

        try
        {
            desktops.EnsureAtLeast(RequiredWorkspaceCount);
            if (command.MoveActiveWindow)
            {
                if (foregroundWindow == IntPtr.Zero)
                {
                    return Result.Failure("There is no movable foreground application window.");
                }

                desktops.MoveWindowTo(foregroundWindow, command.Workspace);
                return Result.Success($"Moved the active window to workspace {command.Workspace}.");
            }

            desktops.SwitchTo(command.Workspace);
            return Result.Success($"Switched to workspace {command.Workspace}.");
        }
        catch (Exception exception)
        {
            return Result.Failure(exception.Message);
        }
    }

    public Result GetActiveWorkspace()
    {
        try
        {
            return Result.Success(workspace: desktops.GetCurrentWorkspace());
        }
        catch (Exception exception)
        {
            return Result.Failure(exception.Message);
        }
    }
}

