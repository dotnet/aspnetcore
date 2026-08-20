// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
app.MapPost("/platform-api/echo", async context =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync(context.RequestAborted);

    context.Response.ContentType = "text/plain";
    context.Response.Headers["x-components-platform"] = "browser";
    await context.Response.WriteAsync(body, context.RequestAborted);
});
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(PlatformTestApp.Client._Imports).Assembly);

app.Run();
