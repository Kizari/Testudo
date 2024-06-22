// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace Testudo;

/// <summary>
/// The default IServiceProvider implementation.<br />
/// <b>Cloned from the .NET 8 runtime.</b>
/// </summary>
/// <remarks>
/// Slight modifications have been made to assign new scopes into a scoped <see cref="ScopeContext" /> service
/// to ensure that the scope can be injected into other scoped services that need to get a scoped service
/// provider to pass to Blazor's <c>WebViewManager</c>.
/// </remarks>
[DebuggerDisplay("{DebuggerToString(),nq}")]
[DebuggerTypeProxy(typeof(ServiceProviderDebugView))]
public sealed class TestudoServiceProvider : IServiceProvider, IKeyedServiceProvider, IDisposable, IAsyncDisposable
{
    private readonly CallSiteValidator? _callSiteValidator;

    private readonly Func<ServiceIdentifier, ServiceAccessor> _createServiceAccessor;

    private readonly ConcurrentDictionary<ServiceIdentifier, ServiceAccessor> _serviceAccessors;

    private bool _disposed;

    // Internal for testing
    internal ServiceProviderEngine _engine;

    internal TestudoServiceProvider(ICollection<ServiceDescriptor> serviceDescriptors, ServiceProviderOptions options)
    {
        // note that Root needs to be set before calling GetEngine(), because the engine may need to access Root
        Root = new ServiceProviderEngineScope(this, true);
        _engine = GetEngine();
        _createServiceAccessor = CreateServiceAccessor;
        _serviceAccessors = new ConcurrentDictionary<ServiceIdentifier, ServiceAccessor>();

        CallSiteFactory = new CallSiteFactory(serviceDescriptors);
        // The list of built in services that aren't part of the list of service descriptors
        // keep this in sync with CallSiteFactory.IsService
        CallSiteFactory.Add(ServiceIdentifier.FromServiceType(typeof(IServiceProvider)), new ServiceProviderCallSite());
        CallSiteFactory.Add(ServiceIdentifier.FromServiceType(typeof(IServiceScopeFactory)),
            new ConstantCallSite(typeof(IServiceScopeFactory), Root));
        CallSiteFactory.Add(ServiceIdentifier.FromServiceType(typeof(IServiceProviderIsService)),
            new ConstantCallSite(typeof(IServiceProviderIsService), CallSiteFactory));
        CallSiteFactory.Add(ServiceIdentifier.FromServiceType(typeof(IServiceProviderIsKeyedService)),
            new ConstantCallSite(typeof(IServiceProviderIsKeyedService), CallSiteFactory));

        if (options.ValidateScopes)
        {
            _callSiteValidator = new CallSiteValidator();
        }

        if (options.ValidateOnBuild)
        {
            List<Exception>? exceptions = null;
            foreach (var serviceDescriptor in serviceDescriptors)
            {
                try
                {
                    ValidateService(serviceDescriptor);
                }
                catch (Exception e)
                {
                    exceptions ??= new List<Exception>();
                    exceptions.Add(e);
                }
            }

            if (exceptions != null)
            {
                throw new AggregateException("Some services are not able to be constructed", exceptions.ToArray());
            }
        }

        DependencyInjectionEventSource.Log.ServiceProviderBuilt(this);
    }

    internal CallSiteFactory CallSiteFactory { get; }

    internal ServiceProviderEngineScope Root { get; }

    internal static bool VerifyOpenGenericServiceTrimmability { get; } =
        AppContext.TryGetSwitch("Microsoft.Extensions.DependencyInjection.VerifyOpenGenericServiceTrimmability",
            out var verifyOpenGenerics)
            ? verifyOpenGenerics
            : false;

    internal static bool DisableDynamicEngine { get; } =
        AppContext.TryGetSwitch("Microsoft.Extensions.DependencyInjection.DisableDynamicEngine",
            out var disableDynamicEngine)
            ? disableDynamicEngine
            : false;

