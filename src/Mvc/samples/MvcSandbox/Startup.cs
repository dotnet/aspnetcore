// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MvcSandbox
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting(options =>
            {
                options.ConstraintMap["slugify"] = typeof(SlugifyParameterTransformer);
            });
            services.AddServerSideBlazor();
            services.AddMvc()
                .SetCompatibilityVersion(Microsoft.AspNetCore.Mvc.CompatibilityVersion.Latest);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseEndpoints(builder =>
            {
                builder.MapGet(
                    requestDelegate: WriteEndpoints,
                    pattern: "/endpoints").WithDisplayName("Endpoints");

                builder.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                builder.MapControllerRoute(
                    name: "transform",
                    pattern: "Transform/{controller:slugify=Home}/{action:slugify=Index}/{id?}",
                    defaults: null,
                    constraints: new { controller = "Home" });

                builder.MapGet(
                    "/graph",
                    (httpContext) =>
                    {
                        using (var writer = new StreamWriter(httpContext.Response.Body, Encoding.UTF8, 1024, leaveOpen: true))
                        {
                            var graphWriter = httpContext.RequestServices.GetRequiredService<DfaGraphWriter>();
                            var dataSource = httpContext.RequestServices.GetRequiredService<EndpointDataSource>();
                            graphWriter.Write(dataSource, writer);
                        }

                        return Task.CompletedTask;
                    }).WithDisplayName("DFA Graph");

                builder.MapControllers();
                builder.MapRazorPages();
                builder.MapBlazorHub();
                builder.MapFallbackToPage("/Components");
            });
        }

        private static Task WriteEndpoints(HttpContext httpContext)
        {
            var dataSource = httpContext.RequestServices.GetRequiredService<EndpointDataSource>();

            var sb = new StringBuilder();
            sb.AppendLine("Endpoints:");
            foreach (var endpoint in dataSource.Endpoints.OfType<RouteEndpoint>().OrderBy(e => e.RoutePattern.RawText, StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine($"- {endpoint.RoutePattern.RawText} '{endpoint.DisplayName}'");
            }

            var response = httpContext.Response;
            response.StatusCode = 200;
            response.ContentType = "text/plain";
            return response.WriteAsync(sb.ToString());
        }

        public static void Main(string[] args)
        {
            var host = CreateWebHostBuilder(args)
                .Build();

            host.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            new WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureLogging(factory =>
                {
                    factory
                        .AddConsole()
                        .AddDebug();
                })
                .UseIISIntegration()
                .UseKestrel()
                .UseStartup<Startup>();
    }
}

