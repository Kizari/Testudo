#pragma once

#include <string>
#include <webkit2/webkit2.h>

#include "Testudo.h"
#include "TestudoWindowConfiguration.h"

/**
 * @brief Manages a native window containing a web view.
 */
class TestudoWindow
{
private:
    /** Reference to the GTK window. */
    GtkWidget* _window;

    /** Reference to the GTK web view. */
    GtkWidget* _web_view;

    /** Reference to the web view's content manager. */
    WebKitUserContentManager* _content_manager;

    /**
     * @brief Passes a JavaScript result back to managed code for processing.
     */
    static void script_message_received_callback(
        WebKitUserContentManager* content_manager,
        WebKitJavascriptResult* js_result,
        gpointer data);

    /**
     * @brief Pulls a resource's data buffer from managed code and passes it to the web view.
     */
    static void web_context_register_uri_scheme_callback(WebKitURISchemeRequest* request, gpointer data);

    /**
     * @brief Escapes characters in a JSON string for use with GTK web view.
     * @param string: The JSON string to format.
     * @returns The formatted JSON string.
     */
    static std::string escape_json(const std::string& string);

    /**
      * @brief Callback function for @ref webkit_web_view_evaluate_javascript.
      * @param source_object The web view instance that initiated the evaluation.
      * @param result The result of the JavaScript evaluation.
      * @param data @ref JSInvocation instance passed through from the consumer.
      * Should be an instance of @ref JSInvocation.
      */
    static void web_view_evaluate_java_script_callback(
        GObject* source_object,
        GAsyncResult* result,
        gpointer data);

public:
    /**
     * @brief Creates a new window containing a web view and immediately shows it.
     * @param configuration The window's configuration.
     */
    explicit TestudoWindow(const TestudoWindowConfiguration* configuration);

    /**
     * @brief Destroys the window.
     */
    ~TestudoWindow();

    /**
     * @brief Navigates this window's web view to the given URI.
     * @param uri The URI to navigate to.
     */
    void navigate(String uri) const;

    /**
     * @brief Sends a JavaScript message to this window's web view for evaluation.
     * @param message The JavaScript to send.
     */
    void send_message(String message) const;
};
