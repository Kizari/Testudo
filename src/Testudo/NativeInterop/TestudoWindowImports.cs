using System.Runtime.InteropServices;

namespace Testudo;

public partial class TestudoWindow
{
    /// <inheritdoc cref="TestudoApplication.LibraryName" />
    private const string LibraryName = TestudoApplication.LibraryName;

    /// <summary>
    /// Creates a new native window.
    /// </summary>
    /// <param name="configuration">The window's configuration.</param>
    /// <returns>A pointer to the newly created instance.</returns>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    private static extern IntPtr TestudoWindow_Construct(IntPtr configuration);

    /// <summary>
    /// Closes and cleans up a web view window without terminating the application.
    /// </summary>
    /// <param name="instance">A pointer to the native instance to destroy.</param>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    private static extern void TestudoWindow_Destroy(IntPtr instance);

    /// <summary>
    /// Initializes the window's embedded web view, then shows the window.
    /// </summary>
    /// <param name="instance">A pointer to the native instance to show.</param>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    private static extern void TestudoWindow_Show(IntPtr instance);

    /// <summary>
    /// Navigates the given window's web view to the given URI.
    /// </summary>
    /// <param name="instance">A pointer to the native window instance whose web view should be navigated.</param>
    /// <param name="uri">The URI to navigate to.</param>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, SetLastError = true, CharSet = CharSet.Auto)]
    private static extern void TestudoWindow_Navigate(IntPtr instance, string uri);

    /// <summary>
    /// Sends a JavaScript message to the given window's web view for evaluation.
    /// </summary>
    /// <param name="instance">
    /// A pointer to the native window instance whose web view should evaluate the JavaScript.
    /// </param>
    /// <param name="message">The JavaScript message to send and evaluate.</param>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, SetLastError = true, CharSet = CharSet.Auto)]
    private static extern void TestudoWindow_SendMessage(IntPtr instance, string message);
}