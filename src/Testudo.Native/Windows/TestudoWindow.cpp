#if _WIN32

#include "TestudoWindow.h"
#include "WindowsHelper.h"

#include <comdef.h>
#include <dwmapi.h>
#include <shlwapi.h>
#include <WebView2EnvironmentOptions.h>
#include <windows.h>
#include <wrl.h>

using namespace Microsoft::WRL;

std::wstring TestudoWindow::generateClassName()
{
    // Create a GUID
    GUID guid;
    DISPLAY_HRESULT(CoCreateGuid(&guid));

    // Convert the GUID to a string
    WCHAR wszGuid[40] = {};
    StringFromGUID2(guid, wszGuid, 40);
    const std::wstring guidString(wszGuid);
    return guidString;
}

HRESULT TestudoWindow::webMessageReceivedHandler(
    ICoreWebView2* sender,
    ICoreWebView2WebMessageReceivedEventArgs* args)
{
    // Get the message
    wil::unique_cotaskmem_string message;
    CHECK_HRESULT(args->TryGetWebMessageAsString(&message));

    // Pass the message back to managed code
    _configuration->webMessageReceivedHandler(this, message.get());

    return S_OK;
}

HRESULT TestudoWindow::webResourceRequestedHandler(
    ICoreWebView2* sender,
    ICoreWebView2WebResourceRequestedEventArgs* args)
{
    // Get the request
    ICoreWebView2WebResourceRequest* request;
    CHECK_HRESULT(args->get_Request(&request));

    // Get the URI of the requested resource
    wil::unique_cotaskmem_string uri;
    CHECK_HRESULT(request->get_Uri(&uri));

    // Pass the request back to managed code
    int sizeBytes;
    String contentType;
    const wil::unique_cotaskmem data(_configuration->
        webResourceRequestedHandler(this, uri.get(), &sizeBytes, &contentType));

    // Create the response object from the resulting resource
    if (data != nullptr && contentType != nullptr)
    {
        const auto stream = SHCreateMemStream(static_cast<BYTE*>(data.get()), sizeBytes);
        const auto type = L"Content-Type: " + std::wstring(contentType);
        wil::com_ptr<ICoreWebView2WebResourceResponse> response;
        CHECK_HRESULT(_webViewEnvironment->CreateWebResourceResponse(stream, 200, L"OK", type.c_str(), &response));
        CHECK_HRESULT(args->put_Response(response.get()));
    }

    return S_OK;
}

HRESULT TestudoWindow::createCoreWebView2ControllerHandler(
    HRESULT errorCode,
    ICoreWebView2Controller* createdController)
{
    webviewController = createdController;
    CHECK_HRESULT(webviewController->get_CoreWebView2(&_webView));

    // Resize WebView to fit the bounds of the parent window
    RECT bounds;
    GetClientRect(_hWnd, &bounds);
    CHECK_HRESULT(webviewController->put_Bounds(bounds));

    // Setup interop script
    CHECK_HRESULT(_webView->AddScriptToExecuteOnDocumentCreated(
        L"window.external = { "
            "sendMessage: function(message) { "
                "window.chrome.webview.postMessage(message); "
            "}, "
            "receiveMessage: function(callback) { "
                "window.chrome.webview.addEventListener(\'message\', function(e) { callback(e.data); }); "
            "} "
        "};",
        nullptr));

    // Specify that all URIs should be intercepted and passed to webResourceRequestedHandler
    CHECK_HRESULT(_webView->AddWebResourceRequestedFilter(L"*", COREWEBVIEW2_WEB_RESOURCE_CONTEXT_ALL));

    // Register message handlers
    EventRegistrationToken token;
    CHECK_HRESULT(_webView->add_WebMessageReceived(Callback<ICoreWebView2WebMessageReceivedEventHandler>
        (this, &TestudoWindow::webMessageReceivedHandler).Get(), &token));
    CHECK_HRESULT(_webView->add_WebResourceRequested(Callback<ICoreWebView2WebResourceRequestedEventHandler>
        (this, &TestudoWindow::webResourceRequestedHandler).Get(), &token));

    // Navigate to the startup page
    CHECK_HRESULT(_webView->Navigate(_configuration->initialUri));

    return S_OK;
}

