using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.Extensions.FileProviders;

namespace Testudo;

/// <inheritdoc cref="WebViewManager" />
public class TestudoWebViewManager : WebViewManager
{
    /// <summary>
    /// Delegate that represents <see cref="OnWebMessageReceived" />.
    /// </summary>
    public delegate void WebMessageReceivedDelegate(string message);

    /// <summary>
    /// Delegate that represents <see cref="OnWebResourceRequested" />.
    /// </summary>
    public delegate IntPtr WebResourceRequestedDelegate(string uri, out int sizeBytes, out string contentType);

    /// <summary>
    /// The path of <c>index.html</c> relative to <c>wwwroot</c>.
    /// </summary>
    private const string HostPageRelativePath = "index.html";

    /// <summary>
    /// The URI scheme used for this application's web resources.
    /// </summary>
    private static readonly string UriScheme = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? "http" // WebView2 doesn't allow using a custom scheme, so use http
        : "app"; // Unix WebViews don't have a way to intercept http:// scheme requests

    /// <summary>
    /// The root URI of this web application.
    /// </summary>
    private static readonly Uri BaseUri = new($"{UriScheme}://localhost/");

    /// <summary>
    /// The window that contains the web view that this class is managing.
    /// </summary>
    private readonly ITestudoWindow _window;

    /// <inheritdoc cref="WebViewManager" />
    /// <param name="window">The native window that contains this web view.</param>
    /// <param name="provider">The service provider associated with this web view's scope.</param>
    /// <param name="dispatcher">A dispatcher that synchronously dispatches actions to the UI thread.</param>
    /// <param name="fileProvider">A file provider that resolves web resources for this application.</param>
    /// <param name="jsComponents">The JS component configuration store for this application.</param>
    /// <param name="webMessageReceivedHandler">The web message received delegate for the window configuration.</param>
    /// <param name="webResourceRequestedHandler">
    /// The web resource requested delegate for the window configuration.
    /// </param>
    public TestudoWebViewManager(ITestudoWindow window,
        IServiceProvider provider,
        Dispatcher dispatcher,
        IFileProvider fileProvider,
        JSComponentConfigurationStore jsComponents,
        out WebMessageReceivedDelegate webMessageReceivedHandler,
        out WebResourceRequestedDelegate webResourceRequestedHandler)
        : base(provider, dispatcher, BaseUri, fileProvider, jsComponents, HostPageRelativePath)
    {
        _window = window;
        webMessageReceivedHandler = OnWebMessageReceived;
        webResourceRequestedHandler = OnWebResourceRequested;
    }

    /// <summary>
    /// Creates an absolute URI from a relative path.
    /// </summary>
    /// <param name="relativePath">The resource path, relative to the web root.</param>
    /// <returns>The absolute URI.</returns>
    public static string CreateUri(string relativePath) => $"{BaseUri.ToString().TrimEnd('/')}{relativePath}";

    /// <inheritdoc />
    protected override void NavigateCore(Uri absoluteUri)
    {
        _window.Navigate(absoluteUri.ToString());
    }

    /// <inheritdoc />
    protected override void SendMessage(string message)
    {
        _window.SendMessage(message);
    }

    /// <summary>
    /// Callback for <see cref="TestudoWindowConfiguration.WebMessageReceivedHandler" />.
    /// </summary>
    /// <param name="message">The message that was received.</param>
    private void OnWebMessageReceived(string message)
    {
        MessageReceived(BaseUri, message);
    }

    /// <summary>
    /// Callback for <see cref="TestudoWindowConfiguration.WebResourceRequestedHandler" />.
    /// Parses the URL and returns the appropriate content buffer.
    /// </summary>
    /// <param name="uri">The URI associated with the desired resource.</param>
    /// <param name="size">The size of the resulting data stream in bytes.</param>
    /// <param name="contentType">The MIME type associated with the resource.</param>
    /// <returns>A pointer to a buffer containing the data of the requested resource.</returns>
    private IntPtr OnWebResourceRequested(string uri, out int size, out string contentType)
    {
        var localPath = new Uri(uri).LocalPath;
        var isFile = Path.HasExtension(localPath);

        // Trim query strings
        var index = uri.IndexOf('?');
        if (index > -1)
        {
            uri = uri[..index];
        }

        // Get the content corresponding to the URI
        if (uri.StartsWith(BaseUri.ToString(), StringComparison.Ordinal)
            && TryGetResponseContent(uri, !isFile, out _, out _,
                out var content, out var headers))
        {
            headers.TryGetValue("Content-Type", out var streamContentType);
            contentType = streamContentType ?? "application/octet-stream";

            using var stream = new MemoryStream();
            content.CopyTo(stream);
            size = (int)stream.Position;

            // Testudo.Native uses a CoTaskMem smart pointer to free "buffer" when it is finished with it
            // so there is no need to free that memory here
            var buffer = Marshal.AllocHGlobal(size);
            Marshal.Copy(stream.ToArray(), 0, buffer, size);
            return buffer;
        }

        size = 0;
        contentType = default!;
        return IntPtr.Zero;
    }
}