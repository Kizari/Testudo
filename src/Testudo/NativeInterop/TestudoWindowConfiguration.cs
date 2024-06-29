using System.Runtime.InteropServices;

namespace Testudo;

/// <summary>
/// The configuration of a <see cref="TestudoWindow" />.
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public struct TestudoWindowConfiguration : IDisposable
{
    /// <summary>
    /// Pointer to the window's .ico file in memory.
    /// </summary>
    public required IntPtr Icon;
    
    /// <summary>
    /// The text to display in the title bar of the window.
    /// </summary>
    private IntPtr _title;

    /// <summary>
    /// The URI that the window's web view will navigate to on startup.
    /// </summary>
    private IntPtr _initialUri;

    /// <summary>
    /// The position of the left edge of the window relative to the left of the screen.
    /// </summary>
    public int Left;

    /// <summary>
    /// The position of the top edge of the window relative to the top of the screen.
    /// </summary>
    public int Top;

    /// <summary>
    /// The width of the window in pixels.
    /// </summary>
    public required int Width;

    /// <summary>
    /// The height of the window in pixels.
    /// </summary>
    public required int Height;

    /// <summary>
    /// Whether to center the window on the screen.
    /// </summary>
    /// <remarks>Overrides <see cref="Left" /> and <see cref="Top" />.</remarks>
    public bool IsCentered;

    /// <summary>
    /// A delegate that handles received web messages.
    /// </summary>
    private IntPtr WebMessageReceivedHandler;

    /// <summary>
    /// A delegate that handles retrieving web resources.
    /// </summary>
    private IntPtr WebResourceRequestedHandler;

    /// <inheritdoc cref="_title" />
    public required string Title
    {
        set => _title = Marshal.StringToHGlobalAuto(value);
    }

    /// <summary>
    /// The path to the initial page to load when the window is first created.
    /// This path should be relative to the web root.
    /// </summary>
    public required string InitialRelativePath
    {
        set => _initialUri = Marshal.StringToHGlobalAuto(TestudoWebViewManager.CreateUri(value));
    }

    /// <inheritdoc cref="WebMessageReceivedHandler" />
    public void SetWebMessageReceivedHandler(IntPtr handler) => WebMessageReceivedHandler = handler;

    /// <inheritdoc cref="WebResourceRequestedHandler" />
    public void SetWebResourceRequestedHandler(IntPtr handler) => WebResourceRequestedHandler = handler;

    /// <inheritdoc />
    public void Dispose()
    {
        Marshal.FreeHGlobal(_initialUri);
        Marshal.FreeHGlobal(_title);
    }
}