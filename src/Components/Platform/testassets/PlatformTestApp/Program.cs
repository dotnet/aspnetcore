// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using PlatformTestApp.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddBrowserPlatform();

var app = builder.Build();

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(PlatformTestApp.Client._Imports).Assembly);

// Records whether the browser aborted each request of the abort link demo, which the page itself cannot see.
// The held requests end only when the browser aborts them, so no step depends on a delay.
var abortLinkRequests = new ConcurrentDictionary<string, AbortLinkRequest>();
app.MapGet("/abortlink/respond", (string id) =>
{
    abortLinkRequests.GetOrAdd(id, _ => new()).Set("completed");
    return Results.Text("ok");
});
app.MapGet("/abortlink/hold-headers", async (string id, HttpContext context) =>
{
    var request = abortLinkRequests.GetOrAdd(id, _ => new());
    request.Set("started");
    try
    {
        await Task.Delay(AbortLinkRequest.HoldTimeout, context.RequestAborted);
    }
    catch (OperationCanceledException)
    {
        request.Set("aborted");
        return Results.Empty;
    }

    request.Set("not aborted");
    return Results.Text("ok");
});
app.MapGet("/abortlink/hold-body", async (string id, HttpContext context) =>
{
    var request = abortLinkRequests.GetOrAdd(id, _ => new());
    request.Set("started");
    context.Response.ContentType = "text/plain";
    try
    {
        await context.Response.WriteAsync("chunk\n", context.RequestAborted);
        await context.Response.Body.FlushAsync(context.RequestAborted);
        await Task.Delay(AbortLinkRequest.HoldTimeout, context.RequestAborted);
    }
    catch (Exception ex) when (ex is OperationCanceledException or IOException)
    {
        request.Set("aborted");
        return;
    }

    request.Set("not aborted");
});
app.MapGet("/abortlink/log/{id}", (string id) =>
    abortLinkRequests.TryGetValue(id, out var request) ? request.State : "unknown");
// Answers once the request reaches the state, or with its state at the timeout.
app.MapGet("/abortlink/wait/{id}/{state}", (string id, string state) =>
    abortLinkRequests.GetOrAdd(id, _ => new()).WaitAsync(state));

app.Run();

internal sealed class AbortLinkRequest
{
    // Long enough that only an abort ends a held request during a test.
    public static readonly TimeSpan HoldTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10);

    private readonly object _lock = new();
    private TaskCompletionSource _changed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public string State { get; private set; } = "unknown";

    public void Set(string state)
    {
        TaskCompletionSource changed;
        lock (_lock)
        {
            State = state;
            changed = _changed;
            _changed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        changed.SetResult();
    }

    public async Task<string> WaitAsync(string state)
    {
        using var timeout = new CancellationTokenSource(WaitTimeout);
        while (true)
        {
            Task changed;
            lock (_lock)
            {
                if (State == state)
                {
                    return State;
                }

                changed = _changed.Task;
            }

            try
            {
                await changed.WaitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                return State;
            }
        }
    }
}
