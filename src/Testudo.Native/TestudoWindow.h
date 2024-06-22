#pragma once

#include <webkit2/webkit2.h>

#include "Testudo.h"
#include "TestudoWindowConfiguration.h"

/**
 * \brief Manages a native window containing a web view.
 */
class TestudoWindow
{
    private:
        GtkWidget* window; /**< Reference to the GTK window. */
        GtkWidget* webView; /**< Reference to the GTK web view. */
        WebKitUserContentManager* contentManager; /**< Reference to the web view's content manager. */

    public:
        /**
         * \brief Creates a new window containing a web view and immediately shows it.
         * \param configuration The window's configuration.
         */
        TestudoWindow(TestudoWindowConfiguration* configuration);

        /**
         * \brief Destroys the window.
         */
        ~TestudoWindow();

        /**
         * \brief Navigates this window's web view to the given URI.
         * \param uri The URI to navigate to.
         */
        void navigate(String uri);

        /**
         * \brief Sends a JavaScript message to this window's web view for evaluation.
         * \param message The JavaScript to send.
         */
        void sendMessage(String message);
};