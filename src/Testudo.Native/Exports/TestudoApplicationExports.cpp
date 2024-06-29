// ReSharper disable CppInconsistentNaming (named this way for C# imports)

#include "Testudo.h"

#if _WIN32
#include "TestudoApplication.h"
#else
#include "TestudoApplication.h"
#endif

extern "C"
{
    /**
     * @brief Instantiates a new @ref TestudoApplication.
     * @returns A pointer to the newly created instance.
     */
    EXPORTED TestudoApplication* TestudoApplication_Construct(const TestudoApplicationConfiguration* configuration)
    {
        return new TestudoApplication(configuration);
    }

    /**
     * @brief Ends the main program loop and destroys the @ref TestudoApplication instance.
     * @param instance A pointer to the @ref TestudoApplication instance that is to be destroyed.
     */
    EXPORTED void TestudoApplication_Destroy(const TestudoApplication* instance)
    {
        delete instance;
    }

    /**
     * @brief Runs the main application loop until the given instance is destroyed.
     * @remarks This function is blocking.
     */
    EXPORTED void TestudoApplication_Run()
    {
        TestudoApplication::run();
    }

    /**
     * @brief Invokes the given action on the main thread.
     * @param action The action to execute on the main thread.
     */
    EXPORTED void TestudoApplication_Invoke(const Action action)
    {
        TestudoApplication::invoke(action);
    }

    /**
     * @brief Opens a native folder selection dialog.
     * @return The path to the selected folder, or null if no folder was selected.
     */
    EXPORTED String TestudoApplication_OpenFolderDialog()
    {
        return TestudoApplication::openFolderDialog();
    }
}
