#ifdef __linux__

#include "../TestudoApplication.h"

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

/**
 * @brief Invokes the given @ref Invocation in a synchronous manner.
 * @param data: Pointer to the @ref Invocation object.
 * @returns Always false.
 */
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
