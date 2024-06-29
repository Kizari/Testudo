using System.Runtime.InteropServices;

namespace Testudo;

/// <summary>
/// The configuration for <see cref="TestudoApplication" />.
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public struct TestudoApplicationConfiguration : IDisposable
{
    /// <summary>
    /// The display-friendly name of this application.
    /// </summary>
    public IntPtr _applicationName;

    /// <summary>
    /// Pointer to the application's icon.
    /// </summary>
    public IntPtr Icon;

    /// <inheritdoc cref="_applicationName"/>
    public required string ApplicationName
    {
        set => _applicationName = Marshal.StringToHGlobalAuto(value);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Marshal.FreeHGlobal(_applicationName);
    }
}

/// <summary>
/// This exists simply because a reference type is required to register the configuration with the service container.
/// </summary>
public class TestudoApplicationConfigurationWrapper(TestudoApplicationConfiguration configuration)
{
    public TestudoApplicationConfiguration Configuration { get; } = configuration;
}