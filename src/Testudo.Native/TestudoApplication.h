#pragma once

#include "Testudo.h"

/**
 * \brief Manages the operations of a native application.
 */
class TestudoApplication
{
    public:
        /**
         * \brief Initializes the application.
         */
        TestudoApplication();

        /**
         * \brief Ends the main program loop and cleans up resources.
         */
        ~TestudoApplication();

        /**
         * \brief Runs the main program loop for the lifetime of this class.
         * \remarks This function is blocking.
         */
        void run();

        /**
         * \brief Invokes the given action on the main thread.
         * \param callback: The action to execute on the main thread.
         */
        void invoke(Action action);
};