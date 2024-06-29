#pragma once

#if _WIN32

#include <mutex>
#include <windows.h>
#include <map>
#include <sstream>

#include "TestudoWindow.h"

/** Represents an invocation on the UI thread. */
#define WM_USER_INVOKE (WM_USER + 1)

/** Represents a message that was sent due to an interaction with the system tray icon. */
#define WM_USER_SYSTRAY (WM_USER + 2)

/**
 * @brief Checks if the result contains an error code and displays a message box if so.
 * @param func The function call that produces the HRESULT.
 * @remarks Returns the given error code if the result contained an error.
 */
#define CHECK_HRESULT(func) \
    { \
        HRESULT result = func; \
        if (result != S_OK) \
        { \
            const _com_error error(result); \
            std::wstringstream stream; \
            stream << error.ErrorMessage() << "\n\n"; \
            stream << "[line " << __LINE__ << "]: " << __FILE__; \
            auto body = stream.str(); \
            MessageBox(nullptr, body.c_str(), L"Error", MB_OK); \
            return result; \
        } \
    } \
    do { } while(0)

/**
 * @brief Checks if the result contains an error code and displays a message box if so.
 * @param func The function call that produces the HRESULT.
 * @remarks Returns nullptr if there was an error.
 */
#define CHECK_HRESULT_NULL(func) \
    { \
        HRESULT result = func; \
        if (result != S_OK) \
        { \
            const _com_error error(result); \
            std::wstringstream stream; \
            stream << error.ErrorMessage() << "\n\n"; \
            stream << "[line " << __LINE__ << "]: " << __FILE__; \
            auto body = stream.str(); \
            MessageBox(nullptr, body.c_str(), L"Error", MB_OK); \
            return nullptr; \
        } \
    } \
    do { } while(0)

/**
 * @brief Checks if the result contains an error code and displays a message box if so.
 * @param func The function call that produces the HRESULT.
 */
#define DISPLAY_HRESULT(func) \
    { \
        HRESULT result = func; \
        if (result != S_OK) \
        { \
            const _com_error error(result); \
            std::wstringstream stream; \
            stream << error.ErrorMessage() << "\n\n"; \
            stream << "[line " << __LINE__ << "]: " << __FILE__; \
            auto body = stream.str(); \
            MessageBox(nullptr, body.c_str(), L"Error", MB_OK); \
        } \
    } \
    do { } while(0)

/**
 * @brief Displays the message associated with the given HRESULT in a message box.
 * @param hresult The result to display the message for.
 */
#define DISPLAY_ERROR(hresult) \
    { \
        const _com_error error(hresult); \
        std::wstringstream stream; \
        stream << error.ErrorMessage() << "\n\n"; \
        stream << "[line " << __LINE__ << "]: " << __FILE__; \
        auto body = stream.str(); \
        MessageBox(nullptr, body.c_str(), L"Error", MB_OK); \
    } \
    do { } while (0)

/**
 * @brief Checks the result of a win32 function call and displays the error in a message box if one occurred.
 * @param func The win32 function call to check the result of.
 */
#define CHECK_WIN32_ERROR(func) \
    { \
        auto result = func; \
        if (!result) \
        { \
            const auto errorCode = GetLastError(); \
            const _com_error error(errorCode); \
            std::wstringstream stream; \
            stream << error.ErrorMessage() << "\n\n"; \
            stream << "[line " << __LINE__ << "]: " << __FILE__; \
            auto body = stream.str(); \
            MessageBox(nullptr, body.c_str(), L"Error", MB_OK); \
        } \
    } \
    do { } while(0)

/**
 * @brief Helper class for Windows-related functionality.
 */
class WindowsHelper
{
private:
    /** Holds references to windows so messages can be passed to the correct window from the main program loop. */
    static std::map<HWND, TestudoWindow*> _windows;
public:
    /** Used to synchronise main thread invocations. */
    static std::mutex invocationLock;

    /**
     * @brief Registers the given window with this window manager.
     * @param hWnd The handle to the native window.
     * @param window The interface representing the window.
     */
    static void registerWindow(HWND hWnd, TestudoWindow* window);

    /**
     * @brief Handles window messages for all windows in the application.
     * @param hWnd The window to which the message was sent.
     * @param uMsg The message identifier (generally a constant with the WM_ prefix).
     * @param wParam A pointer to an object passed in with the message.
     * @param lParam A pointer to an object passed in with the message.
     */
    static LRESULT CALLBACK windowProcedure(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam);
};

#endif
