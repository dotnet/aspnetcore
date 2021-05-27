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
            services.AddSingleton(new TokenBucketRateLimiter(2, 2));

            services.AddRequestLimiter(options =>
            {
                options.SetDefaultPolicy(new ConcurrencyLimiter(new ConcurrencyLimiterOptions { ResourceLimit = 100 }));
                options.AddPolicy("ipPolicy", policy =>
                {
                    // Add instance
                    policy.AddAggregatedLimiter(new IPAggregatedRateLimiter(2, 2));
                });
                options.AddPolicy("rate", policy =>
                {
                    // Add from DI
                    policy.AddLimiter<TokenBucketRateLimiter>();
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
                }).EnforceDefaultRequestLimit();

                endpoints.MapGet("/instance", async context =>
                {
                    await Task.Delay(5000);
                    await context.Response.WriteAsync("Hello World!");
                }).EnforceRequestLimit(new TokenBucketRateLimiter(2, 2));

                endpoints.MapGet("/concurrent", async context =>
                {
                    await Task.Delay(5000);
                    await context.Response.WriteAsync("Wrote!");
                }).EnforceRequestConcurrencyLimit(concurrentRequests: 1);

                endpoints.MapGet("/adhoc", async context =>
                {
                    await Task.Delay(5000);
                    await context.Response.WriteAsync("Tested!");
                }).EnforceRequestRateLimit(requestPerSecond: 2);

                endpoints.MapGet("/ipFromDI", async context =>
                {
                    await Task.Delay(5000);
                    await context.Response.WriteAsync("IP limited!");
                }).EnforceRequestLimitPolicy("ipPolicy");

                endpoints.MapGet("/multiple", async context =>
                {
                    await Task.Delay(5000);
                    await context.Response.WriteAsync("IP limited!");
                })
                .EnforceRequestLimitPolicy("ipPolicy")
                .EnforceRequestLimitPolicy("rate");

                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
