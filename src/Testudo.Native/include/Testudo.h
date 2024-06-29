#pragma once

#ifdef _WIN32
#define EXPORTED __declspec(dllexport)
using String = const wchar_t*;
#else
#define EXPORTED
using String = const char*;
#endif

/**
 * @brief Represents a parameterless callback with no return value.
 */
using Action = void(*)();

/**
 * @brief Represents a function pointer to a managed function that handles web messages.
 * @param pInstance Pointer to the @ref TestudoWindow instance whose web view received the message.
 * @param message The web message that was received.
 */
using WebMessageReceivedDelegate = void(*)(void* pInstance, String message);

/**
 * @brief Represents a function pointer to a managed function that handles web requests.
 * @param pInstance Pointer to the @ref TestudoWindow instance whose web view requested the resource.
 * @param uri The URI of the requested resource.
 * @param sizeBytes Will be populated with the size of the returned buffer.
 * @param contentType Will be populated with the MIME type of the resource.
 * @return Pointer to a buffer containing the data of the requested resource.
 */
using WebResourceRequestedDelegate = void* (*)(void* pInstance, String uri, int* sizeBytes, String* contentType);
