// ReSharper disable CppInconsistentNaming (named this way for C# imports)

#include "Testudo.h"
#include "TestudoWindowConfiguration.h"

#if _WIN32
#include "../Windows/TestudoWindow.h"
#else
#include "../Linux/TestudoWindow.h"
#endif

extern "C"
{
    /**
     * @brief Creates a new native window.
     * @param configuration The window's configuration.
     * @returns A pointer to the newly created instance.
     */
    EXPORTED TestudoWindow* TestudoWindow_Construct(const TestudoWindowConfiguration* configuration)
    {
        return new TestudoWindow(configuration);
    }

    /**
     * @brief Destroys an existing web view window.
     * @param instance A pointer to the window instance that should be destroyed.
     */
    EXPORTED void TestudoWindow_Destroy(const TestudoWindow* instance)
    {
        delete instance;
    }

    /**
     * @brief Initializes the window's embedded web view then shows the window.
     * @param instance A pointer to the window instance that is to be shown.
     */
    EXPORTED void TestudoWindow_Show(TestudoWindow* instance)
    {
        instance->show();
    }

    /**
     * @brief Navigates the given window's web view to the given URI.
     * @param instance A pointer to the window whose web view should be navigated.
     * @param uri The URI to navigate to.
     */
    EXPORTED void TestudoWindow_Navigate(const TestudoWindow* instance, const String uri)
    {
        instance->navigate(uri);
    }

    /**
     * @brief Sends a JavaScript message to the given window's web view for evaluation.
     * @param instance A pointer to the window containing the web view.
     * @param message The JavaScript message to send and evaluate.
     */
    EXPORTED void TestudoWindow_SendMessage(const TestudoWindow* instance, const String message)
    {
        instance->sendMessage(message);
    }
}
