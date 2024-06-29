#include <windows.h>
#include "ITestudoWindow.h"
#include "TestudoApplication.h"

extern "C"
{
    __declspec(dllimport) TestudoApplication* TestudoApplication_Construct(const TestudoApplicationConfiguration* configuration);
    __declspec(dllimport) ITestudoWindow* TestudoWindow_Construct(const TestudoWindowConfiguration* configuration);
    __declspec(dllimport) void TestudoApplication_Run();
}

void nothing(String arg) { }
void* redPage(String uri, int* sizeBytes, String* contentType)
{
    auto html = std::wstring(L"<html><body bgcolor=\"red\"></body></html>");
    auto buffer = html.c_str();
    *sizeBytes = wcslen(buffer) * sizeof(wchar_t);
    *contentType = L"application/html";
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
    windowConfig.webMessageReceivedHandler = &nothing;
    windowConfig.webResourceRequestedHandler = &redPage;
    auto window = TestudoWindow_Construct(&windowConfig);

    TestudoApplication_Run();
    
    return 0;
}
