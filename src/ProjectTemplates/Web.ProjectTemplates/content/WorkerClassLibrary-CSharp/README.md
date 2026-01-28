# Worker Class Library

A class library for running .NET code in a WebWorker from Blazor WebAssembly, keeping your UI responsive during heavy computations.

## Installation

```bash
dotnet new workerlib -o WorkerClient
```

## Integration

After creating the project, you need to manually integrate it with your Blazor WebAssembly application.

### 1. Add Project Reference

In your Blazor WebAssembly project (`.csproj`), add a reference to the WorkerClient:

```xml
<ProjectReference Include="..\WorkerClient\WorkerClient.csproj" />
```

Or use the CLI:
```bash
dotnet add reference ../WorkerClient/WorkerClient.csproj
```

### 2. Add to Solution (if applicable)

```bash
dotnet sln add WorkerClient/WorkerClient.csproj
```

### 3. Register the Service

In your Blazor app's `Program.cs`:

```csharp
using YourNamespace.WorkerClient;

builder.Services.AddWorkerClient();
```

### 4. Enable Unsafe Blocks (Required for [JSExport])

The `[JSExport]` attribute requires `AllowUnsafeBlocks` in the project that defines worker methods. Add to your Blazor app's `.csproj`:

```xml
<PropertyGroup>
  <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
</PropertyGroup>
```

> **Note:** This is required in *your* Blazor project, not the WorkerClient library.

## Usage

### 1. Create a Worker Class

Create a class with `[JSExport]` static methods in your Blazor app:

```csharp
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace YourApp.Worker;

[SupportedOSPlatform("browser")]
public static partial class MyWorker
{
    [JSExport]
    public static string DoWork(string input)
    {
        // Heavy computation here (runs in worker, not UI thread)
        return $"Processed: {input}";
    }

    [JSExport]
    public static async Task<string> DoWorkAsync(string baseUrl, string dataUrl)
    {
        using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        var data = await http.GetStringAsync(dataUrl);
        return data.ToUpper();
    }
}
```

### 2. Call from Blazor Component

```razor
@page "/my-page"
@rendermode @(new InteractiveWebAssemblyRenderMode(prerender: false))
@inject IWorkerClient Worker

<button @onclick="CallWorker" disabled="@(!_ready)">Run in Worker</button>
<p>@_result</p>

@code {
    private bool _ready;
    private string _result = "";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await Worker.InitializeAsync();
            await Worker.WaitForReadyAsync();
            _ready = true;
            StateHasChanged();
        }
    }

    private async Task CallWorker()
    {
        _result = await Worker.InvokeStringAsync(
            "YourApp.Worker.MyWorker.DoWork",
            TimeSpan.FromSeconds(30),
            "Hello");
    }
}
```

> **Note:** Use `prerender: false` when injecting `IWorkerClient` in Blazor Web Apps to avoid server-side rendering errors.

## API Reference

### IWorkerClient

| Method | Description |
|--------|-------------|
| `InitializeAsync()` | Loads the worker and .NET runtime |
| `WaitForReadyAsync()` | Waits for worker to be fully initialized |
| `InvokeStringAsync(method, timeout, args)` | Calls a `[JSExport]` method and returns string result |
| `Terminate()` | Kills the worker (next call creates a new one) |

### Timeout Handling

```csharp
// With timeout
var result = await Worker.InvokeStringAsync(
    "Namespace.Class.Method",
    TimeSpan.FromMinutes(2),
    arg1, arg2);

// No timeout
var result = await Worker.InvokeStringAsync(
    "Namespace.Class.Method",
    Timeout.InfiniteTimeSpan,
    arg1);
```

## Error Handling

```csharp
try
{
    var result = await Worker.InvokeStringAsync(...);
}
catch (JSException ex)
{
    // Worker method threw an exception
}
catch (TimeoutException ex)
{
    // Timeout exceeded
}
catch (InvalidOperationException ex)
{
    // InitializeAsync() was not called
}
```

## Tips

- **Return JSON strings** from workers for complex types—deserialize in the calling code
- **Terminate stuck workers** with `Worker.Terminate()` (expensive—reloads .NET runtime)
- **One worker at a time**: Calls are serialized; long-running tasks block subsequent calls

## Alternative

For a more feature-complete and opinionated solution, see the [BlazorWorker](https://github.com/Tewr/BlazorWorker) community project.
