#pragma once

#include "Testudo.h"

/**
 * \brief The configuration for a \ref TestudoWindow.
 */
struct TestudoWindowConfiguration
{
    /** The URI that the window's web view should begin at. */
    String initialUri; 

    /** The callback that handles received web messages. */
    WebMessageReceivedDelegate* webMessageReceivedHandler;

    /** The callback that handles retrieving web resources. */
    WebResourceRequestedDelegate* webResourceRequestedHandler;
};