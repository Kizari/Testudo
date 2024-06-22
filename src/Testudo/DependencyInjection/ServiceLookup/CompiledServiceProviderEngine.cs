// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Testudo;

internal abstract class CompiledServiceProviderEngine : ServiceProviderEngine
{
#if IL_EMIT
        public ILEmitResolverBuilder ResolverBuilder { get; }
#else
    public ExpressionResolverBuilder ResolverBuilder { get; }
#endif

    [RequiresDynamicCode("Creates DynamicMethods")]
    public CompiledServiceProviderEngine(TestudoServiceProvider provider) =>
        ResolverBuilder = new ExpressionResolverBuilder(provider);

    public override Func<ServiceProviderEngineScope, object?> RealizeService(ServiceCallSite callSite) =>
        ResolverBuilder.Build(callSite);
}