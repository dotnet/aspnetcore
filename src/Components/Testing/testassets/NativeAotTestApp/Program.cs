// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using NativeAotTestApp.Components;
using NativeAotTestApp.Models;
using NativeAotTestApp.Serialization;
using NativeAotTestApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
#pragma warning disable ASPNETCORE9004
        options.JsonTypeInfoResolvers.Add(NativeAotJsonContext.Default);
        options.JsonTypeInfoResolvers.Add(ResolverFirstJsonContext.Default);
        options.JsonTypeInfoResolvers.Add(ResolverSecondJsonContext.Default);
#pragma warning restore ASPNETCORE9004
    });
builder.Services.AddSingleton<IGreetingService, GreetingService>();
builder.Services.AddSingleton<PersistentComponentStateSerializer<PersistenceSnapshot>, PersistenceSnapshotSerializer>();

var app = builder.Build();

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
