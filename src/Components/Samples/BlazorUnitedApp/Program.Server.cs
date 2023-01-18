// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BlazorUnitedApp.Data;
using BlazorUnitedApp.Pages;
using Microsoft.AspNetCore.Mvc.RazorPages;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles(); // Enable WebAssembly
app.UseStaticFiles();

app.UseRouting();

app.MapRazorComponents();
app.MapBlazorHub();

app.Map("/mycomponent", () =>
{
    return new RazorComponentResult
    (
        new FetchDataCore { InitialDate = DateTime.Now.AddYears(1000) }
    );
});

FetchDataCore.MapEndpoints(app); // Should be codegenned

app.Run();
