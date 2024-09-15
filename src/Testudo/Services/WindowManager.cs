using System.Collections.Concurrent;

namespace Testudo;

/// <inheritdoc />
public class WindowManager(IScopeContext scopeContext) : IWindowManager
{
    private readonly ConcurrentDictionary<Type, ITestudoWindow> _windows = [];

    /// <inheritdoc />
    public async Task CloseWindowAsync<TComponent>()
    {
        if (_windows.TryRemove(typeof(TComponent), out var window))
        {
            await window.DisposeAsync();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        foreach (var (_, window) in _windows)
        {
            await window.DisposeAsync();
        }
        
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public Task OpenWindowAsync<TComponent>(TestudoWindowConfiguration configuration)
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

        return Task.CompletedTask;
    }
}