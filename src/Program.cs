using workspaces.Utils;

namespace workspaces;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        var background = args.Contains("--background", StringComparer.OrdinalIgnoreCase);

        using var singleInstance = new SingleInstance("Local\\workspaces-5BFDC305-43EF-4FC3-AB3A-7E561654CCF8");
        if (!singleInstance.IsOwner)
        {
            if (!background) singleInstance.RequestActivation();
            return;
        }

        using var applicationContext = new TrayApp();
        using var activation = singleInstance.ListenForActivation(applicationContext.RequestSettings);
        if (!background) applicationContext.RequestSettings();
        Application.Run(applicationContext);
    }
}
