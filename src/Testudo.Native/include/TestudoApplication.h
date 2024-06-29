#pragma once

#include <condition_variable>

#include "Testudo.h"
#include "TestudoApplicationConfiguration.h"

/**
 * @brief Holds information pertaining to a main thread invocation.
 */
struct Invocation
{
    /** The action to execute on the main thread. */
    Action action;

    /** Notifies when the callback has finished executing. */
    std::condition_variable completion;

    /** Specifies whether or not the invocation has completed. */
    bool isCompleted;
};

/**
 * @brief Manages the operations of a native application.
 */
class TestudoApplication
{
public:
    /**
     * @brief Initializes the application.
     */
    explicit TestudoApplication(const TestudoApplicationConfiguration* pConfiguration);

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
    static String openFolderDialog();
};
