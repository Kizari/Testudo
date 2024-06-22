using System.Runtime.InteropServices;

namespace Testudo;

public partial class TestudoApplication
{
    /// <summary>
    /// The name of the native library name, less the extension.
    /// </summary>
    public const string LibraryName = "Testudo.Native";

    /// <summary>
    /// Creates and initializes the native application.
    /// </summary>
    /// <returns>A pointer to the newly created instance.</returns>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    private static extern IntPtr TestudoApplication_Construct();

    /// <summary>
    /// Ends the main program loop (which will cause <see cref="TestudoApplication_Run" /> to return)
    /// and destroys the native TestudoApplication instance.
    /// </summary>
    /// <param name="instance">The native TestudoApplication instance.</param>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    private static extern void TestudoApplication_Destroy(IntPtr instance);

    /// <summary>
    /// Runs the main application loop until the given instance is destroyed.
    /// </summary>
    /// <param name="instance">The native TestudoApplication instance.</param>
    /// <remarks>
    /// This method is blocking.
    /// </remarks>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    private static extern void TestudoApplication_Run(IntPtr instance);

    /// <summary>
    /// Invokes the given action on the main thread.
    /// </summary>
    /// <param name="instance">The native TestudoApplication instance.</param>
    /// <param name="action">The action to execute on the main thread.</param>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
    private static extern void TestudoApplication_Invoke(IntPtr instance, InvokeAction action);

    /// <summary>
    /// A delegate representing an <see cref="Action" /> to invoke on the main thread.
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void InvokeAction();
}