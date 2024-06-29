#pragma once

#if _WIN32

#include <string>
#include <WebView2.h>
#include <wil/com.h>

#include "ITestudoWindow.h"

class TestudoWindow final : ITestudoWindow
{
private:
    /** Handle to the native window represented by this class. */
    HWND _hWnd;

    /** The configuration for this window. */
    const TestudoWindowConfiguration* _configuration;

    /** The web view environment for this window. */
    wil::com_ptr<ICoreWebView2Environment> _webViewEnvironment;

    /** The controller for the web view embedded in this window. */
    wil::com_ptr<ICoreWebView2Controller> webviewController;

    /** The web view embedded in this window. */
    wil::com_ptr<ICoreWebView2> _webView;

    /**
     * @brief Generates a random unique class name for a new window.
     * @return The randomly generated class name as a wide string.
     */
    static std::wstring generateClassName();

    /**
     * @brief Event handler for @ref ICoreWebView2.add_WebMessageReceived.
     * @param sender The web view that sent the event.
     * @param args The event arguments.
     * @return Always returns @ref S_OK.
     */
    HRESULT webMessageReceivedHandler(
        ICoreWebView2* sender,
        ICoreWebView2WebMessageReceivedEventArgs* args);

    /**
     * @brief Event handler for @ref ICoreWebView2.add_WebResourceRequested.
     * @param sender The web view that sent the event.
     * @param args The event arguments.
     * @return Always returns @ref S_OK.
     */
    HRESULT webResourceRequestedHandler(
        ICoreWebView2* sender,
        ICoreWebView2WebResourceRequestedEventArgs* args);

    /**
     * @brief Event handler for @ref ICoreWebView2Environment.CreateCoreWebView2Controller.
     * @param errorCode The error code representing errors that occured while creating the web view controller, if any.
     * @param createdController The newly created controller.
     * @return Whether the call was successful.
     */
    HRESULT createCoreWebView2ControllerHandler(
        HRESULT errorCode,
        ICoreWebView2Controller* createdController);

    /**
     * @brief Event handler for @ref CreateCoreWebView2Environment.
     * @param errorCode The error code representing errors that occured while creating the web view environment, if any.
     * @param createdEnvironment The newly created environment.
     * @return Whether the call was successful.
     */
    HRESULT createCoreWebView2EnvironmentHandler(
        HRESULT errorCode,
        ICoreWebView2Environment* createdEnvironment);

public:
    explicit TestudoWindow(const TestudoWindowConfiguration* configuration);

    ~TestudoWindow() override;

    void show() override;

    void navigate(String uri) const override;

    void sendMessage(String message) const override;

    void resizeWebView(const RECT* bounds) const;
};

#endif
