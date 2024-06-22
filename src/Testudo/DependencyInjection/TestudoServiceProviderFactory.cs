// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Testudo;

/// <summary>
/// Default implementation of <see cref="IServiceProviderFactory{TContainerBuilder}" />.
/// </summary>
public class TestudoServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
{
    private readonly ServiceProviderOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultServiceProviderFactory" /> class
    /// with default options.
    /// </summary>
    public TestudoServiceProviderFactory() : this(new ServiceProviderOptions()) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultServiceProviderFactory" /> class
    /// with the specified <paramref name="options" />.
    /// </summary>
    /// <param name="options">The options to use for this instance.</param>
    public TestudoServiceProviderFactory(ServiceProviderOptions options) =>
        _options = options ?? throw new ArgumentNullException(nameof(options));

    /// <inheritdoc />
    public IServiceCollection CreateBuilder(IServiceCollection services) => services;

    /// <inheritdoc />
    public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder)
    {
        if (containerBuilder is null)
        {
            throw new ArgumentNullException(nameof(containerBuilder));
        }

        if (_options is null)
        {
            throw new ArgumentNullException(nameof(_options));
        }

        var provider = new TestudoServiceProvider(containerBuilder, _options);
        provider.GetRequiredService<IScopeContext>().ServiceProvider = provider;
        return provider;
    }
}