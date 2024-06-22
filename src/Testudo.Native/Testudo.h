#pragma once

#ifdef _WIN32
    #define EXPORTED __declspec(dllexport)
    typedef wchar_t* String;
#else
    #define EXPORTED
    typedef char* String;
#endif

typedef void (*Action)();
typedef void (*WebMessageReceivedDelegate)(String message);
typedef void* (*WebResourceRequestedDelegate)(String uri, int* numBytes, String* contentType);