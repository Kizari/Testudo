#pragma once

#include "Testudo.h"

/**
 * @brief The configuration for a @ref TestudoWindow.
 */
struct TestudoWindowConfiguration
{
    /** The URI that the window's web view should begin at. */
    String initial_uri;

    /** The position of the window's left edge relative to the left of the screen. */
    int left;

    /** The position of the window's top edge relative to the top of the screen. */
    int top;

    /** The width of the window in pixels. */
    int width;

    /** The height of the window in pixels. */
    int height;

    /** Whether or not the window should be centered on the screen. Overrides @ref left and @ref top. */
    bool is_centered;

    /** The callback that handles received web messages. */
    WebMessageReceivedDelegate* web_message_received_handler;

    /** The callback that handles retrieving web resources. */
    WebResourceRequestedDelegate* web_resource_requested_handler;
};
