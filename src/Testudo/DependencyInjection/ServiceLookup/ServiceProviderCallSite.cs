// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Testudo;

internal sealed class ServiceProviderCallSite : ServiceCallSite
{
    public ServiceProviderCallSite() : base(ResultCache.None(typeof(IServiceProvider))) { }

    public override Type ServiceType { get; } = typeof(IServiceProvider);
    public override Type ImplementationType { get; } = typeof(TestudoServiceProvider);
    public override CallSiteKind Kind { get; } = CallSiteKind.ServiceProvider;
}