    internal static bool VerifyAotCompatibility =>
#if NETFRAMEWORK || NETSTANDARD2_0
            false;
#else
        !RuntimeFeature.IsDynamicCodeSupported;
#endif

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        DisposeCore();
        return Root.DisposeAsync();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DisposeCore();
        Root.Dispose();
    }

    /// <summary>
    /// Gets the service object of the specified type with the specified key.
    /// </summary>
    /// <param name="serviceType">The type of the service to get.</param>
    /// <param name="serviceKey">The key of the service to get.</param>
    /// <returns>The keyed service.</returns>
    public object? GetKeyedService(Type serviceType, object? serviceKey)
        => GetKeyedService(serviceType, serviceKey, Root);

    /// <summary>
    /// Gets the service object of the specified type. Will throw if the service not found.
    /// </summary>
    /// <param name="serviceType">The type of the service to get.</param>
    /// <param name="serviceKey">The key of the service to get.</param>
    /// <returns>The keyed service.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
        => GetRequiredKeyedService(serviceType, serviceKey, Root);

    /// <summary>
    /// Gets the service object of the specified type.
    /// </summary>
    /// <param name="serviceType">The type of the service to get.</param>
    /// <returns>The service that was produced.</returns>
    public object? GetService(Type serviceType) => GetService(ServiceIdentifier.FromServiceType(serviceType), Root);

    internal object? GetKeyedService(Type serviceType, object? serviceKey,
        ServiceProviderEngineScope serviceProviderEngineScope)
        => GetService(new ServiceIdentifier(serviceKey, serviceType), serviceProviderEngineScope);

    internal object GetRequiredKeyedService(Type serviceType, object? serviceKey,
        ServiceProviderEngineScope serviceProviderEngineScope)
    {
        var service = GetKeyedService(serviceType, serviceKey, serviceProviderEngineScope);
        if (service == null)
        {
            throw new InvalidOperationException($"No service of type {serviceType} was registered.");
        }

        return service;
    }

    internal bool IsDisposed() => _disposed;

    private void DisposeCore()
    {
        _disposed = true;
        DependencyInjectionEventSource.Log.ServiceProviderDisposed(this);
    }

    private void OnCreate(ServiceCallSite callSite)
    {
        _callSiteValidator?.ValidateCallSite(callSite);
    }

    private void OnResolve(ServiceCallSite? callSite, IServiceScope scope)
    {
        if (callSite != null)
        {
            _callSiteValidator?.ValidateResolution(callSite, scope, Root);
        }
    }

    internal object? GetService(ServiceIdentifier serviceIdentifier,
        ServiceProviderEngineScope serviceProviderEngineScope)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }

        var serviceAccessor = _serviceAccessors.GetOrAdd(serviceIdentifier, _createServiceAccessor);
        OnResolve(serviceAccessor.CallSite, serviceProviderEngineScope);
        DependencyInjectionEventSource.Log.ServiceResolved(this, serviceIdentifier.ServiceType);
        var result = serviceAccessor.RealizedService?.Invoke(serviceProviderEngineScope);
        Debug.Assert(result is null || CallSiteFactory.IsService(serviceIdentifier));
        return result;
    }

    private void ValidateService(ServiceDescriptor descriptor)
    {
        if (descriptor.ServiceType.IsGenericType && !descriptor.ServiceType.IsConstructedGenericType)
        {
            return;
        }

        try
        {
            var callSite = CallSiteFactory.GetCallSite(descriptor, new CallSiteChain());
            if (callSite != null)
            {
                OnCreate(callSite);
            }
        }
        catch (Exception e)
        {
            throw new InvalidOperationException(
                $"Error while validating the service descriptor '{descriptor}': {e.Message}", e);
        }
    }

    private ServiceAccessor CreateServiceAccessor(ServiceIdentifier serviceIdentifier)
    {
        var callSite = CallSiteFactory.GetCallSite(serviceIdentifier, new CallSiteChain());
        if (callSite != null)
        {
            DependencyInjectionEventSource.Log.CallSiteBuilt(this, serviceIdentifier.ServiceType, callSite);
            OnCreate(callSite);

            // Optimize singleton case
            if (callSite.Cache.Location == CallSiteResultCacheLocation.Root)
            {
                var value = CallSiteRuntimeResolver.Instance.Resolve(callSite, Root);
                return new ServiceAccessor {CallSite = callSite, RealizedService = scope => value};
            }

            var realizedService = _engine.RealizeService(callSite);
            return new ServiceAccessor {CallSite = callSite, RealizedService = realizedService};
        }

        return new ServiceAccessor {CallSite = callSite, RealizedService = _ => null};
    }

    internal void ReplaceServiceAccessor(ServiceCallSite callSite, Func<ServiceProviderEngineScope, object?> accessor)
    {
        _serviceAccessors[new ServiceIdentifier(callSite.Key, callSite.ServiceType)] = new ServiceAccessor
        {
            CallSite = callSite,
            RealizedService = accessor
        };
    }

    /// <summary>
    /// Creates a new service scope.
    /// </summary>
    /// <returns>The newly created scope.</returns>
    /// <remarks>
    /// This method has been modified slightly to assign the scope to the <see cref="ScopeContext.ServiceProvider" />
    /// property of the scoped <see cref="ScopeContext" /> service so that a reference to the scope can be retrieved
    /// from within the scope when it needs to be passed to Blazor's <c>WebViewManager</c>.
    /// </remarks>
    internal IServiceScope CreateScope()
    {
        if (_disposed)
        {
            ThrowHelper.ThrowObjectDisposedException();
        }

        var scope = new ServiceProviderEngineScope(this, false);
        scope.GetRequiredService<IScopeContext>().ServiceProvider = scope.ServiceProvider;
        return scope;
    }

    private ServiceProviderEngine GetEngine()
    {
        ServiceProviderEngine engine;

#if NETFRAMEWORK || NETSTANDARD2_0
            engine = CreateDynamicEngine();
#else
        if (RuntimeFeature.IsDynamicCodeCompiled && !DisableDynamicEngine)
        {
            engine = CreateDynamicEngine();
        }
        else
        {
            // Don't try to compile Expressions/IL if they are going to get interpreted
            engine = RuntimeServiceProviderEngine.Instance;
        }
#endif
        return engine;

        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification =
                "CreateDynamicEngine won't be called when using NativeAOT.")] // see also https://github.com/dotnet/linker/issues/2715
        ServiceProviderEngine CreateDynamicEngine() => new DynamicServiceProviderEngine(this);
    }

    private string DebuggerToString() => Root.DebuggerToString();

    internal sealed class ServiceProviderDebugView
    {
        private readonly TestudoServiceProvider _serviceProvider;

        public ServiceProviderDebugView(TestudoServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public List<ServiceDescriptor> ServiceDescriptors =>
            new(_serviceProvider.Root.RootProvider.CallSiteFactory.Descriptors);

        public List<object> Disposables => new(_serviceProvider.Root.Disposables);
        public bool Disposed => _serviceProvider.Root.Disposed;
        public bool IsScope => !_serviceProvider.Root.IsRootScope;
    }

    private sealed class ServiceAccessor
    {
        public ServiceCallSite? CallSite { get; set; }
        public Func<ServiceProviderEngineScope, object?>? RealizedService { get; set; }
    }
}