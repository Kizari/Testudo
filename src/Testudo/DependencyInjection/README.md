### Testudo.DependencyInjection

This namespace contains code related to dependency injection.

Everything in this directory and its subdirectories that retains the .NET Foundation license notice is
cloned from the .NET 8 runtime repository. The only non-cosmetic modifications are clearly marked with
XML document comments to show where changes have been made.

The reason this has been done is because there was seemingly no way to handle forcing Blazor to use a single
service scope per WebView. By default, client-side Blazor uses a scope per component, which is not feasible when
needing to share state between multiple components within the same WebView. However, singleton services were
not appropriate for this as the state of said services is not to be shared with other WebViews.

As such, modifications have been made to facilitate reusing the same scope for all scoped services within
a WebView within this project.