# WorkerClient

A lightweight library for running .NET code in a WebWorker from Blazor WebAssembly, keeping your UI responsive during heavy computations. If need more feature complete and opinionated solution, have look at https://github.com/Tewr/BlazorWorker community project as an alternative.

## Quick Start

### 1. Register the Service

In your `Program.cs`, register the WorkerClient service:

```csharp
using WebWorkerTemplate.WorkerClient;

builder.Services.AddWorkerClient();
```

### 2. Initialize the Client

Inject and initialize in your component:

```csharp
@inject IWorkerClient Worker

private bool _workerReady;

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        await Worker.InitializeAsync();
        await Worker.WaitForReadyAsync(); // Pre-load .NET runtime in worker
        _workerReady = true;
        StateHasChanged();
    }
}
```

Use `_workerReady` to conditionally enable UI elements that depend on the worker:

```razor
<button @onclick="FetchDataAsync" disabled="@(!_workerReady)">
    @(_workerReady ? "Fetch Data" : "Loading Worker...")
</button>
```

### 3. Create a Worker Class

Create a class with `[JSExport]` static methods in your Blazor app. These methods run in the background WebWorker thread:

```csharp
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace YourApp.Worker;

[SupportedOSPlatform("browser")]
public partial class MyWorker
{
    [JSExport]
    public static string ComputeHeavyTask(string input)
    {
        // This runs in the WebWorker - won't block the UI
        Thread.Sleep(5000); // Simulating heavy work
        return $"Processed: {input}";
    }
}
```

### 4. Invoke Worker Methods

```csharp
// Call the worker and get JSON string result
string json = await Worker.InvokeStringAsync(
    "YourApp.Worker.MyWorker.ComputeHeavyTask",
    TimeSpan.FromSeconds(30),
    "my input");

// Deserialize the result
var result = JsonSerializer.Deserialize<MyResult>(json);
```

---

## `IWorkerClient` Interface

#### Methods

##### `InitializeAsync()`

```csharp
Task InitializeAsync()
```

Initializes the WebWorker client. **Must be called once before any other methods.** Safe to call multiple times (subsequent calls are no-ops).

---

##### `InvokeStringAsync(method, timeout, args)`

```csharp
Task<string> InvokeStringAsync(string method, TimeSpan timeout, params object[] args)
```

Invokes a worker method and returns the JSON string result. Deserialize manually using `JsonSerializer.Deserialize<T>()`.

| Parameter | Type | Description |
|-----------|------|-------------|
| `method`  | `string`   | Full method path: `"Namespace.ClassName.MethodName"` |
| `timeout` | `TimeSpan` | Maximum time to wait. Use `Timeout.InfiniteTimeSpan` to disable. |
| `args`    | `object[]` | Arguments passed to the worker method |

**Returns:** JSON string result from the worker method

**Throws:**
- `InvalidOperationException` - If `InitializeAsync()` was not called
- `TimeoutException` - If the method exceeds the specified timeout
- `JSException` - If the worker method throws an error

**Example:**
```csharp
// 2 minute timeout for long operations
var json = await Worker.InvokeStringAsync(
    "MyApp.Worker.GitHubWorker.FetchMetrics",
    TimeSpan.FromMinutes(2),
    "dotnet/aspnetcore",
    5);

var metrics = JsonSerializer.Deserialize<RepoMetrics>(json);

// No timeout
var json = await Worker.InvokeStringAsync(
    "MyApp.Worker.DataProcessor.Process",
    Timeout.InfiniteTimeSpan,
    data);
```

---

##### `WaitForReadyAsync()`

```csharp
Task WaitForReadyAsync()
```

Waits for the worker to be fully initialized and ready. The worker loads the .NET runtime lazily on first invocation. **Recommended:** Call this in `OnAfterRenderAsync` for eager initialization to avoid delays on first worker call.

**Example:**
```csharp
@inject IWorkerClient Worker

private bool _workerReady;

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        await Worker.InitializeAsync();
        await Worker.WaitForReadyAsync(); // Pre-load .NET runtime
        _workerReady = true;
        StateHasChanged();
    }
}
```

---

##### `Terminate()`

```csharp
void Terminate()
```

Terminates the current worker and creates a new one. All pending requests will be rejected. Use this to recover from a stuck worker.

⚠️ **Warning:** This is expensive as it requires reloading the .NET runtime in the new worker.

---

## Worker Method Requirements

1. Be `static`
2. Have the `[JSExport]` attribute
3. Be declared in a `partial` class
4. Use only [supported parameter/return types](https://learn.microsoft.com/dotnet/api/system.runtime.interopservices.javascript.jsmarshalasattribute)
5. Return JSON strings for complex objects.
