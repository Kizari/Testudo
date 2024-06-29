using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Testudo;

public static class Extensions
{
    /// <summary>
    /// Adds all services required for Testudo to operate to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add Testudo to.</param>
    /// <param name="configuration">The configuration for the application.</param>
    /// <returns>The same service collection that was passed in.</returns>
    /// <remarks>
    /// <b>The service container must be built on the main thread for this to operate correctly.</b>
    /// </remarks>
    public static IServiceCollection AddTestudo(this IServiceCollection services, 
        TestudoApplicationConfiguration configuration) => services
        .AddSingleton(new TestudoApplicationConfigurationWrapper(configuration))
        .AddSingleton<ITestudoApplication, TestudoApplication>()
        .AddSingleton<Dispatcher, TestudoDispatcher>()
        .AddSingleton<IWindowManager, WindowManager>()
        .AddSingleton<JSComponentConfigurationStore>()
        .AddEmbeddedFileProvider(Assembly.GetCallingAssembly())
        .AddScoped<IScopeContext, ScopeContext>()
        .AddBlazorWebView();

    /// <summary>
    /// Sets the application up to use embedded web files.
    /// </summary>
    /// <param name="services">The service collection to add the embedded file provider to.</param>
    /// <param name="assembly">The assembly of the executable.</param>
    /// <returns></returns>
    private static IServiceCollection AddEmbeddedFileProvider(this IServiceCollection services, Assembly assembly) =>
        services.AddSingleton<IFileProvider>(_ =>
        {
            var baseNamespace = assembly.GetName().Name;
            return new EmbeddedFileProvider(assembly, $"{baseNamespace}.wwwroot");
        });
}