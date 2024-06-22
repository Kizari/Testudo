namespace Testudo;

/// <summary>
/// Manages the underlying platform-specific native application.
/// </summary>
/// <remarks>
/// Relies on the singleton instance being created on the main thread to populate <see cref="MainThreadId" /> correctly.
/// </remarks>
public interface ITestudoApplication : IDisposable
{
    /// <summary>
    /// The ID of the application's main thread.
    /// </summary>
    int MainThreadId { get; }

    /// <summary>
    /// Runs the main application loop until this class is disposed.
    /// </summary>
    /// <remarks>
    /// This method is blocking.
    /// </remarks>
    void Run();

    /// <summary>
    /// Invokes the given action on the UI thread.
    /// </summary>
    /// <param name="action">The action to execute on the main thread.</param>
    void Invoke(Action action);
}