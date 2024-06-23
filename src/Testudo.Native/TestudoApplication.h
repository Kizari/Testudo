#pragma once

#include "Testudo.h"

#include <gtk/gtk.h>

/**
 * @brief Manages the operations of a native application.
 */
class TestudoApplication
{
private:
    /**
     * @brief Invokes the given @ref Invocation in a synchronous manner.
     * @param data: Pointer to the @ref Invocation object.
     * @returns Always false.
     */
    static gboolean invoke_function(gpointer data);

public:
    /**
     * @brief Initializes the application.
     */
    TestudoApplication();

    /**
     * @brief Ends the main program loop and cleans up resources.
     */
    ~TestudoApplication();

    /**
     * @brief Runs the main program loop for the lifetime of this class.
     * @remarks This function is blocking.
     */
    static void run();

    /**
     * @brief Invokes the given action on the main thread.
     * @param action The action to execute on the main thread.
     */
    static void invoke(Action action);

    /**
     * @brief Opens a native folder selection dialog.
     * @return The path to the selected folder, or null if no folder was selected.
     */
    static String open_folder_dialog();
};
