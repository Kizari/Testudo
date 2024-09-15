using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Testudo;

/// <inheritdoc />
public partial class TestudoWindow : ITestudoWindow
{
    /// <summary>
    /// Holds references to the web message received delegates for each <see cref="TestudoWindow" /> instance.<br />
    /// <b>Key</b> — Pointer to the native instance that this class wraps.<br />
    /// <b>Value</b> — The delegate associated with this instance.
    /// </summary>
    private static readonly ConcurrentDictionary<IntPtr, TestudoWebViewManager.WebMessageReceivedDelegate>
        _webMessageReceivedHandlers = [];

    /// <summary>
    /// Holds references to the web resource requested delegates for each <see cref="TestudoWindow" /> instance.<br />
    /// <b>Key</b> — Pointer to the native instance that this class wraps.<br />
    /// <b>Value</b> — The delegate associated with this instance.
    /// </summary>
    private static readonly ConcurrentDictionary<IntPtr, TestudoWebViewManager.WebResourceRequestedDelegate>
        _webResourceRequestedHandlers = [];

    /// <summary>
    /// Holds a reference to the configuration's dispose method so it can be called when this class disposes.
    /// </summary>
    private readonly Action _configurationFinalizer;

    /// <summary>
    /// A pointer to the native TestudoWindow instance that this class wraps.
    /// </summary>
    private readonly IntPtr _instance;

    /// <summary>
    /// Holds a reference to the web view manager for the lifetime of this window to avoid it being disposed early.
    /// </summary>
    private readonly WebViewManager _webViewManager;

    /// <summary>
    /// Holds a reference to the application service.
    /// </summary>
    private readonly ITestudoApplication _application;

    /// <summary>
    /// Pinned configuration struct that needs to remain stable for the life of the native application.
    /// </summary>
    private GCHandle _configurationHandle;

    private bool _isDisposing;

    /// <summary>
    /// Creates a new native window containing a web view and immediately shows it.
    /// </summary>
    /// <param name="provider">The service provider associated with this window's scope.</param>
    /// <param name="configuration">The window's configuration.</param>
    public TestudoWindow(IServiceProvider provider, TestudoWindowConfiguration configuration)
    {
        // Pass in pointers to the web view manager callbacks
        var pWebMessageReceivedHandler = typeof(TestudoWindow)
            .GetMethod(nameof(WebMessageReceivedHandler), BindingFlags.Static | BindingFlags.Public)!
            .MethodHandle.GetFunctionPointer();
        configuration.SetWebMessageReceivedHandler(pWebMessageReceivedHandler);
        var pWebResourceRequestedHandler = typeof(TestudoWindow)
            .GetMethod(nameof(WebResourceRequestedHandler), BindingFlags.Static | BindingFlags.Public)!
            .MethodHandle.GetFunctionPointer();
        configuration.SetWebResourceRequestedHandler(pWebResourceRequestedHandler);

        // Create a web view manager for this window
        _webViewManager = new TestudoWebViewManager(this, provider,
            provider.GetRequiredService<Dispatcher>(),
            provider.GetRequiredService<IFileProvider>(),
            provider.GetRequiredService<JSComponentConfigurationStore>(),
            out var webMessageReceivedHandler,
            out var webResourceRequestedHandler);

        // Create the native window
        _configurationHandle = GCHandle.Alloc(configuration, GCHandleType.Pinned);
        _configurationFinalizer = configuration.Dispose;

        // Instantiate window on main thread
        _application = provider.GetRequiredService<ITestudoApplication>();
        var instance = IntPtr.Zero;
        _application.Invoke(() => instance = TestudoWindow_Construct(_configurationHandle.AddrOfPinnedObject()));
        _instance = instance;

        // Store the web view callbacks
        _webMessageReceivedHandlers[_instance] = webMessageReceivedHandler;
        _webResourceRequestedHandlers[_instance] = webResourceRequestedHandler;

        // Initialize the web view and show the window
        _application.Invoke(() => TestudoWindow_Show(_instance));
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _isDisposing = true;
        
        _application.Invoke(() => TestudoWindow_Destroy(_instance));
        await _webViewManager.DisposeAsync();

        if (_configurationHandle.IsAllocated)
        {
            _configurationHandle.Free();
        }

        _configurationFinalizer();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public void AddRootComponent<TComponent>()
    {
        _webViewManager.AddRootComponentAsync(typeof(TComponent), "app", ParameterView.Empty)
            .ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public void Navigate(string relativePath)
    {
        if (!_isDisposing)
        {
            TestudoWindow_Navigate(_instance, TestudoWebViewManager.CreateUri(relativePath));
        }
    }

    /// <inheritdoc />
    public void SendMessage(string message)
    {
        if (!_isDisposing)
        {
            TestudoWindow_SendMessage(_instance, message);
        }
    }

    /// <summary>
    /// Calls the appropriate web message received delegate.
    /// </summary>
    /// <param name="instance">The native instance that called this method.</param>
    /// <param name="pMessage">Pointer to the web message <c>string</c>.</param>
    /// <remarks>
    /// This method is static so that pointers to it do not move around in memory at runtime.
    /// </remarks>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static void WebMessageReceivedHandler(IntPtr instance, IntPtr pMessage)
    {
        var message = Marshal.PtrToStringAuto(pMessage)!;
        _webMessageReceivedHandlers[instance](message);
    }

    /// <summary>
    /// Calls the appropriate web resource requested delegate.
    /// </summary>
    /// <param name="instance">The native instance that called this method.</param>
    /// <param name="pUri">Pointer to the URI <c>string</c>.</param>
    /// <param name="outSizeBytes">The size of the response buffer in bytes.</param>
    /// <param name="outContentType">Pointer to the content type <c>string</c>.</param>
    /// <returns>Pointer to the response buffer.</returns>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe IntPtr WebResourceRequestedHandler(IntPtr instance, IntPtr pUri,
        int* outSizeBytes, IntPtr* outContentType)
    {
        var uri = Marshal.PtrToStringAuto(pUri)!;
        var result = _webResourceRequestedHandlers[instance](uri, out var sizeBytes, out var contentType);
        *outSizeBytes = sizeBytes;
        *outContentType = Marshal.StringToHGlobalAuto(contentType);
        return result;
    }
}