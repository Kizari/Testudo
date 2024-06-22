# Testudo

A work-in-progress experimental cross-platform library for creating lightweight desktop Blazor applications.

## Table of contents

* [Features](#Features)
* [Getting started](#Getting-started)
  * [Creating the project](#Creating-the-project)
  * [Setting up dependency injection](#Setting-up-dependency-injection)
  * [Creating the Blazor application](#Creating-the-Blazor-application)
  * [Launching the application window](#Launching-the-application-window)

## Features

* Supports Linux only (Windows and macOS planned for future)
* Supports background applications that need to still run while the main window is not shown
* Supports multiple windows
* Per-window dependency injection scopes (overrides Blazor's per-component scoping model)
* Integrates with `Microsoft.Extensions.DependencyInjection`
* Integrates with `Microsoft.Extensions.Hosting`

## Getting started

The following guide explains the basics of creating a Testudo application.

### Creating the project

1. Create a new .NET console application project as a base.
2. Add a folder called `wwwroot` to the root of the project, this will house the web content.
3. Open the `.csproj` file for editing.
4. Replace the project SDK with `Microsoft.NET.Sdk.Razor` as this is needed for using Razor components.
5. Add a package reference to Testudo.
6. Set everything in the `wwwroot` folder as an embedded resource. Testudo delivers embedded files to the web view at runtime so the application can be compiled into a single file.

The final `.csproj` should look like this:

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">

    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net8.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
    </PropertyGroup>
    
    <ItemGroup>
        <PackageReference Include="Testudo" Version="0.1.0" />
    </ItemGroup>
    
    <ItemGroup>
        <EmbeddedResource Include="wwwroot\**" />
    </ItemGroup>

</Project>
```

### Setting up dependency injection

Testudo operates via `Microsoft.Extensions.DependencyInjection` and provides extension methods to help you easily add Testudo to your service container. Ensure you have a reference to the NuGet package if you don't already have it as a dependency in your project.

```xml
    <ItemGroup>
        <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
        <PackageReference Include="Testudo" Version="0.1.0" />
    </ItemGroup>
```

Now you can create the service container and add Testudo as follows. Logging has also been enabled via `Microsoft.Extensions.Logging` in this example for later, but this is not necessary for Testudo to function.

```csharp
var services = new ServiceCollection()
    .AddLogging()
    .AddTestudo();
```

Since Blazor creates a new scope for every Razor component, the default `IServiceProvider` implementation must be replaced with Testudo's implementation. With this implementation, scopes are created per-window so that components within the same window/webview can share state.

```csharp
var provider = new TestudoServiceProviderFactory()
    .CreateServiceProvider(services);
```

If you are using `Microsoft.Extensions.Hosting`, you would add the `TestudoServiceProviderFactory` to your `IHostBuilder` instead.

```csharp
Host.CreateDefaultBuilder(args)
    .UseServiceProviderFactory(new TestudoServiceProviderFactory())
    .ConfigureServices((_, services) => services
        .AddTestudo()
    )
);
```

### Creating the Blazor application

Firstly, create a file in `wwwroot` named `index.html` and give it the standard Blazor markup.

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8"/>
    <meta content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no, viewport-fit=cover"
          name="viewport"/>
    <title>MyProject</title>
    <base href="/"/>
    <link href="MyProject.styles.css" rel="stylesheet"/>
</head>

<body>

<app>Loading...</app>

<script src="_framework/blazor.webview.js"></script>

</body>

</html>
```

Note that `_framework/blazor.webview.js` is used here instead of the usual non-webview script.

Next, create an `_Imports.razor` file in the project root for global `.razor` using statements. Add the common Blazor includes here, and any others you like.

```csharp
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
```

Create an `App.razor` file with the standard router setup.

```html
<Router AppAssembly="typeof(Program).Assembly">
    <Found Context="routeData">
        <RouteView RouteData="routeData" DefaultLayout="typeof(MainLayout)"/>
    </Found>
    <NotFound>
        <PageTitle>Not found</PageTitle>
        <LayoutView Layout="@typeof(MainLayout)">
            <p role="alert">Sorry, there's nothing at this address.</p>
        </LayoutView>
    </NotFound>
</Router>
```

Create a `MainLayout.razor` file. This will be a little different to the typical implementation, since Testudo isn't able to throw exceptions the usual way that Blazor does. You can use whatever method you like inside the `<ErrorContent>` component to handle the exception, but this example will use `Microsoft.Extensions.Logging` to log the exception to the logger configured in the service container, then display some text in the web view.

```html
@inject ILogger<MainLayout> Logger

<ErrorBoundary>
    <ChildContent>
        @Body
    </ChildContent>
    <ErrorContent Context="exception">
        @{
            Logger.LogError(exception, "An unhandled exception occurred in the Blazor application");
            <p>Unhandled exception! Please review the logs to see what went wrong.</p>
        }
    </ErrorContent>
</ErrorBoundary>
```

Finally, you can create any pages you like. This example will just use a `HelloWorld.razor` component as follows.

```html
@page "/"

<h1>Hello World!</h1>
```

### Launching the application window

To show your Blazor application in a desktop window, simply resolve the `IWindowManager` service and call `OpenWindow`.

```csharp
var windowManager = provider.GetRequiredService<IWindowManager>();
windowManager.OpenWindow<App>(new TestudoWindowConfiguration("/"));
```

Note how the type parameter given to `OpenWindow` is that of the Blazor application's root component. You can open a window with any component you like, but typically the main window would use something like this. Also note that the constructor parameter passed to `TestudoWindowConfiguration` matches the `@page` route declared in `HelloWorld.razor`. This tells the window to navigate to the `HelloWorld.razor` page when the window opens.

### Running the program

Finally, you can resolve the native application from the service container and call `Run` to begin the main program loop. The loop will terminate when the service container cleans up and disposes the service. You can also dispose it manually if you wish to terminate the application programmatically.

```csharp
var application = provider.GetRequiredService<ITestudoApplication>();
application.Run();
```

Beware that `ITestudoApplication.Run` will not return until the main program loop ends.

The final `Program.cs` may look something like this.

```csharp
// Configure the dependency injection container
var services = new ServiceCollection()
    .AddLogging()
    .AddTestudo();
    
// Create the dependency injection container
var provider = new TestudoServiceProviderFactory()
    .CreateServiceProvider(services);

// Launch the main window
var windowManager = provider.GetRequiredService<IWindowManager>();
windowManager.OpenWindow<App>(new TestudoWindowConfiguration("/"));

// Run the application
var application = provider.GetRequiredService<ITestudoApplication>();
application.Run();
```
