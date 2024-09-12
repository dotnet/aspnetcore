## About

`Microsoft.AspNetCore.Components.WebAssembly.Server` provides runtime server features for ASP.NET Core Blazor applications that have a client running under WebAssembly.

## Key Features

* Provides the ability to statically render components that utilize WebAssembly interactivity
* Enables debugging functionality for code running in WebAssembly
* Allows serialization and transmission of server-side authentication state for use during WebAssembly interactivity

## How to Use

To use `Microsoft.AspNetCore.Components.WebAssembly.Server`, follow these steps:

### Installation

```shell
dotnet add package Microsoft.AspNetCore.Components.WebAssembly.Server
```

### Configuration

To enable WebAssembly interactivity in a Blazor Web app, configure it in your app's `Program.cs`:

```csharp
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(BlazorWebApp.Client._Imports).Assembly);
```

Make sure to update the call to `AddAdditionalAssemblies` with any client assemblies that should be included in the Blazor application.

## Main Types

The main types provided by this package are:

* `WebAssemblyComponentsEndpointOptions`: Provides options for configuring interactive WebAssembly components
* `AuthenticationStateSerializationOptions`: Provides options for configuring the JSON serialization of the `AuthenticationState` to the WebAssembly client

## Additional Documentation

For additional documentation and examples, refer to the [official documentation](https://learn.microsoft.com/aspnet/core/blazor) on Blazor.

## Feedback &amp; Contributing

`Microsoft.AspNetCore.Components.WebAssembly.Server` is released as open-source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/aspnetcore).
