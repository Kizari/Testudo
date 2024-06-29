# Testudo.Native

This project is the native library that handles the platform-specific implementation of Testudo.

It is responsible for creating windows, managing web views, and dispatching callbacks to the main thread.

CMake is used to compile the binaries.

## Dependencies

A `Dependencies` folder must be created in the repository root and must contain an extracted copy of the
NuGet package `Microsoft.Web.WebView2` for building for Windows.