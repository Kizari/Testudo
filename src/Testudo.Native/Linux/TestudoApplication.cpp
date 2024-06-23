#ifdef __linux__

#include <condition_variable>

#include "../TestudoApplication.h"

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
    bool is_completed;
};

/** Used to synchronise main thread invocations. */
std::mutex invocation_lock;

TestudoApplication::TestudoApplication()
{
    gtk_init(nullptr, nullptr);
}

TestudoApplication::~TestudoApplication()
{
    gtk_main_quit();
}

void TestudoApplication::run()
{
    gtk_main();
}

// ReSharper disable once CppParameterMayBeConst
gboolean TestudoApplication::invoke_function(gpointer data)
{
    const auto invocation = static_cast<Invocation*>(data);
    invocation->action();
    {
        std::lock_guard guard(invocation_lock);
        invocation->is_completed = true;
    }

    invocation->completion.notify_one();
    return false;
}

void TestudoApplication::invoke(const Action action)
{
    Invocation invocation = {};
    invocation.action = action;
    gdk_threads_add_idle(invoke_function, &invocation);

    std::unique_lock lock(invocation_lock);
    invocation.completion.wait(lock, [&] { return invocation.is_completed; });
}

String TestudoApplication::open_folder_dialog()
{
    String result = nullptr;
    const auto dialog = gtk_file_chooser_dialog_new("Select Directory", nullptr,
                                                    GTK_FILE_CHOOSER_ACTION_SELECT_FOLDER,
                                                    GTK_STOCK_CANCEL, GTK_RESPONSE_CANCEL,
                                                    GTK_STOCK_OPEN, GTK_RESPONSE_ACCEPT,
                                                    nullptr);

    const auto response = gtk_dialog_run(GTK_DIALOG(dialog));

    if (response == GTK_RESPONSE_ACCEPT)
    {
        result = gtk_file_chooser_get_filename(GTK_FILE_CHOOSER(dialog));
    }

    gtk_widget_destroy(dialog);
    return result;
}


#endif
