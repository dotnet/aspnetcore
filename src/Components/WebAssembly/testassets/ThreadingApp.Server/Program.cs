// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ThreadingApp.Server;

public class Program
{
    private static void Main(string[] args)
        => BuildWebHost(args).Run();

    public static IHost BuildWebHost(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // We require this line because we run in Production environment
        // and static web assets are only on by default during development.
        builder.Environment.ApplicationName = typeof(Program).Assembly.GetName().Name!;
        builder.WebHost.UseStaticWebAssets();

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveWebAssemblyComponents();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<Server.Components.App>()
            .AddInteractiveWebAssemblyRenderMode(options => { options.ServeMultithreadingHeaders = true; })
            .AddAdditionalAssemblies(typeof(ThreadingApp.Pages.Index).Assembly);

        return app;
    }
}
