// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.ResourceLimits;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RequestLimiter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RateLimiterSample
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddSingleton(new IPAggregatedRateLimiter(2, 2));
            services.AddSingleton(new RateLimiter(2, 2));

            services.AddRequestLimiter(options =>
            {
                options.SetDefaultPolicy(new ConcurrencyLimiter(100));
                // TODO: Consider a policy builder
                // TODO: Support combining/composing policies
                options.AddPolicy("concurrency", policy =>
                {
                    // Add instance
                    policy.AddLimiter(new ConcurrencyLimiter(1));
                });
                options.AddPolicy("rate", policy =>
                {
                    // Add from DI
                    policy.AddLimiter<RateLimiter>();
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            app.UseRouting();

            app.UseRateLimiter();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/defaultPolicy", async context =>
                {
                    await Task.Delay(5000);
                    await context.Response.WriteAsync("Default!");
                }).EnforceLimit();

                endpoints.MapGet("/instance", async context =>
                {
                    await Task.Delay(5000);
                    await context.Response.WriteAsync("Hello World!");
                }).EnforceLimit(new RateLimiter(2, 2));

                endpoints.MapGet("/concurrentPolicy", async context =>
                {
                    await Task.Delay(5000);
                    await context.Response.WriteAsync("Wrote!");
                }).EnforceLimit("concurrency");

                endpoints.MapGet("/adhoc", async context =>
                {
                    await Task.Delay(5000);
                    await context.Response.WriteAsync("Tested!");
                }).EnforceLimit(requestPerSecond: 2);

                endpoints.MapGet("/ipFromDI", async context =>
                {
                    await Task.Delay(5000);
                    await context.Response.WriteAsync("IP limited!");
                }).EnforceAggregatedLimit<IPAggregatedRateLimiter>();

                endpoints.MapGet("/multiple", async context =>
                {
                    await Task.Delay(5000);
                    await context.Response.WriteAsync("IP limited!");
                }).EnforceLimit("concurrency").EnforceLimit("rate");

                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