HRESULT TestudoWindow::createCoreWebView2EnvironmentHandler(
    HRESULT errorCode,
    ICoreWebView2Environment* createdEnvironment)
{
    _webViewEnvironment = createdEnvironment;

    // Create the web view controller
    CHECK_HRESULT(createdEnvironment->CreateCoreWebView2Controller(_hWnd,
        Callback<ICoreWebView2CreateCoreWebView2ControllerCompletedHandler>
        (this, &TestudoWindow::createCoreWebView2ControllerHandler).Get()));

    return S_OK;
}

TestudoWindow::TestudoWindow(const TestudoWindowConfiguration* configuration): ITestudoWindow(configuration)
{
    _configuration = configuration;
    const auto hInstance = GetModuleHandle(nullptr);
    const auto className = generateClassName();

    // Register the window class
    WNDCLASSEX windowClass = {};
    windowClass.cbSize = sizeof WNDCLASSEX;
    windowClass.style = CS_HREDRAW | CS_VREDRAW;
    windowClass.lpfnWndProc = WindowsHelper::windowProcedure;
    windowClass.hInstance = hInstance;
    windowClass.lpszClassName = className.c_str();
    windowClass.hIcon = static_cast<HICON>(_configuration->hIcon);
    windowClass.hCursor = LoadCursor(nullptr, IDC_ARROW);
    windowClass.hbrBackground = CreateSolidBrush(RGB(50, 50, 50));
    CHECK_WIN32_ERROR(RegisterClassEx(&windowClass));

    // Create the window
    _hWnd = CreateWindowEx(0,
                           className.c_str(),
                           configuration->title,
                           WS_OVERLAPPEDWINDOW,
                           configuration->left,
                           configuration->top,
                           configuration->width,
                           configuration->height,
                           nullptr,
                           nullptr,
                           hInstance,
                           nullptr);
    CHECK_WIN32_ERROR(_hWnd);
    WindowsHelper::registerWindow(_hWnd, this);

    // Set dark mode on Windows 10+ to prevent title bar going white when window loses focus
    int state = 1;
    DISPLAY_HRESULT(DwmSetWindowAttribute(_hWnd, DWMWA_USE_IMMERSIVE_DARK_MODE, &state, sizeof(int)));
}

TestudoWindow::~TestudoWindow()
{
    DestroyWindow(_hWnd);
}

void TestudoWindow::show()
{
    // Show the window
    ShowWindow(_hWnd, SW_SHOWDEFAULT);
    UpdateWindow(_hWnd);

    // Create the web view
    const auto options = Make<CoreWebView2EnvironmentOptions>();
    DISPLAY_HRESULT(options->put_AdditionalBrowserArguments(L"--kiosk"));
    DISPLAY_HRESULT(CreateCoreWebView2EnvironmentWithOptions(nullptr, nullptr, options.Get(),
        Callback<ICoreWebView2CreateCoreWebView2EnvironmentCompletedHandler>
            (this, &TestudoWindow::createCoreWebView2EnvironmentHandler).Get()));
}

void TestudoWindow::navigate(const String uri) const
{
    DISPLAY_HRESULT(_webView->Navigate(uri));
}

void TestudoWindow::sendMessage(const String message) const
{
    DISPLAY_HRESULT(_webView->PostWebMessageAsString(message));
}

void TestudoWindow::resizeWebView(const RECT* bounds) const
{
    if (webviewController != nullptr)
    {
        DISPLAY_HRESULT(webviewController->put_Bounds(*bounds));
    }
}

#endif
