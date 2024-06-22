#ifdef __linux__

#include <condition_variable>
#include <gtk/gtk.h>

#include "../TestudoApplication.h"

/**
 * \brief Holds information pertaining to a main thread invocation.
 */
struct Invocation
{
    Action action; /**< The action to execute on the main thread. */
    std::condition_variable completion; /**< Notifies when the callback has finished executing. */
    bool isCompleted; /**< Specifies whether or not the invocation has completed. */
};

std::mutex invocationLock; /**< Used to synchronise main thread invocations. */

TestudoApplication::TestudoApplication()
{
    gtk_init(0, nullptr);
}

TestudoApplication::~TestudoApplication()
{
    gtk_main_quit();
}

void TestudoApplication::run()
{
    gtk_main();
}

/**
 * \brief Invokes the given \ref Invocation in a synchronous manner.
 * \param data: Pointer to the \ref Invocation object.
 * \returns Always false. This value is meaningless.
 */
static gboolean _invoke(gpointer data)
{
    auto invocation = static_cast<Invocation*>(data);
    invocation->action();
    {
        std::lock_guard<std::mutex> guard(invocationLock);
        invocation->isCompleted = true;
    }

    invocation->completion.notify_one();
    return false;
}

void TestudoApplication::invoke(Action action)
{
    Invocation invocation = {};
    invocation.action = action;
    gdk_threads_add_idle(_invoke, &invocation);

    std::unique_lock<std::mutex> lock(invocationLock);
    invocation.completion.wait(lock, [&] { return invocation.isCompleted; });
}

#endif