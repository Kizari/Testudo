// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Testudo;

internal sealed class ConstantCallSite : ServiceCallSite
{
    public ConstantCallSite(Type serviceType, object? defaultValue) : base(ResultCache.None(serviceType))
    {
        ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
        if (defaultValue != null && !serviceType.IsInstanceOfType(defaultValue))
        {
            throw new ArgumentException(string.Format(
                "Constant {0} cannot be converted to service type {1}.",
                defaultValue.GetType(),
                serviceType));
        }

        Value = defaultValue;
    }

    internal object? DefaultValue => Value;

    public override Type ServiceType { get; }

    public override Type ImplementationType => DefaultValue?.GetType() ?? ServiceType;
    public override CallSiteKind Kind { get; } = CallSiteKind.Constant;
}