#pragma once
#include "Testudo.h"

/**
 * @brief The configuration for the @ref TestudoApplication instance.
 */
struct TestudoApplicationConfiguration
{
    /** The display-friendly name of the application. */
    String applicationName;

    /** The application icon. */
    void* hIcon;
};
