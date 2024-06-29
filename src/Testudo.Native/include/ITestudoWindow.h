#pragma once

#include "Testudo.h"
#include "TestudoWindowConfiguration.h"

/**
 * @brief Manages a native window containing a web view.
 */
class ITestudoWindow
{
public:
    /**
     * @brief Creates a new native window.
     * @param configuration The window's configuration.
     */
    explicit ITestudoWindow(const TestudoWindowConfiguration* configuration)
    {
    }

    /**
     * @brief Destroys the window.
     */
    virtual ~ITestudoWindow() = default;

    /**
     * @brief Initializes the window's embedded web view, then shows the window.
     */
    virtual void show() = 0;

    /**
     * @brief Navigates this window's web view to the given URI.
     * @param uri The URI to navigate to.
     */
    virtual void navigate(String uri) const = 0;

    /**
     * @brief Sends a JavaScript message to this window's web view for evaluation.
     * @param message The JavaScript to send.
     */
    virtual void sendMessage(String message) const = 0;
};
