# WorkerClient

A lightweight library for running .NET code in a WebWorker from Blazor WebAssembly, keeping your UI responsive during heavy computations.

## Quick Start

### 1. Initialize the Client

Call `InitializeAsync()` and `WaitForReadyAsync()` once in `OnAfterRenderAsync` to eagerly load the worker:

```csharp
using WebWorkerTemplate.WorkerClient;

private bool _workerReady;

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        await WorkerClient.InitializeAsync();
        await WorkerClient.WaitForReadyAsync(); // Pre-load .NET runtime in worker
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

### 2. Create a Worker Class

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

### 3. Invoke Worker Methods

```csharp
// Call the worker and get JSON string result
string json = await WorkerClient.InvokeJsonAsync(
    "YourApp.Worker.MyWorker.ComputeHeavyTask", 
    "my input");

// Deserialize the result
var result = JsonSerializer.Deserialize<MyResult>(json);
```

> 💡 **Need a different invocation pattern?** The `WorkerClient.cs` file is part of your project. If `InvokeJsonAsync` doesn't fit your needs, open `WorkerClient.cs` and implement your own method using the existing `[JSImport]` bindings as a reference.

---

## API Reference

### `WorkerClient` Static Class

#### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `DefaultTimeout` | `TimeSpan` | 30 seconds | Default timeout for all worker invocations. Set to `Timeout.InfiniteTimeSpan` to disable. |

#### Methods

##### `InitializeAsync()`

```csharp
public static Task InitializeAsync()
```

Initializes the WebWorker client. **Must be called once before any other methods.** Safe to call multiple times (subsequent calls are no-ops).

**Throws:** `JSException` if the JavaScript module fails to load

---

##### `InvokeJsonAsync(method, args)`

```csharp
public static Task<string> InvokeJsonAsync(string method, params object[] args)
```

Invokes a worker method and returns the JSON string result. Deserialize manually using `JsonSerializer.Deserialize<T>()`.

| Parameter | Type | Description |
|-----------|------|-------------|
| `method` | `string` | Full method path: `"Namespace.ClassName.MethodName"` |
| `args` | `object[]` | Arguments passed to the worker method |

**Returns:** JSON string result from the worker method

**Throws:**
- `InvalidOperationException` - If `InitializeAsync()` was not called
- `TimeoutException` - If the method exceeds `DefaultTimeout`
- `JSException` - If the worker method throws an error

**Example:**
```csharp
var json = await WorkerClient.InvokeJsonAsync(
    "MyApp.Worker.GitHubWorker.FetchMetrics",
    "dotnet/aspnetcore",
    5);

var metrics = JsonSerializer.Deserialize<RepoMetrics>(json);
```

---

##### `InvokeJsonAsync(method, timeout, args)`

```csharp
public static Task<string> InvokeJsonAsync(string method, TimeSpan timeout, params object[] args)
```

Same as above, but with a custom timeout.

**Example:**
```csharp
// 2 minute timeout for long operations
var json = await WorkerClient.InvokeJsonAsync(
    "MyApp.Worker.ReportGenerator.Generate",
    TimeSpan.FromMinutes(2),
    reportId);

var result = JsonSerializer.Deserialize<Report>(json);

// No timeout
var json = await WorkerClient.InvokeJsonAsync(
    "MyApp.Worker.DataProcessor.Process",
    Timeout.InfiniteTimeSpan,
    data);
```

---

##### `SetProgressCallback(callback)`

```csharp
public static void SetProgressCallback(Action<string, int, int>? callback)
```

Sets a callback to receive progress updates from worker operations.

| Parameter | Type | Description |
|-----------|------|-------------|
| `callback` | `Action<string, int, int>?` | Receives `(message, current, total)`. Pass `null` to clear. |

**Example:**
```csharp
WorkerClient.SetProgressCallback((message, current, total) =>
{
    _status = $"{message} ({current}/{total})";
    InvokeAsync(StateHasChanged);
});

var json = await WorkerClient.InvokeJsonAsync("MyApp.Worker.LongTask.Run");
var result = JsonSerializer.Deserialize<Result>(json);

WorkerClient.SetProgressCallback(null); // Clear when done
```

To report progress from your worker, use `[JSImport]`:

```csharp
[SupportedOSPlatform("browser")]
public partial class LongTask
{
    [JSImport("globalThis.postProgress")]
    private static partial void ReportProgress(string message, int current, int total);

    [JSExport]
    public static string Run()
    {
        for (int i = 0; i < 100; i++)
        {
            ReportProgress("Processing...", i, 100);
            // ... work ...
        }
        return "done";
    }
}
```

---

##### `WaitForReadyAsync()`

```csharp
public static Task WaitForReadyAsync()
```

Waits for the worker to be fully initialized and ready. The worker loads the .NET runtime lazily on first invocation. **Recommended:** Call this in `OnAfterRenderAsync` for eager initialization to avoid delays on first worker call.

**Example:**
```csharp
private bool _workerReady;

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        await WorkerClient.InitializeAsync();
        await WorkerClient.WaitForReadyAsync(); // Pre-load .NET runtime
        _workerReady = true;
        StateHasChanged();
    }
}
```

---

##### `Terminate()`

```csharp
public static void Terminate()
```

Terminates the current worker and creates a new one. All pending requests will be rejected. Use this to recover from a stuck worker.

⚠️ **Warning:** This is expensive as it requires reloading the .NET runtime in the new worker.

---

## Worker Method Requirements

Your worker methods must:

1. Be `static` and `partial`
2. Have the `[JSExport]` attribute
3. Use only [supported parameter/return types](https://learn.microsoft.com/dotnet/api/system.runtime.interopservices.javascript.jsmarshalasattribute)
4. Return JSON strings for complex objects.
