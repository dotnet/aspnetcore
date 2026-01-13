# WorkerClient

A lightweight library for running .NET code in a WebWorker from Blazor WebAssembly, keeping your UI responsive during heavy computations.

## Quick Start

### 1. Initialize the Client

Call `InitializeAsync()` once, typically in `OnAfterRenderAsync`:

```csharp
using WebWorkerTemplate.WorkerClient;

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        await WorkerClient.InitializeAsync();
    }
}
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
// For methods returning strings/primitives
string result = await WorkerClient.InvokeJsonAsync<string>(
    "YourApp.Worker.MyWorker.ComputeHeavyTask", 
    "my input");
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

##### `InvokeJsonAsync<T>(method, args)`

```csharp
public static Task<T?> InvokeJsonAsync<T>(string method, params object[] args)
```

Invokes a worker method that returns JSON and deserializes the result.

| Parameter | Type | Description |
|-----------|------|-------------|
| `method` | `string` | Full method path: `"Namespace.ClassName.MethodName"` |
| `args` | `object[]` | Arguments passed to the worker method |

**Returns:** Deserialized result of type `T`

**Throws:**
- `InvalidOperationException` - If `InitializeAsync()` was not called
- `TimeoutException` - If the method exceeds `DefaultTimeout`
- `JSException` - If the worker method throws an error

**Example:**
```csharp
var metrics = await WorkerClient.InvokeJsonAsync<RepoMetrics>(
    "MyApp.Worker.GitHubWorker.FetchMetrics",
    "dotnet/aspnetcore",
    5);
```

---

##### `InvokeJsonAsync<T>(method, timeout, args)`

```csharp
public static Task<T?> InvokeJsonAsync<T>(string method, TimeSpan timeout, params object[] args)
```

Same as above, but with a custom timeout.

**Example:**
```csharp
// 2 minute timeout for long operations
var result = await WorkerClient.InvokeJsonAsync<Report>(
    "MyApp.Worker.ReportGenerator.Generate",
    TimeSpan.FromMinutes(2),
    reportId);

// No timeout
var result = await WorkerClient.InvokeJsonAsync<Data>(
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

await WorkerClient.InvokeJsonAsync<Result>("MyApp.Worker.LongTask.Run");

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

Waits for the worker to be fully initialized and ready. The worker loads the .NET runtime lazily on first invocation; use this if you want to pre-warm it.

**Example:**
```csharp
await WorkerClient.InitializeAsync();
await WorkerClient.WaitForReadyAsync(); // Pre-load .NET runtime in worker
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
4. Return JSON strings for complex objects (use `InvokeJsonAsync<T>`)
