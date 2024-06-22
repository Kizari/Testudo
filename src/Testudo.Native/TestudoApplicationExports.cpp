#include "Testudo.h"
#include "TestudoApplication.h"

extern "C"
{
    /**
     * \brief Instantiates a new \ref TestudoApplication.
     * \returns A pointer to the newly created instance.
     */
    EXPORTED TestudoApplication* TestudoApplication_Construct()
    {
        return new TestudoApplication();
    }

    /**
     * \brief Ends the main program loop and destroys the \ref TestudoApplication instance.
     * \param instance A pointer to the \ref TestudoApplication instance that is to be destroyed.
     */
    EXPORTED void TestudoApplication_Destroy(TestudoApplication* instance)
    {
        delete instance;
    }

    /**
     * \brief Runs the main application loop until the given instance is destroyed.
     * \param instance A pointer to the \ref TestudoApplication whose \ref run method should be invoked.
     * \remarks This function is blocking.
     */
    EXPORTED void TestudoApplication_Run(TestudoApplication* instance)
    {
        instance->run();
    }

    /**
     * \brief Invokes the given action on the main thread.
     * \param instance The application whose main thread the action should be invoked on.
     * \param action The action to execute on the main thread.
     */
    EXPORTED void TestudoApplication_Invoke(TestudoApplication* instance, Action action)
    {
        instance->invoke(action);
    }
}