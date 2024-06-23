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
    /// A pointer to the native TestudoWindow instance.
    /// </summary>
    private readonly IntPtr _instance;

    /// <summary>
    /// Holds a reference to the web view manager for the lifetime of this window to avoid it being disposed early.
    /// </summary>
    private readonly WebViewManager _webViewManager;

    /// <summary>
    /// Holds a reference to the window configuration to prevent the delegates being garbage collected while
    /// the native window still needs to be able to call them.
    /// </summary>
    private readonly TestudoWindowConfiguration _configuration;

    /// <summary>
    /// Creates a new native window containing a web view and immediately shows it.
    /// </summary>
    /// <param name="provider">The service provider associated with this window's scope.</param>
    /// <param name="configuration">The window's configuration.</param>
    public TestudoWindow(IServiceProvider provider, TestudoWindowConfiguration configuration)
    {
        // Create a web view manager for this window
        _webViewManager = new TestudoWebViewManager(this, provider,
            provider.GetRequiredService<Dispatcher>(),
            provider.GetRequiredService<IFileProvider>(),
            provider.GetRequiredService<JSComponentConfigurationStore>(),
            out var webMessageReceivedHandler,
            out var webResourceRequestedHandler);
        
        configuration.SetWebMessageReceivedHandler(webMessageReceivedHandler);
        configuration.SetWebResourceRequestedHandler(webResourceRequestedHandler);

        // Create the native window
        _configuration = configuration;
        var pConfiguration = Marshal.AllocHGlobal(Marshal.SizeOf(configuration));
        Marshal.StructureToPtr(configuration, pConfiguration, false);
        _instance = TestudoWindow_Construct(pConfiguration);
        Marshal.FreeHGlobal(pConfiguration);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _webViewManager.DisposeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        TestudoWindow_Destroy(_instance);
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
        TestudoWindow_Navigate(_instance, TestudoWebViewManager.CreateUri(relativePath));
    }

    /// <inheritdoc />
    public void SendMessage(string message)
    {
        TestudoWindow_SendMessage(_instance, message);
    }
}