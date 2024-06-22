#ifdef __linux__

#include "../TestudoWindow.h"

#include <string>
#include <sstream>
#include <iomanip>

/**
 * \brief Holds information pertaining to a JavaScript invocation.
 */
struct JSInvocation
{
    bool isCompleted; /**< Specifies whether or not the invocation has completed. */
};

/**
 * \brief Passes a JavaScript result back to managed code for processing.
 */
void handleWebMessage(
    WebKitUserContentManager* contentManager,
    WebKitJavascriptResult* jsResult,
    gpointer arg)
{
    JSCValue* jsValue = webkit_javascript_result_get_js_value(jsResult);

    if (jsc_value_is_string(jsValue))
    {
        String value = jsc_value_to_string(jsValue);
        ((WebMessageReceivedDelegate)arg)(value);
        g_free(value);
    }

    webkit_javascript_result_unref(jsResult);
}

/**
 * \brief Pulls a resource's data buffer from managed code and passes it to the web view.
 */
void handleWebResourceRequest(WebKitURISchemeRequest* request, gpointer data)
{
    auto delegate = (WebResourceRequestedDelegate)data;
    auto uri = webkit_uri_scheme_request_get_uri(request);

    int numBytes;
    String contentType;
    auto result = delegate((String)uri, &numBytes, &contentType);

    GInputStream* stream = g_memory_input_stream_new_from_data(result, numBytes, nullptr);
    webkit_uri_scheme_request_finish(request, stream, -1, contentType);

    g_object_unref(stream);
    delete[] contentType;
}

TestudoWindow::TestudoWindow(TestudoWindowConfiguration* configuration)
{
    // Create the window
    window = gtk_window_new(GTK_WINDOW_TOPLEVEL);
    gtk_window_set_default_size(GTK_WINDOW(window), -1, -1);
    gtk_window_set_position(GTK_WINDOW(window), GTK_WIN_POS_NONE);

    // Create the web view and add it to the window
    contentManager = webkit_user_content_manager_new();
    webView = webkit_web_view_new_with_user_content_manager(contentManager);
    gtk_container_add(GTK_CONTAINER(window), webView);

    // Setup interop script
    auto script = webkit_user_script_new(
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
        WEBKIT_USER_CONTENT_INJECT_ALL_FRAMES, WEBKIT_USER_SCRIPT_INJECT_AT_DOCUMENT_START, nullptr, nullptr);

    webkit_user_content_manager_add_script(contentManager, script);
    webkit_user_script_unref(script);

    g_signal_connect(contentManager, "script-message-received::visium", 
        G_CALLBACK(handleWebMessage), (void*)configuration->webMessageReceivedHandler);

    webkit_user_content_manager_register_script_message_handler(contentManager, "visium");

    // Setup custom scheme handler
    auto context = webkit_web_context_get_default();
    webkit_web_context_register_uri_scheme(context, 
        "app",
        (WebKitURISchemeRequestCallback)handleWebResourceRequest, 
        (void*)configuration->webResourceRequestedHandler,
        nullptr);

    // Navigate to the initial URI
    if (configuration->initialUri != nullptr)
    {
        navigate(configuration->initialUri);
    }

    // Show the window
    gtk_widget_show_all(window);
}

TestudoWindow::~TestudoWindow()
{
    gtk_widget_destroy(window);
}

void TestudoWindow::navigate(String uri)
{
    webkit_web_view_load_uri(WEBKIT_WEB_VIEW(webView), uri);
}

/** 
 * \brief Escapes characters in a JSON string for use with GTK web view.
 * \param string: The JSON string to format.
 * \returns The formatted JSON string.
 */
std::string escapeJson(const std::string &string)
{
	std::ostringstream o;

	for (auto c = string.cbegin(); c != string.cend(); c++)
	{
		switch (*c)
		{
		case '"':
			o << "\\\"";
			break;
		case '\\':
			o << "\\\\";
			break;
		case '\b':
			o << "\\b";
			break;
		case '\f':
			o << "\\f";
			break;
		case '\n':
			o << "\\n";
			break;
		case '\r':
			o << "\\r";
			break;
		case '\t':
			o << "\\t";
			break;
		default:
			if ('\x00' <= *c && *c <= '\x1f')
			{
				o << "\\u"
				  << std::hex << std::setw(4) << std::setfill('0') << (int)*c;
			}
			else
			{
				o << *c;
			}
		}
	}

	return o.str();
}

/**
 * \brief Callback to pass to \ref webkit_web_view_run_javascript to execute
 * when the JavaScript has completed execution. Simply sets the invocation
 * result to true so the caller knows to continue.
 * \param data Pointer to the \ref JSInvocation object.
 */
static void webViewRunJavaScriptCallback(GObject* object, GAsyncResult* result, gpointer data)
{
    auto invocation = (JSInvocation*)data;
    invocation->isCompleted = true;
}

void TestudoWindow::sendMessage(String message)
{
    // Format the message appropriately for Linux
    std::string javascript;
    javascript.append("__dispatchMessageCallback(\"");
    javascript.append(escapeJson(message));
    javascript.append("\")");

    // Invoke the JavaScript evaluation
    JSInvocation invocation = {};
    webkit_web_view_run_javascript(WEBKIT_WEB_VIEW(webView), 
        javascript.c_str(), nullptr, webViewRunJavaScriptCallback, &invocation);

    // Block until the invocation is complete
    while (!invocation.isCompleted)
    {
        g_main_context_iteration(nullptr, true);
    }
}

#endif