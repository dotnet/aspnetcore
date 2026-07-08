using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace Company.WebWorker1;

// Define [JSExport] methods here to run them in a Web Worker.
// Call them from your Blazor app using WebWorkerClient.InvokeAsync.
// Example: await worker.InvokeAsync<string>("Company.WebWorker1.WorkerMethods.Greet", ["World"]);

[SupportedOSPlatform("browser")]
public static partial class WorkerMethods
{
    [JSExport]
    public static string Greet(string name) => $"Hello, {name}!";
}
