using System.ComponentModel;
using System.Runtime.InteropServices;
using workspaces.Objects;
using workspaces.Workspace;

namespace workspaces.Utils;

public sealed class KeyboardHook : IDisposable
{
    private static readonly nint InjectedInputTag = 0x57534D47;
    private readonly WindowsApi.LowLevelKeyboardProc _callback;
    private readonly Func<WindowsApi.Input[], uint> _sendInput;
    private readonly HashSet<uint> _suppressedKeys = [];
    private readonly HashSet<uint> _shiftKeysDown = [];
    private readonly HashSet<uint> _windowsKeysDown = [];
    private readonly HashSet<uint> _modifierKeysToMask = [];
    private readonly HashSet<uint> _keysDown = [];
    private ShortcutModifier _modifier;
    private int _desktopCount = 10;
    private AppSettings? _pendingSettings;
    private nint _hook;

    public void Configure(AppSettings settings)
    {
        _pendingSettings = settings.Validate();
        ApplyPendingSettings();
    }

    private void ApplyPendingSettings()
    {
        if (_keysDown.Count != 0 || _pendingSettings is null) return;
        _modifier = _pendingSettings.Modifier;
        _desktopCount = _pendingSettings.DesktopCount;
        _pendingSettings = null;
    }

    public KeyboardHook() : this(inputs => WindowsApi.SendInput(
        (uint)inputs.Length, inputs, Marshal.SizeOf<WindowsApi.Input>()))
    {
    }

    internal KeyboardHook(Func<WindowsApi.Input[], uint> sendInput)
    {
        _callback = HookCallback;
        _sendInput = sendInput;
    }

    public event Action<Command>? CommandPressed;

    public bool IsInstalled => _hook != IntPtr.Zero;

    public void Install()
    {
        if (IsInstalled)
        {
            return;
        }

        _hook = WindowsApi.SetWindowsHookEx(WindowsApi.WhKeyboardLl, _callback, IntPtr.Zero, 0);
        if (_hook == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows could not install the workspace keyboard hook.");
        }
    }

    public void Dispose()
    {
        if (_hook != IntPtr.Zero)
        {
            _ = WindowsApi.UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }

        _suppressedKeys.Clear();
        _shiftKeysDown.Clear();
        _windowsKeysDown.Clear();
        _modifierKeysToMask.Clear();
        _keysDown.Clear();
        ApplyPendingSettings();
    }

    private nint HookCallback(int code, nint wParam, nint lParam)
    {
        if (code < 0)
        {
            return WindowsApi.CallNextHookEx(_hook, code, wParam, lParam);
        }

        var message = unchecked((int)wParam);
        var keyboard = Marshal.PtrToStructure<WindowsApi.KbdLlHookStruct>(lParam);
        var key = keyboard.VirtualKeyCode;

        // Our replacement release must reach Windows, without changing physical-key tracking
        // or recursively replacing itself. Other software's injected keys still work normally.
        if ((keyboard.Flags & WindowsApi.LlkhfInjected) != 0 && keyboard.ExtraInfo == InjectedInputTag)
        {
            return WindowsApi.CallNextHookEx(_hook, code, wParam, lParam);
        }

        if (message is WindowsApi.WmKeyUp or WindowsApi.WmSysKeyUp) _keysDown.Remove(key);
        else if (message is WindowsApi.WmKeyDown or WindowsApi.WmSysKeyDown) _keysDown.Add(key);
        try { return ProcessKey(code, wParam, lParam, message, key); }
        finally { ApplyPendingSettings(); }
    }

    private nint ProcessKey(int code, nint wParam, nint lParam, int message, uint key)
    {
        if (message is WindowsApi.WmKeyUp or WindowsApi.WmSysKeyUp)
        {
            if (key is WindowsApi.VkLwin or WindowsApi.VkRwin or WindowsApi.VkLmenu)
            {
                _windowsKeysDown.Remove(key);
                if (_modifierKeysToMask.Remove(key) && MaskAndReleaseModifier(key))
                {
                    return 1;
                }

                // Never swallow a release unless its replacement was successfully queued.
                return WindowsApi.CallNextHookEx(_hook, code, wParam, lParam);
            }

            if (IsShiftKey(key))
            {
                _shiftKeysDown.Remove(key);
                return WindowsApi.CallNextHookEx(_hook, code, wParam, lParam);
            }

            return _suppressedKeys.Remove(key)
                ? 1
                : WindowsApi.CallNextHookEx(_hook, code, wParam, lParam);
        }

        if (message is not (WindowsApi.WmKeyDown or WindowsApi.WmSysKeyDown))
        {
            return WindowsApi.CallNextHookEx(_hook, code, wParam, lParam);
        }

        if (key is WindowsApi.VkLwin or WindowsApi.VkRwin)
        {
            _windowsKeysDown.Add(key);
            return WindowsApi.CallNextHookEx(_hook, code, wParam, lParam);
        }

        if (IsShiftKey(key))
        {
            _shiftKeysDown.Add(key);
            return WindowsApi.CallNextHookEx(_hook, code, wParam, lParam);
        }

        if (_suppressedKeys.Contains(key)) return 1;
        var modifierDown = _modifier == ShortcutModifier.Windows ? _windowsKeysDown.Count > 0 :
            _keysDown.Contains(WindowsApi.VkLmenu) && !_keysDown.Contains(WindowsApi.VkRmenu) &&
            !_keysDown.Contains(WindowsApi.VkControl) && !_keysDown.Contains(WindowsApi.VkLcontrol) &&
            !_keysDown.Contains(WindowsApi.VkRcontrol) && _windowsKeysDown.Count == 0;
        if (!Shortcuts.TryMap(key, modifierDown, _shiftKeysDown.Count > 0, _desktopCount, out var command))
        {
            return WindowsApi.CallNextHookEx(_hook, code, wParam, lParam);
        }

        if (_suppressedKeys.Add(key))
        {
            if (_modifier == ShortcutModifier.Windows) _modifierKeysToMask.UnionWith(_windowsKeysDown);
            else _modifierKeysToMask.Add(WindowsApi.VkLmenu);
            CommandPressed?.Invoke(command);
        }

        return 1;
    }

    private static bool IsShiftKey(uint key) => key is WindowsApi.VkShift or WindowsApi.VkLshift or WindowsApi.VkRshift;

    private bool MaskAndReleaseModifier(uint key)
    {
        WindowsApi.Input[] inputs =
        [
            CreateInput(WindowsApi.VkMenuMask, 0),
            CreateInput(WindowsApi.VkMenuMask, WindowsApi.KeyeventfKeyup),
            CreateInput((ushort)key, WindowsApi.KeyeventfKeyup |
                (key == WindowsApi.VkLmenu ? 0 : WindowsApi.KeyeventfExtendedkey)),
        ];
        return _sendInput(inputs) == (uint)inputs.Length;
    }

    private static WindowsApi.Input CreateInput(ushort key, uint flags) => new()
    {
        Type = WindowsApi.InputKeyboard,
        Data = new WindowsApi.InputUnion
        {
            Keyboard = new WindowsApi.KeyboardInput
            {
                VirtualKey = key,
                Flags = flags,
                ExtraInfo = InjectedInputTag,
            },
        },
    };
}
