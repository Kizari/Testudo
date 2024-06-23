namespace Testudo;

/// <inheritdoc />
public partial class TestudoApplication : ITestudoApplication
{
    private readonly IntPtr _instance = TestudoApplication_Construct();

    /// <inheritdoc />
    public int MainThreadId { get; } = Environment.CurrentManagedThreadId;

    public void Dispose()
    {
        TestudoApplication_Destroy(_instance);
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
    public string? OpenFolderDialog()
    {
        return TestudoApplication_OpenFolderDialog();
    }
}