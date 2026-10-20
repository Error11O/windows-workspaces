using workspaces.Objects;

namespace workspaces.Workspace;

public static class Shortcuts
{
    private const uint Vk0 = 0x30;
    private const uint Vk9 = 0x39;

    public static bool TryMap(uint virtualKey, bool windowsKeyDown, bool shiftDown, out Command command)
        => TryMap(virtualKey, windowsKeyDown, shiftDown, 10, out command);

    public static bool TryMap(uint virtualKey, bool modifierDown, bool shiftDown, int desktopCount, out Command command)
    {
        command = default;
        if (!modifierDown || virtualKey < Vk0 || virtualKey > Vk9)
        {
            return false;
        }

        var digit = (int)(virtualKey - Vk0);
        var workspace = digit == 0 ? Command.MaximumWorkspace : digit;
        if (workspace > desktopCount) return false;
        command = new Command(workspace, shiftDown);
        return true;
    }
}
