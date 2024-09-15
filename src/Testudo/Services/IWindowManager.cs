namespace Testudo;

/// <summary>
/// Manages all WebView windows for this process.
/// </summary>
public interface IWindowManager : IAsyncDisposable
{
    /// <summary>
    /// Opens a new native window containing a WebView populated with the given Razor component.
    /// </summary>
    /// <param name="configuration">The configuration of the window.</param>
    /// <typeparam name="TComponent">The type of the Razor Component to host in the window.</typeparam>
    Task OpenWindowAsync<TComponent>(TestudoWindowConfiguration configuration);

    /// <summary>
    /// Closes a native window.
    /// </summary>
    /// <typeparam name="TComponent">The type of the Razor Component associated with the window.</typeparam>
    Task CloseWindowAsync<TComponent>();
}