namespace workspaces.Objects;

public readonly record struct Command(int Workspace, bool MoveActiveWindow)
{
    public const int MinimumWorkspace = 1;
    public const int MaximumWorkspace = 10;
}
