#pragma once

#ifdef _WIN32
    #define EXPORTED __declspec(dllexport)
    using String = const wchar_t*;
#else
#define EXPORTED
using String = const char*;
#endif

using Action = void(*)();
using WebMessageReceivedDelegate = void(*)(String message);
using WebResourceRequestedDelegate = void* (*)(String uri, int* size_bytes, String* content_type);
