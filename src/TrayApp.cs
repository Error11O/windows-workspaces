using workspaces.Objects;
using workspaces.Workspace;
using workspaces.Utils;

namespace workspaces;

internal sealed class TrayApp : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly KeyboardHook _keyboardHook = new();
    private readonly System.Windows.Forms.Timer _synchronizationTimer = new() { Interval = 1000 };
    private readonly SynchronizationContext _uiContext;
    private Desktops? _desktopService;
    private Manager? _coordinator;
    private string? _lastError;
    private bool _startupRegistered;
    private bool _startupRegistrationAttempted;
    private readonly SettingsStore _settingsStore = new(SettingsStore.DefaultPath, SettingsStore.LegacyPath);
    private AppSettings _settings = new();
    private SettingsForm? _settingsForm;
    private WindowMonitor? _windowMonitor;
    private readonly Icon _icon = new(typeof(TrayApp).Assembly.GetManifestResourceStream("workspaces.workspace.ico")!);

    public TrayApp()
    {
        _uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        _trayIcon = CreateTrayIcon();
        try { _settings = _settingsStore.Load(); }
        catch (Exception exception)
        {
            MessageBox.Show($"Could not load settings; defaults will be used.\n{exception.Message}",
                "workspaces", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        _keyboardHook.Configure(_settings);
        try { _windowMonitor = new WindowMonitor(new NativeWindowMonitorApi()); }
        catch (Exception exception) { ShowError(exception.Message); }
        _keyboardHook.CommandPressed += command => _uiContext.Post(_ => Execute(command), null);
        _synchronizationTimer.Tick += (_, _) => Synchronize();

        Synchronize();
        _synchronizationTimer.Start();
    }

    protected override void ExitThreadCore()
    {
        _synchronizationTimer.Stop();
        _synchronizationTimer.Dispose();
        _keyboardHook.Dispose();
        _settingsForm?.Dispose();
        _windowMonitor?.Dispose();
        _desktopService?.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _icon.Dispose();
        base.ExitThreadCore();
    }

    private NotifyIcon CreateTrayIcon()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Settings…", null, (_, _) => OpenSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());

        return new NotifyIcon
        {
            ContextMenuStrip = menu,
            Icon = _icon,
            Text = "workspaces: starting...",
            Visible = true,
        };
    }

    internal void RequestSettings() => _uiContext.Post(_ => OpenSettings(), null);

    private void OpenSettings()
    {
        if (_settingsForm is { IsDisposed: false })
        {
            _settingsForm.WindowState = FormWindowState.Normal;
            _settingsForm.Show();
            _settingsForm.BringToFront();
            _settingsForm.Activate();
            return;
        }
        _settingsForm = new SettingsForm(_settings, _icon, settings =>
        {
            settings = settings.Validate();
            _settingsStore.Save(settings);
            _settings = settings;
            _keyboardHook.Configure(settings);
            _coordinator = _desktopService is null ? null : new Manager(_desktopService, settings.DesktopCount);
            Synchronize();
        });
        _settingsForm.Show();
    }

    private void TryRegisterStartup()
    {
        try
        {
            Startup.EnsureCurrentExecutableStartsAtSignIn();
            _startupRegistered = true;
        }
        catch (Exception exception)
        {
            ShowError($"Could not enable sign-in startup: {exception.Message}");
        }
    }

    private void Synchronize()
    {
        try
        {
            _desktopService ??= new Desktops();
            _coordinator ??= new Manager(_desktopService, _settings.DesktopCount);

            var synchronized = _coordinator.Synchronize();
            if (!synchronized.Succeeded)
            {
                DisableShortcuts(synchronized.Message!);
                return;
            }

            if (!_keyboardHook.IsInstalled)
            {
                _keyboardHook.Install();
            }

            if (!_startupRegistered && !_startupRegistrationAttempted)
            {
                _startupRegistrationAttempted = true;
                TryRegisterStartup();
            }

            UpdateActiveWorkspaceStatus();
            _lastError = null;
        }
        catch (Exception exception)
        {
            DisableShortcuts(exception.Message);
        }
    }

    private void Execute(Command command)
    {
        if (_coordinator is null)
        {
            return;
        }

        var result = _coordinator.Execute(command, command.MoveActiveWindow ? ForegroundWindow.GetMovableTopLevelWindow() : IntPtr.Zero);
        if (!result.Succeeded)
        {
            ShowError(result.Message!);
            return;
        }

        UpdateActiveWorkspaceStatus();
    }

    private void UpdateActiveWorkspaceStatus()
    {
        var active = _coordinator?.GetActiveWorkspace();
        if (active is { Succeeded: true, Workspace: not null })
        {
            _trayIcon.Text = $"workspaces: {_settings.DesktopCount} | Active: {active.Value.Workspace}";
            return;
        }

        _trayIcon.Text = $"workspaces: {_settings.DesktopCount} | Active: unknown";
    }

    private void DisableShortcuts(string message)
    {
        _keyboardHook.Dispose();
        _desktopService?.Dispose();
        _desktopService = null;
        _coordinator = null;
        ShowError(message);
    }

    private void ShowError(string message)
    {
        _trayIcon.Text = "workspaces: unavailable";
        if (string.Equals(_lastError, message, StringComparison.Ordinal))
        {
            return;
        }

        _lastError = message;
        _trayIcon.ShowBalloonTip(5000, "workspaces", message, ToolTipIcon.Error);
    }
}
