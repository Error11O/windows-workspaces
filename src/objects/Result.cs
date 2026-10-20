namespace workspaces.Objects;

public readonly record struct Result(bool Succeeded, string? Message, int? Workspace)
{
    public static Result Success(string? message = null, int? workspace = null) => new(true, message, workspace);

    public static Result Failure(string message) => new(false, message, null);
}
