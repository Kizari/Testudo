#include "Testudo.h"
#include "TestudoWindow.h"

extern "C"
{
    /**
     * \brief Creates a new window containing a web view and immediately shows it.
     * \param configuration The window's configuration.
     * \returns A pointer to the newly created instance.
     */
    EXPORTED TestudoWindow* TestudoWindow_Construct(TestudoWindowConfiguration* configuration)
    {
        return new TestudoWindow(configuration);
    }

    /**
     * \brief Destroys an existing web view window.
     * \param instance: A pointer to the window instance that should be destroyed.
     */
    EXPORTED void TestudoWindow_Destroy(TestudoWindow* instance)
    {
        delete instance;
    }

    /**
     * \brief Navigates the given window's web view to the given URI.
     * \param instance A pointer to the window whose web view should be navigated.
     * \param uri The URI to navigate to.
     */
    EXPORTED void TestudoWindow_Navigate(TestudoWindow* instance, String uri)
    {
        instance->navigate(uri);
    }

    /**
     * \brief Sends a JavaScript message to the given window's web view for evaluation.
     * \param instance A pointer to the window containing the web view.
     * \param message The JavaScript message to send and evaluate.
     */
    EXPORTED void TestudoWindow_SendMessage(TestudoWindow* instance, String message)
    {
        instance->sendMessage(message);
    }
}