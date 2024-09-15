using System.Drawing;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testudo;
using Testudo.Sample;

internal static class Program
{
    [STAThread]
    internal static void Main(string[] args)
    {
        using var stream = Assembly.GetEntryAssembly()!.GetManifestResourceStream("Testudo.Sample.icon.ico")!;
        using var icon = new Icon(stream);

        var host = Host.CreateDefaultBuilder(args)
            .UseServiceProviderFactory(new TestudoServiceProviderFactory())
            .ConfigureServices((_, services) => services
                .AddTestudo(new TestudoApplicationConfiguration
                {
                    ApplicationName = "TestudoSample",
                    Icon = icon.Handle
                }))
            .Build();
    
        var application = host.Services.GetRequiredService<ITestudoApplication>();
        var windowManager = host.Services.GetRequiredService<IWindowManager>();

        windowManager.OpenWindowAsync<App>(new TestudoWindowConfiguration
        {
            Icon = icon.Handle,
            Title = "TestudoSample",
            InitialRelativePath = "/",
            IsCentered = true,
            Width = 1920,
            Height = 1080,
            HasWindowShell = true
        }).ConfigureAwait(false).GetAwaiter().GetResult();

        application.Run();
    }
}