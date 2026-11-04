// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.DiagnosticAdapter;

namespace MiddlewareAnaysisSample;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMiddlewareAnalysis();
    }

    public void Configure(IApplicationBuilder app, ILoggerFactory factory, DiagnosticListener diagnosticListener)
    {
        // Listen for middleware events and log them to the console.
        var listener = new TestDiagnosticListener();
        diagnosticListener.SubscribeWithAdapter(listener);

        // Build our application pipeline

        // Named via app.UseMiddleware<T>()
        app.UseDeveloperExceptionPage();

        // Anonymous method inline middleware
        app.Use((context, next) =>
        {
            // No-op
            return next(context);
        });

        app.Map("/map", subApp =>
        {
            subApp.Run(context =>
            {
                return context.Response.WriteAsync("Hello World");
            });
        });

        // Low level anonymous method inline middleware, named Diagnostics.Middleware.Analysis.Startup+<>c by default
        app.Use(next =>
        {
            return context =>
            {
                return next(context);
            };
        });

        app.Map("/throw", throwApp =>
        {
            throwApp.Run(context => { throw new Exception("Application Exception"); });
        });

        // The home page.
        app.Properties["analysis.NextMiddlewareName"] = "HomePage";
        app.Use(async (context, next) =>
        {
            if (context.Request.Path == "/")
            {
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync("<html><body>Welcome to the sample<br><br>\r\n");
                await context.Response.WriteAsync("Click here to take a side branch: <a href=\"/map/foo\">Map</a><br>\r\n");
                await context.Response.WriteAsync("Click here to throw an exception: <a href=\"/throw\">Throw</a><br>\r\n");
                await context.Response.WriteAsync("Click here to for a 404: <a href=\"/404\">404</a><br>\r\n");
                await context.Response.WriteAsync("</body></html>\r\n");
                return;
            }
            else
            {
                await next(context);
            }
        });

        // Note there's always a default 404 middleware at the end of the pipeline.
    }

    public static Task Main(string[] args)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .ConfigureLogging((_, factory) =>
                {
                    factory.AddConsole();
                    factory.AddFilter("Console", level => level >= LogLevel.Debug);
                })
                .UseKestrel()
                .UseIISIntegration()
                .UseStartup<Startup>();
            }).Build();

        return host.RunAsync();
    }

    public class TestDiagnosticListener
    {
        [DiagnosticName("Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareStarting")]
        public virtual void OnMiddlewareStarting(HttpContext httpContext, string name)
        {
            Console.WriteLine($"MiddlewareStarting: {name}; {httpContext.Request.Path}");
        }

        [DiagnosticName("Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareException")]
        public virtual void OnMiddlewareException(Exception exception, string name)
        {
            Console.WriteLine($"MiddlewareException: {name}; {exception.Message}");
        }

        [DiagnosticName("Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareFinished")]
        public virtual void OnMiddlewareFinished(HttpContext httpContext, string name)
        {
            Console.WriteLine($"MiddlewareFinished: {name}; {httpContext.Response.StatusCode}");
        }
    }
}

