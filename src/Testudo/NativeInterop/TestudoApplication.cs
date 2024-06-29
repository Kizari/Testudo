using System.Runtime.InteropServices;

namespace Testudo;

/// <inheritdoc />
public partial class TestudoApplication : ITestudoApplication
{
    /// <summary>
    /// A pointer to the native TestudoApplication instance that this class wraps.
    /// </summary>
    private readonly IntPtr _instance;

    /// <summary>
    /// Pinned configuration struct that needs to remain stable for the life of the native application.
    /// </summary>
    private GCHandle _configurationHandle;

    /// <summary>
    /// Creates a new native application.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    public TestudoApplication(TestudoApplicationConfigurationWrapper configuration)
    {
        _configurationHandle = GCHandle.Alloc(configuration.Configuration, GCHandleType.Pinned);
        _instance = TestudoApplication_Construct(_configurationHandle.AddrOfPinnedObject());
    }

    /// <inheritdoc />
    public int MainThreadId { get; } = Environment.CurrentManagedThreadId;

    /// <inheritdoc />
    public void Dispose()
    {
        TestudoApplication_Destroy(_instance);

        if (_configurationHandle.IsAllocated)
        {
            _configurationHandle.Free();
        }

        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public void Run()
    {
        TestudoApplication_Run();
    }

    /// <inheritdoc />
    public void Invoke(Action action)
    {
        if (Environment.CurrentManagedThreadId == MainThreadId)
        {
            // Already on the UI thread, no point dispatching
            action();
        }
        else
        {
            TestudoApplication_Invoke(action.Invoke);
        }
    }

    /// <inheritdoc />
    public string? OpenFolderDialog() => TestudoApplication_OpenFolderDialog();
}