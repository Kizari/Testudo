#ifdef __linux__

#include "../TestudoWindow.h"

#include <iomanip>
#include <sstream>
#include <string>
#include <webkit/webkit2.h>

/**
 * @brief Holds information pertaining to a JavaScript invocation.
 */
struct JavaScriptInvocation
{
    /** Specifies whether or not the invocation has completed. */
    bool is_completed;
};

/** Reference to the GTK window. */
GtkWidget* _window;

/** Reference to the GTK web view. */
GtkWidget* _web_view;

/** Reference to the web view's content manager. */
WebKitUserContentManager* _content_manager;

/**
 * @brief Passes a JavaScript result back to managed code for processing.
 */
static void TestudoWindow::script_message_received_callback(
    [[maybe_unused]] WebKitUserContentManager* content_manager,
    WebKitJavascriptResult* js_result,
    // ReSharper disable once CppParameterMayBeConst
    gpointer data)
{
    JSCValue* js_value = webkit_javascript_result_get_js_value(js_result);

    if (jsc_value_is_string(js_value))
    {
        char* value = jsc_value_to_string(js_value);
        reinterpret_cast<WebMessageReceivedDelegate>(data)(value);
        g_free(value);
    }

    webkit_javascript_result_unref(js_result);
}

/**
 * @brief Pulls a resource's data buffer from managed code and passes it to the web view.
 */
// ReSharper disable once CppParameterMayBeConst
static void TestudoWindow::web_context_register_uri_scheme_callback(WebKitURISchemeRequest* request, gpointer data)
{
    // ReSharper disable once CppCStyleCast
    const auto delegate = (WebResourceRequestedDelegate)data;
    const auto uri = webkit_uri_scheme_request_get_uri(request);

    int size_bytes;
    String content_type;
    const auto result =
        delegate(const_cast<String>(uri), &size_bytes, &content_type);

    GInputStream* stream = g_memory_input_stream_new_from_data(
        result, size_bytes, nullptr);
    webkit_uri_scheme_request_finish(request, stream, -1, content_type);

    g_object_unref(stream);
    delete[] content_type;
}

TestudoWindow::TestudoWindow(const TestudoWindowConfiguration* configuration)
{
    // Create the window
    _window = gtk_window_new(GTK_WINDOW_TOPLEVEL);

    // Apply window configuration
    gtk_window_set_default_size(GTK_WINDOW(_window), configuration->width, configuration->height);

    if (configuration->is_centered)
    {
        gtk_window_set_position(GTK_WINDOW(_window), GTK_WIN_POS_CENTER);
    }
    else
    {
        gtk_window_move(GTK_WINDOW(_window), configuration->left, configuration->top);
    }

    // Create the web view and add it to the window
    _content_manager = webkit_user_content_manager_new();
    _web_view = webkit_web_view_new_with_user_content_manager(_content_manager);
    gtk_container_add(GTK_CONTAINER(_window), _web_view);

    // Setup interop script
    const auto script = webkit_user_script_new(
        "window.__receiveMessageCallbacks = [];"
        "window.__dispatchMessageCallback = function(message) {"
        "	window.__receiveMessageCallbacks.forEach(function(callback) { callback(message); });"
        "};"
        "window.external = {"
        "	sendMessage: function(message) {"
        "		window.webkit.messageHandlers.visium.postMessage(message);"
        "	},"
        "	receiveMessage: function(callback) {"
        "		window.__receiveMessageCallbacks.push(callback);"
        "	}"
        "};",
        WEBKIT_USER_CONTENT_INJECT_ALL_FRAMES,
        WEBKIT_USER_SCRIPT_INJECT_AT_DOCUMENT_START, nullptr, nullptr);

    webkit_user_content_manager_add_script(_content_manager, script);
    webkit_user_script_unref(script);

    g_signal_connect(_content_manager, "script-message-received::visium",
                     G_CALLBACK(script_message_received_callback),
                     configuration->web_message_received_handler);

    webkit_user_content_manager_register_script_message_handler(
        _content_manager, "visium");

    // Setup custom scheme handler
    const auto context = webkit_web_context_get_default();
    webkit_web_context_register_uri_scheme(context,
                                           "app",
                                           web_context_register_uri_scheme_callback,
                                           configuration->web_resource_requested_handler,
                                           nullptr);

    // Navigate to the initial URI
    if (configuration->initial_uri != nullptr)
    {
        navigate(configuration->initial_uri);
    }

    // Show the window
    gtk_widget_show_all(_window);
}

TestudoWindow::~TestudoWindow()
{
    gtk_widget_destroy(_window);
}

void TestudoWindow::navigate(const String uri) const
{
    webkit_web_view_load_uri(WEBKIT_WEB_VIEW(_web_view), uri);
}

/**
 * @brief Escapes characters in a JSON string for use with GTK web view.
 * @param string: The JSON string to format.
 * @returns The formatted JSON string.
 */
static std::string TestudoWindow::escape_json(const std::string& string)
{
    std::ostringstream string_stream;

    for (const char character : string)
    {
        switch (character)
        {
        case '"':
            string_stream << "\\\"";
            break;
        case '\\':
            string_stream << "\\\\";
            break;
        case '\b':
            string_stream << "\\b";
            break;
        case '\f':
            string_stream << "\\f";
            break;
        case '\n':
            string_stream << "\\n";
            break;
        case '\r':
            string_stream << "\\r";
            break;
        case '\t':
            string_stream << "\\t";
            break;
        default:
            if ('\x00' <= character && character <= '\x1f')
            {
                string_stream << "\\u"
                    << std::hex << std::setw(4) << std::setfill('0') << static_cast<int>
                    (character);
            }
            else
            {
                string_stream << character;
            }
        }
    }

    return string_stream.str();
}

/**
  * @brief Callback function for @ref webkit_web_view_evaluate_javascript.
  * @param source_object The web view instance that initiated the evaluation.
  * @param result The result of the JavaScript evaluation.
  * @param data @ref JSInvocation instance passed through from the consumer.
  * Should be an instance of @ref JSInvocation.
  */
static void TestudoWindow::web_view_evaluate_java_script_callback(
    [[maybe_unused]] GObject* source_object,
    [[maybe_unused]] GAsyncResult* result,
    // ReSharper disable once CppParameterMayBeConst
    gpointer data)
{
    const auto invocation = static_cast<JavaScriptInvocation*>(data);
    invocation->is_completed = true;
}

void TestudoWindow::send_message(const String message) const
{
    // Format the message appropriately for Linux
    std::string javascript;
    javascript.append("__dispatchMessageCallback(\"");
    javascript.append(escape_json(message));
    javascript.append("\")");
    const auto script = javascript.c_str();

    // Invoke the JavaScript evaluation
    JavaScriptInvocation invocation = {};
    webkit_web_view_evaluate_javascript(
        WEBKIT_WEB_VIEW(_web_view),
        script,
        static_cast<gssize>(strlen(script)),
        nullptr,
        nullptr,
        nullptr,
        web_view_evaluate_java_script_callback,
        &invocation);

    // Block until the invocation is complete
    while (!invocation.is_completed)
    {
        g_main_context_iteration(nullptr, true);
    }
}

#endif
