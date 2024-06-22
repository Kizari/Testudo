// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Testudo;

/// <summary>
/// A multifunctional service scope for <see cref="ServiceProvider" />.<br />
/// <b>Cloned from the .NET 8 runtime.</b>
/// </summary>
/// <remarks>
/// Small modifications have been made to allow for a scope model that will work with Razor components.
/// Blazor creates a new scope per component when running in client-side mode.
/// <see cref="CreateScope" /> has been altered to return the current WebView's scope instead of creating a new one
/// to circumvent this behaviour.
/// </remarks>
[DebuggerDisplay("{DebuggerToString(),nq}")]
[DebuggerTypeProxy(typeof(ServiceProviderEngineScopeDebugView))]
internal sealed class ServiceProviderEngineScope : IServiceScope, IServiceProvider, IKeyedServiceProvider,
    IAsyncDisposable, IServiceScopeFactory
{
    private List<object>? _disposables;

    public ServiceProviderEngineScope(TestudoServiceProvider provider, bool isRootScope)
    {
        ResolvedServices = new Dictionary<ServiceCacheKey, object?>();
        RootProvider = provider;
        IsRootScope = isRootScope;
    }

    // For testing and debugging only
    internal IList<object> Disposables => _disposables ?? (IList<object>)Array.Empty<object>();

    internal Dictionary<ServiceCacheKey, object?> ResolvedServices { get; }

    internal bool Disposed { get; private set; }

    // This lock protects state on the scope, in particular, for the root scope, it protects
    // the list of disposable entries only, since ResolvedServices are cached on CallSites
    // For other scopes, it protects ResolvedServices and the list of disposables
    internal object Sync => ResolvedServices;

    public bool IsRootScope { get; }

    internal TestudoServiceProvider RootProvider { get; }

    public ValueTask DisposeAsync()
    {
        var toDispose = BeginDispose();

        if (toDispose != null)
        {
            try
            {
                for (var i = toDispose.Count - 1; i >= 0; i--)
                {
                    var disposable = toDispose[i];
                    if (disposable is IAsyncDisposable asyncDisposable)
                    {
                        var vt = asyncDisposable.DisposeAsync();
                        if (!vt.IsCompletedSuccessfully)
                        {
                            return Await(i, vt, toDispose);
                        }

                        // If its a IValueTaskSource backed ValueTask,
                        // inform it its result has been read so it can reset
                        vt.GetAwaiter().GetResult();
                    }
                    else
                    {
                        ((IDisposable)disposable).Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                return new ValueTask(Task.FromException(ex));
            }
        }

        return default;

        static async ValueTask Await(int i, ValueTask vt, List<object> toDispose)
        {
            await vt.ConfigureAwait(false);
            // vt is acting on the disposable at index i,
            // decrement it and move to the next iteration
            i--;

            for (; i >= 0; i--)
            {
                var disposable = toDispose[i];
                if (disposable is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                }
                else
                {
                    ((IDisposable)disposable).Dispose();
                }
            }
        }
    }

    public object? GetKeyedService(Type serviceType, object? serviceKey)
    {
        if (Disposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }

        return RootProvider.GetKeyedService(serviceType, serviceKey, this);
    }

    public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
    {
        if (Disposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }

        return RootProvider.GetRequiredKeyedService(serviceType, serviceKey, this);
    }

    public object? GetService(Type serviceType)
    {
        if (Disposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }

        if (serviceType == typeof(IServiceScopeFactory))
        {
            return this;
        }

        return RootProvider.GetService(ServiceIdentifier.FromServiceType(serviceType), this);
    }

    public IServiceProvider ServiceProvider => this;

    public void Dispose()
    {
        var toDispose = BeginDispose();

        if (toDispose != null)
        {
            for (var i = toDispose.Count - 1; i >= 0; i--)
            {
                if (toDispose[i] is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                else
                {
                    throw new InvalidOperationException($"Cannot dispose of service " +
                                                        $"{toDispose[i].GetType().Name} as it only has an async " +
                                                        $"dispose method.");
                }
            }
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// This method has been altered to simply return the current scope when calling it from a non-root scope.
    /// This ensures that scoped services are consistent across all components within a single WebView.
    /// </remarks>
    public IServiceScope CreateScope()
    {
        if (IsRootScope)
        {
            return RootProvider.CreateScope();
        }

        return this;
    }

    [return: NotNullIfNotNull(nameof(service))]
    internal object? CaptureDisposable(object? service)
    {
        if (ReferenceEquals(this, service) || !(service is IDisposable || service is IAsyncDisposable))
        {
            return service;
        }

        var disposed = false;
        lock (Sync)
        {
            if (Disposed)
            {
                disposed = true;
            }
            else
            {
                _disposables ??= new List<object>();

                _disposables.Add(service);
            }
        }

        // Don't run customer code under the lock
        if (disposed)
        {
            if (service is IDisposable disposable)
            {
                disposable.Dispose();
            }
            else
            {
                // sync over async, for the rare case that an object only implements IAsyncDisposable and may end up starving the thread pool.
                var localService = service; // copy to avoid closure on other paths
                Task.Run(() => ((IAsyncDisposable)localService).DisposeAsync().AsTask()).GetAwaiter().GetResult();
            }

            throw new ObjectDisposedException(GetType().Name);
        }

        return service;
    }

    private List<object>? BeginDispose()
    {
        lock (Sync)
        {
            if (Disposed)
            {
                return null;
            }

            // Track statistics about the scope (number of disposable objects and number of disposed services)
            DependencyInjectionEventSource.Log.ScopeDisposed(RootProvider.GetHashCode(), ResolvedServices.Count,
                _disposables?.Count ?? 0);

            // We've transitioned to the disposed state, so future calls to
            // CaptureDisposable will immediately dispose the object.
            // No further changes to _state.Disposables, are allowed.
            Disposed = true;
        }

        if (IsRootScope && !RootProvider.IsDisposed())
        {
            // If this ServiceProviderEngineScope instance is a root scope, disposing this instance will need to dispose the RootProvider too.
            // Otherwise the RootProvider will never get disposed and will leak.
            // Note, if the RootProvider get disposed first, it will automatically dispose all attached ServiceProviderEngineScope objects.
            RootProvider.Dispose();
        }

        // ResolvedServices is never cleared for singletons because there might be a compilation running in background
        // trying to get a cached singleton service. If it doesn't find it
        // it will try to create a new one which will result in an ObjectDisposedException.
        return _disposables;
    }

    internal string DebuggerToString()
    {
        var debugText = $"ServiceDescriptors = {RootProvider.CallSiteFactory.Descriptors.Length}";
        if (!IsRootScope)
        {
            debugText += ", IsScope = true";
        }

        if (Disposed)
        {
            debugText += ", Disposed = true";
        }

        return debugText;
    }

    private sealed class ServiceProviderEngineScopeDebugView
    {
        private readonly ServiceProviderEngineScope _serviceProvider;

        public ServiceProviderEngineScopeDebugView(ServiceProviderEngineScope serviceProvider) =>
            _serviceProvider = serviceProvider;

        public List<ServiceDescriptor> ServiceDescriptors =>
            new(_serviceProvider.RootProvider.CallSiteFactory.Descriptors);

        public List<object> Disposables => new(_serviceProvider.Disposables);
        public bool Disposed => _serviceProvider.Disposed;
        public bool IsScope => !_serviceProvider.IsRootScope;
    }
}