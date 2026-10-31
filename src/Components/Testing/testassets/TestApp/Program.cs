// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using TestApp.Components;
using TestApp.Services;

if (Environment.GetEnvironmentVariable("E2E_FAIL_ON_STARTUP") is "1")
{
    Console.WriteLine("Intentional startup failure stdout");
    Console.Error.WriteLine("Intentional startup failure stderr");
    throw new InvalidOperationException("Intentional startup failure");
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddSingleton<IWeatherService, DefaultWeatherService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
#pragma warning disable ASPDEPR011 // UseWebAssemblyDebugging is obsolete
    app.UseWebAssemblyDebugging();
#pragma warning restore ASPDEPR011
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(TestApp.Client._Imports).Assembly);

app.Run();
