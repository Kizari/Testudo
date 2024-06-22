namespace Testudo;

/// <summary>
/// Manages a native window containing a web view.
/// </summary>
public interface ITestudoWindow : IDisposable
{
    /// <summary>
    /// Adds the root Razor component to this window's web view.
    /// </summary>
    /// <typeparam name="TComponent">The type of the Razor component to add.</typeparam>
    void AddRootComponent<TComponent>();

    /// <summary>
    /// Navigates this window's web view to the given URI.
    /// </summary>
    /// <param name="uri">The URI to navigate to.</param>
    void Navigate(string uri);

    /// <summary>
    /// Sends a JavaScript message to this window's web view for evaluation.
    /// </summary>
    /// <param name="message">The JavaScript message to send and evaluate.</param>
    void SendMessage(string message);
}