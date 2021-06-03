// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Runtime.RateLimits;
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
            services.AddSingleton(
                new TokenBucketRateLimiter(
                    new TokenBucketRateLimiterOptions
                    {
                        PermitLimit = 2,
                        TokensPerPeriod = 2,
                        ReplenishmentPeriod = TimeSpan.FromSeconds(1)
                    }));
            services.AddSingleton(new AggregatedTokenBucketLimiter<IPAddress>(2, 2));

            services.AddRequestLimiter(options =>
            {
                options.SetDefaultPolicy(new ConcurrencyLimiter(new ConcurrencyLimiterOptions { PermitLimit = 100 }));
                options.AddPolicy("ipPolicy", policy =>
                {
                    // Add instance
                    policy.AddLimiter(
                        new TokenBucketRateLimiter(
                            new TokenBucketRateLimiterOptions
                            {
                                PermitLimit = 2,
                                TokensPerPeriod = 2,
                                ReplenishmentPeriod = TimeSpan.FromSeconds(1)
                            }));
                    policy.AddAggregatedLimiter(new AggregatedTokenBucketLimiter<IPAddress>(2, 2), context => context.Connection.RemoteIpAddress);
                });
                options.AddPolicy("diPolicy", policy =>
                {
                    // Add from DI
                    policy.AddLimiter<TokenBucketRateLimiter>();
                    policy.AddAggregatedLimiter<AggregatedTokenBucketLimiter<IPAddress>, IPAddress>(context => context.Connection.RemoteIpAddress);
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
                }).EnforceRequestLimit(new TokenBucketRateLimiter(
                    new TokenBucketRateLimiterOptions
                    {
                        PermitLimit = 2,
                        TokensPerPeriod = 2,
                        ReplenishmentPeriod = TimeSpan.FromSeconds(1)
                    }));

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
