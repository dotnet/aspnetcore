using System;
using System.Diagnostics;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DiagnosticAdapter;
using Microsoft.Extensions.Logging;

namespace MiddlewareAnaysisSample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMiddlewareAnalysis();
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory factory, DiagnosticListener diagnosticListener)
        {
            // Displays all log levels
            factory.AddConsole(LogLevel.Debug);

            // Listen for middleware events and log them to the console.
            var listener = new TestDiagnosticListener();
            diagnosticListener.SubscribeWithAdapter(listener);

            // Build our application pipeline

            // Named via app.UseMiddleware<T>()
            app.UseDeveloperExceptionPage();
            app.UseIISPlatformHandler();

            // Anonymous method inline middleware
            app.Use((context, next) =>
            {
                // No-op
                return next();
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
                    await next();
                }
            });

            // Note there's always a default 404 middleware at the end of the pipeline.
        }

        public class TestDiagnosticListener
        {
            [DiagnosticName("Microsoft.AspNet.MiddlewareAnalysis.MiddlewareStarting")]
            public virtual void OnMiddlewareStarting(HttpContext httpContext, string name)
            {
                Console.WriteLine($"MiddlewareStarting: {name}; {httpContext.Request.Path}");
            }

            [DiagnosticName("Microsoft.AspNet.MiddlewareAnalysis.MiddlewareException")]
            public virtual void OnMiddlewareException(Exception exception, string name)
            {
                Console.WriteLine($"MiddlewareException: {name}; {exception.Message}");
            }

            [DiagnosticName("Microsoft.AspNet.MiddlewareAnalysis.MiddlewareFinished")]
            public virtual void OnMiddlewareFinished(HttpContext httpContext, string name)
            {
                Console.WriteLine($"MiddlewareFinished: {name}; {httpContext.Response.StatusCode}");
            }
        }
    }
}
