#include "WindowsHelper.h"

#include <comdef.h>

#include "TestudoApplication.h"

std::map<HWND, TestudoWindow*> WindowsHelper::_windows;

std::mutex WindowsHelper::invocationLock;

void WindowsHelper::registerWindow(const HWND hWnd, TestudoWindow* window)
{
    _windows[hWnd] = window;
}

LRESULT CALLBACK WindowsHelper::windowProcedure(
    const HWND hWnd, const UINT uMsg, const WPARAM wParam, const LPARAM lParam)
{
    switch (uMsg)
    {
    case WM_SIZE:
        if (_windows.contains(hWnd))
        {
            RECT bounds;
            GetClientRect(hWnd, &bounds);
            _windows[hWnd]->resizeWebView(&bounds);
        }
        break;
        
    case WM_USER_INVOKE:
        {
            reinterpret_cast<Action>(wParam)();
            const auto invocation = reinterpret_cast<Invocation*>(lParam);
            {
                std::lock_guard guard(invocationLock);
                invocation->isCompleted = true;
            }

            invocation->completion.notify_one();
            break;
        }
        
    default:
        return DefWindowProc(hWnd, uMsg, wParam, lParam);
    }

    return 0;
}