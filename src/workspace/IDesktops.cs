namespace workspaces.Workspace;

public interface IDesktops
{
    void EnsureAtLeast(int count);

    void SwitchTo(int oneBasedWorkspace);

    void MoveWindowTo(IntPtr window, int oneBasedWorkspace);

    int GetCurrentWorkspace();
}
