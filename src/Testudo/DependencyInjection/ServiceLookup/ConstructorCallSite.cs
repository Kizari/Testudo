// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Testudo;

internal sealed class ConstructorCallSite : ServiceCallSite
{
    public ConstructorCallSite(ResultCache cache, Type serviceType, ConstructorInfo constructorInfo) : this(cache,
        serviceType, constructorInfo, Array.Empty<ServiceCallSite>()) { }

    public ConstructorCallSite(ResultCache cache, Type serviceType, ConstructorInfo constructorInfo,
        ServiceCallSite[] parameterCallSites) : base(cache)
    {
        if (!serviceType.IsAssignableFrom(constructorInfo.DeclaringType))
        {
            throw new ArgumentException($"Implementation type {constructorInfo.DeclaringType} can't be " +
                                        $"converted to service type {serviceType}");
        }

        ServiceType = serviceType;
        ConstructorInfo = constructorInfo;
        ParameterCallSites = parameterCallSites;
    }

    internal ConstructorInfo ConstructorInfo { get; }
    internal ServiceCallSite[] ParameterCallSites { get; }

    public override Type ServiceType { get; }

    public override Type? ImplementationType => ConstructorInfo.DeclaringType;
    public override CallSiteKind Kind { get; } = CallSiteKind.Constructor;
}