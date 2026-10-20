using System.Runtime.InteropServices;

namespace workspaces.Utils;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("3F07F4BE-B107-441A-AF0F-39D82529072C")]
internal interface IVirtualDesktop24H2
{
    [return: MarshalAs(UnmanagedType.Bool)]
    bool IsViewVisible([MarshalAs(UnmanagedType.IUnknown)] object view);

    Guid GetId();
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("53F5CA0B-158F-4124-900C-057158060B27")]
internal interface IVirtualDesktopManagerInternal24H2
{
    int GetCount();

    void MoveViewToDesktop(IApplicationView24H2 view, IVirtualDesktop24H2 desktop);

    [return: MarshalAs(UnmanagedType.Bool)]
    bool CanViewMoveDesktops(IApplicationView24H2 view);

    IVirtualDesktop24H2 GetCurrentDesktop();

    void GetDesktops(out IObjectArray24H2 desktops);

    [PreserveSig]
    int GetAdjacentDesktop(IVirtualDesktop24H2 from, int direction, out IVirtualDesktop24H2 desktop);

    void SwitchDesktop(IVirtualDesktop24H2 desktop);

    void SwitchDesktopAndMoveForegroundView(IVirtualDesktop24H2 desktop);

    IVirtualDesktop24H2 CreateDesktop();
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("372E1D3B-38D3-42E4-A15B-8AB2B178F513")]
internal interface IApplicationView24H2
{
    // Opaque IApplicationView pointer. No methods are invoked, so only the
    // IUnknown prefix is needed; the native interface also has IInspectable slots.
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("1841C6D7-4F9D-42C0-AF41-8747538F10E5")]
internal interface IApplicationViewCollection24H2
{
    // Keep all slots preceding GetViewForHwnd, even though they are not called.
    [PreserveSig]
    int GetViews(out IObjectArray24H2 views);

    [PreserveSig]
    int GetViewsByZOrder(out IObjectArray24H2 views);

    [PreserveSig]
    int GetViewsByAppUserModelId([MarshalAs(UnmanagedType.LPWStr)] string id, out IObjectArray24H2 views);

    [PreserveSig]
    int GetViewForHwnd(nint window, out IApplicationView24H2? view);
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("92CA9DCD-5622-4BBA-A805-5E9F541BD8C9")]
internal interface IObjectArray24H2
{
    void GetCount(out int count);

    void GetAt(int index, ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out object value);
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("6D5140C1-7436-11CE-8034-00AA006009FA")]
internal interface IComServiceProvider
{
    [return: MarshalAs(UnmanagedType.IUnknown)]
    object QueryService(ref Guid service, ref Guid riid);
}
