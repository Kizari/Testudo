// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Testudo;

internal sealed class CallSiteValidator : CallSiteVisitor<CallSiteValidator.CallSiteValidatorState, Type?>
{
    // Keys are services being resolved via GetService, values - first scoped service in their call site tree
    private readonly ConcurrentDictionary<ServiceCacheKey, Type?> _scopedServices = new();

    public void ValidateCallSite(ServiceCallSite callSite) => VisitCallSite(callSite, default);

    public void ValidateResolution(ServiceCallSite callSite, IServiceScope scope, IServiceScope rootScope)
    {
        if (ReferenceEquals(scope, rootScope)
            && _scopedServices.TryGetValue(callSite.Cache.Key, out var scopedService)
            && scopedService != null)
        {
            var serviceType = callSite.ServiceType;
            if (serviceType == scopedService)
            {
                throw new InvalidOperationException(
                    string.Format("Cannot resolve {1} service of type {0} from root (unscoped).",
                        callSite.ServiceType,
                        nameof(ServiceLifetime.Scoped).ToLowerInvariant()));
            }

            throw new InvalidOperationException(
                string.Format("Cannot resolve {2} service of type {1} (service type {0}) from root (unscoped).",
                    callSite.ServiceType,
                    scopedService,
                    nameof(ServiceLifetime.Scoped).ToLowerInvariant()));
        }
    }

    protected override Type? VisitCallSite(ServiceCallSite callSite, CallSiteValidatorState argument)
    {
        // First, check if we have encountered this call site before to prevent visiting call site trees that have already been visited
        // If firstScopedServiceInCallSiteTree is null there are no scoped dependencies in this service's call site tree
        // If firstScopedServiceInCallSiteTree has a value, it contains the first scoped service in this service's call site tree
        if (!_scopedServices.TryGetValue(callSite.Cache.Key, out var firstScopedServiceInCallSiteTree))
        {
            // This call site wasn't cached yet, walk the tree
            firstScopedServiceInCallSiteTree = base.VisitCallSite(callSite, argument);

            // Cache the result
            _scopedServices[callSite.Cache.Key] = firstScopedServiceInCallSiteTree;
        }

        // If there is a scoped service in the call site tree, make sure we are not resolving it from a singleton
        if (firstScopedServiceInCallSiteTree != null && argument.Singleton != null)
        {
            throw new InvalidOperationException(string.Format(
                "Cannot resolve {2} service {0} from {3} service {1}.",
                callSite.ServiceType,
                argument.Singleton.ServiceType,
                nameof(ServiceLifetime.Scoped).ToLowerInvariant(),
                nameof(ServiceLifetime.Singleton).ToLowerInvariant()
            ));
        }

        return firstScopedServiceInCallSiteTree;
    }

    protected override Type? VisitConstructor(ConstructorCallSite constructorCallSite, CallSiteValidatorState state)
    {
        Type? result = null;
        foreach (var parameterCallSite in constructorCallSite.ParameterCallSites)
        {
            var scoped = VisitCallSite(parameterCallSite, state);
            result ??= scoped;
        }

        return result;
    }

    protected override Type? VisitIEnumerable(IEnumerableCallSite enumerableCallSite,
        CallSiteValidatorState state)
    {
        Type? result = null;
        foreach (var serviceCallSite in enumerableCallSite.ServiceCallSites)
        {
            var scoped = VisitCallSite(serviceCallSite, state);
            result ??= scoped;
        }

        return result;
    }

    protected override Type? VisitRootCache(ServiceCallSite singletonCallSite, CallSiteValidatorState state)
    {
        state.Singleton = singletonCallSite;
        return VisitCallSiteMain(singletonCallSite, state);
    }

    protected override Type? VisitScopeCache(ServiceCallSite scopedCallSite, CallSiteValidatorState state)
    {
        // We are fine with having ServiceScopeService requested by singletons
        if (scopedCallSite.ServiceType == typeof(IServiceScopeFactory))
        {
            return null;
        }

        VisitCallSiteMain(scopedCallSite, state);
        return scopedCallSite.ServiceType;
    }

    protected override Type? VisitConstant(ConstantCallSite constantCallSite, CallSiteValidatorState state) => null;

    protected override Type? VisitServiceProvider(ServiceProviderCallSite serviceProviderCallSite,
        CallSiteValidatorState state) => null;

    protected override Type? VisitFactory(FactoryCallSite factoryCallSite, CallSiteValidatorState state) => null;

    internal struct CallSiteValidatorState
    {
        [DisallowNull] public ServiceCallSite? Singleton { get; set; }
    }
}