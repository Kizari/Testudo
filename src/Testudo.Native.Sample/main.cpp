#include <windows.h>
#include "ITestudoWindow.h"
#include "TestudoApplication.h"

extern "C"
{
    __declspec(dllimport) TestudoApplication* TestudoApplication_Construct(const TestudoApplicationConfiguration* configuration);
    __declspec(dllimport) ITestudoWindow* TestudoWindow_Construct(const TestudoWindowConfiguration* configuration);
    __declspec(dllimport) void TestudoWindow_Show(ITestudoWindow* instance);
    __declspec(dllimport) void TestudoApplication_Run();
}

void nothing(void* pInstance, String arg) { }
void* redPage(void* pInstance, String uri, int* sizeBytes, String* contentType)
{
    const auto html = std::wstring(L"<html><body bgcolor=\"red\"></body></html>");
    const auto size = (wcslen(html.c_str()) + 1) * sizeof(wchar_t); // +1 for null terminator
    const auto buffer = new BYTE[size];
    memcpy(buffer, html.c_str(), size);
    *sizeBytes = size;
    *contentType = L"text/html";
    return (void*)buffer;
}


int main(int argc, char* argv[])
{
    CoInitialize(nullptr);
    TestudoApplicationConfiguration configuration = {};
    configuration.applicationName = L"Testudo.Native.Sample";
    configuration.hIcon = nullptr;
    auto application = TestudoApplication_Construct(&configuration);

    TestudoWindowConfiguration windowConfig = {};
    windowConfig.width = 1920;
    windowConfig.height = 1080;
    windowConfig.isCentered = true;
    windowConfig.title = L"Testudo.Native.Sample";
    windowConfig.initialUri = L"https://duckduckgo.com";
    windowConfig.hIcon = nullptr;
    windowConfig.hasWindowShell = true;
    windowConfig.areDevToolsEnabled = false;
    windowConfig.webMessageReceivedHandler = &nothing;
    windowConfig.webResourceRequestedHandler = &redPage;
    const auto window = TestudoWindow_Construct(&windowConfig);
    TestudoWindow_Show(window);

    TestudoApplication_Run();
    
    return 0;
}
