// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BlazorWebAppPerPage.Client.Pages;
using BlazorWebAppPerPage.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

var unobservedExceptions = new System.Collections.Concurrent.ConcurrentBag<string>();
TaskScheduler.UnobservedTaskException += (_, e) =>
{
    unobservedExceptions.Add(e.Exception.ToString());
    app.Logger.LogError(e.Exception, "Unobserved task exception");
};
_ = Task.Run(async () =>
{
    while (true)
    {
        await Task.Delay(250);
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
});

long activeCircuits = 0;
long connectedCircuits = 0;
long minActiveCircuits = 0;
long minConnectedCircuits = 0;
var meterListener = new System.Diagnostics.Metrics.MeterListener();
meterListener.InstrumentPublished = (instrument, listener) =>
{
    if (instrument.Meter.Name == "Microsoft.AspNetCore.Components.Server.Circuits" &&
        (instrument.Name == "aspnetcore.components.circuit.active" || instrument.Name == "aspnetcore.components.circuit.connected"))
    {
        listener.EnableMeasurementEvents(instrument);
    }
};
meterListener.SetMeasurementEventCallback<long>((instrument, measurement, _, _) =>
{
    if (instrument.Name == "aspnetcore.components.circuit.active")
    {
        var current = Interlocked.Add(ref activeCircuits, measurement);
        Interlocked.Exchange(ref minActiveCircuits, Math.Min(Interlocked.Read(ref minActiveCircuits), current));
    }
    else if (instrument.Name == "aspnetcore.components.circuit.connected")
    {
        var current = Interlocked.Add(ref connectedCircuits, measurement);
        Interlocked.Exchange(ref minConnectedCircuits, Math.Min(Interlocked.Read(ref minConnectedCircuits), current));
    }
});
meterListener.Start();

app.MapGet("/test/unobserved-exceptions", () => unobservedExceptions.ToArray());
app.MapGet("/test/circuit-metrics", () => new
{
    active = Interlocked.Read(ref activeCircuits),
    connected = Interlocked.Read(ref connectedCircuits),
    minActive = Interlocked.Read(ref minActiveCircuits),
    minConnected = Interlocked.Read(ref minConnectedCircuits),
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
#pragma warning disable ASPDEPR011 // UseWebAssemblyDebugging is obsolete
    app.UseWebAssemblyDebugging();
#pragma warning restore ASPDEPR011
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(BlazorWebAppPerPage.Client._Imports).Assembly);

app.Run();
