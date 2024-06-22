using System.Runtime.InteropServices;

namespace Testudo;

/// <summary>
/// The configuration of a <see cref="TestudoWindow" />.
/// </summary>
/// <param name="initialRelativePath">
/// The path to the initial page to load when the window is first created.
/// This path should be relative to the web root.
/// </param>
[StructLayout(LayoutKind.Sequential)]
public struct TestudoWindowConfiguration(string initialRelativePath)
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)]
    public delegate void WebMessageReceivedDelegate(string message);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)]
    public delegate IntPtr WebResourceRequestedDelegate(string uri, out int numBytes, out string contentType);

    /// <summary>
    /// The URI that the window's web view will navigate to on startup.
    /// </summary>
    public string InitialUri = TestudoWebViewManager.CreateUri(initialRelativePath);

    /// <summary>
    /// A delegate that handles received web messages.
    /// </summary>
    [MarshalAs(UnmanagedType.FunctionPtr)] public WebMessageReceivedDelegate? WebMessageReceivedHandler;

    /// <summary>
    /// A delegate that handles retrieving web resources.
    /// </summary>
    [MarshalAs(UnmanagedType.FunctionPtr)] public WebResourceRequestedDelegate? WebResourceRequestedHandler;
}