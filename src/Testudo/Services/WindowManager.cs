using System.Collections.Concurrent;

namespace Testudo;

/// <inheritdoc />
public class WindowManager(IScopeContext scopeContext) : IWindowManager
{
    private readonly ConcurrentDictionary<Type, ITestudoWindow> _windows = [];

    /// <inheritdoc />
    public void CloseWindow<TComponent>()
    {
        if (_windows.TryRemove(typeof(TComponent), out var window))
        {
            window.Dispose();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var (_, window) in _windows)
        {
            window.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public void OpenWindow<TComponent>(TestudoWindowConfiguration configuration)
    {
        // Ensure the window isn't already open
        if (!_windows.ContainsKey(typeof(TComponent)))
        {
            // Ensure this scope's service provider can be retrieved
            if (scopeContext.ServiceProvider == null)
            {
                throw new InvalidOperationException($"{nameof(IScopeContext)}.{nameof(IScopeContext.ServiceProvider)}" +
                                                    $" must be assigned immediately after the scope is created.");
            }

            // Create the native window
            var window = new TestudoWindow(scopeContext.ServiceProvider, configuration);
            window.AddRootComponent<TComponent>();
            _windows[typeof(TComponent)] = window;
        }
    }
}