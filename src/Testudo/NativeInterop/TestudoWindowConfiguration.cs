using System.Runtime.InteropServices;

namespace Testudo;

/// <summary>
/// The configuration of a <see cref="TestudoWindow" />.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct TestudoWindowConfiguration
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)]
    public delegate void WebMessageReceivedDelegate(string message);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)]
    public delegate IntPtr WebResourceRequestedDelegate(string uri, out int sizeBytes, out string contentType);

    /// <summary>
    /// The URI that the window's web view will navigate to on startup.
    /// </summary>
    private string _initialUri;

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
    /// <remarks>Overrides <see cref="Left"/> and <see cref="Top"/>.</remarks>
    public bool IsCentered;

    /// <summary>
    /// A delegate that handles received web messages.
    /// </summary>
    [MarshalAs(UnmanagedType.FunctionPtr)] private WebMessageReceivedDelegate? WebMessageReceivedHandler;

    /// <summary>
    /// A delegate that handles retrieving web resources.
    /// </summary>
    [MarshalAs(UnmanagedType.FunctionPtr)] private WebResourceRequestedDelegate? WebResourceRequestedHandler;

    /// <summary>
    /// The path to the initial page to load when the window is first created.
    /// This path should be relative to the web root.
    /// </summary>
    public required string InitialRelativePath
    {
        set => _initialUri = TestudoWebViewManager.CreateUri(value);
    }

    /// <inheritdoc cref="WebMessageReceivedHandler"/>
    public void SetWebMessageReceivedHandler(WebMessageReceivedDelegate handler) => 
        WebMessageReceivedHandler = handler;

    /// <inheritdoc cref="WebResourceRequestedHandler"/>
    public void SetWebResourceRequestedHandler(WebResourceRequestedDelegate handler) =>
        WebResourceRequestedHandler = handler;
}