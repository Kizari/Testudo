#ifdef _WIN32

#include "TestudoApplication.h"
#include "TestudoApplicationConfiguration.h"
#include "WindowsHelper.h"

#include <comdef.h>
#include <format>
#include <windows.h>
#include <ShObjIdl.h>

/** Handle to the system tray icon hidden window that represents this application. */
HWND _processWindow;

TestudoApplication::TestudoApplication(const TestudoApplicationConfiguration* pConfiguration)
{
    const auto hInstance = GetModuleHandle(nullptr);

    // Generate the class name
    std::wstringstream stream;
    stream << pConfiguration->applicationName << "_SystemTrayIconClass";
    const auto className = stream.str();

    // Register system tray icon message-only window class
    WNDCLASS windowClass = {};
    windowClass.lpfnWndProc = WindowsHelper::windowProcedure;
    windowClass.hInstance = hInstance;
    windowClass.lpszClassName = className.c_str();
    RegisterClass(&windowClass);

    // Create the message-only window
    _processWindow = CreateWindowEx(0, className.c_str(), pConfiguration->applicationName,
                                   0, 0, 0, 0, 0,
                                   HWND_MESSAGE, nullptr, hInstance, nullptr);

    // Create the system tray notification
    NOTIFYICONDATA notification = {};
    notification.cbSize = sizeof NOTIFYICONDATA;
    notification.hWnd = _processWindow;
    notification.uID = 1;
    notification.uFlags = NIF_ICON | NIF_MESSAGE | NIF_TIP;
    notification.uCallbackMessage = WM_USER_SYSTRAY;
    notification.hIcon = static_cast<HICON>(pConfiguration->hIcon);

    // Show the system tray notification
    memcpy_s(notification.szTip, sizeof(notification.szTip),
             pConfiguration->applicationName, wcslen(pConfiguration->applicationName) * sizeof(wchar_t));
    Shell_NotifyIcon(NIM_ADD, &notification);
}

TestudoApplication::~TestudoApplication()
{
    // Delete the system tray notification
    NOTIFYICONDATA notification = {};
    notification.cbSize = sizeof NOTIFYICONDATA;
    notification.hWnd = _processWindow;
    notification.uID = 1;
    Shell_NotifyIcon(NIM_DELETE, &notification);

    // Destroy the message-only window
    DestroyWindow(_processWindow);
}

void TestudoApplication::run()
{
    MSG msg = {};
    while (GetMessage(&msg, nullptr, 0, 0))
    {
        TranslateMessage(&msg);
        DispatchMessage(&msg);
    }
}

void TestudoApplication::invoke(Action action)
{
    // Post the message to the invisible window's message queue
    Invocation invocation = {};
    PostMessage(_processWindow,
                WM_USER_INVOKE,
                reinterpret_cast<WPARAM>(action),
                reinterpret_cast<LPARAM>(&invocation));

    // Wait for the action to finish executing
    std::unique_lock lock(WindowsHelper::invocationLock);
    invocation.completion.wait(lock, [&] { return invocation.isCompleted; });
}

String TestudoApplication::openFolderDialog()
{
    String result = nullptr;
    IFileDialog* pFileDialog = nullptr;
    
    HRESULT hr = CoCreateInstance(CLSID_FileOpenDialog, nullptr, CLSCTX_ALL,
        IID_IFileOpenDialog, reinterpret_cast<void**>(&pFileDialog));

    if (SUCCEEDED(hr))
    {
        DWORD dwOptions;
        hr = pFileDialog->GetOptions(&dwOptions);
        
        if (SUCCEEDED(hr))
        {
            hr = pFileDialog->SetOptions(dwOptions | FOS_PICKFOLDERS);
            
            if (SUCCEEDED(hr))
            {
                hr = pFileDialog->Show(_processWindow);
                
                if (SUCCEEDED(hr))
                {
                    IShellItem* pItem;
                    hr = pFileDialog->GetResult(&pItem);
                    
                    if (SUCCEEDED(hr))
                    {
                        PWSTR pszFilePath = nullptr;
                        hr = pItem->GetDisplayName(SIGDN_FILESYSPATH, &pszFilePath);
                        
                        if (SUCCEEDED(hr))
                        {
                            result = pszFilePath;
                            CoTaskMemFree(pszFilePath);
                        }
                        else
                        {
                            DISPLAY_ERROR(hr);
                        }
                        
                        pItem->Release();
                    }
                    else
                    {
                        DISPLAY_ERROR(hr);
                    }
                }
                else if (hr != HRESULT_FROM_WIN32(ERROR_CANCELLED))
                {
                    DISPLAY_ERROR(hr);
                }
            }
            else
            {
                DISPLAY_ERROR(hr);
            }
        }
        else
        {
            DISPLAY_ERROR(hr);
        }
        
        pFileDialog->Release();
    }
    else
    {
        DISPLAY_ERROR(hr);
    }

    return result;
}

#endif
