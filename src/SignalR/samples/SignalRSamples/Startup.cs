// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using SignalRSamples.ConnectionHandlers;
using SignalRSamples.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Threading;
using Microsoft.AspNetCore.Http;

namespace SignalRSamples
{
    public class CustomHubFilter : IHubFilter
    {
        private readonly ILogger _logger;
        private readonly IHttpContextAccessor _h;
        public CustomHubFilter(ILogger<CustomHubFilter> logger, IHttpContextAccessor h)
        {
            _logger = logger;
            _h = h;
        }

        public async ValueTask<object> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object>> next)
        {
            try
            {
                _h.HttpContext = invocationContext.Context.GetHttpContext();
                _logger.LogInformation("Starting invoke");
                var res = await next(invocationContext);
                if (invocationContext.HubMethod.Name == nameof(Chat.Echo))
                {
                    return "test";
                }
                else if (invocationContext.HubMethod.Name == nameof(Streaming.AsyncEnumerableCounter))
                {
                    return Add((IAsyncEnumerable<int>)res);

                    static async IAsyncEnumerable<int> Add(IAsyncEnumerable<int> enumerable)
                    {
                        await foreach(var item in enumerable)
                        {
                            yield return item + 5;
                        }
                    }
                }
                return res;
            }
            catch (Exception ex)
            {
                throw new HubException($"some error: {ex.Message}");
            }
            finally
            {
                _logger.LogInformation("Ending invoke");
            }
        }

        public Task OnConnectedAsync(HubCallerContext context, Func<HubCallerContext, Task> next)
        {
            _h.HttpContext = context.GetHttpContext();
            return next(context);
        }

        public Task OnDisconnectedAsync(HubCallerContext context, Func<HubCallerContext, Task> next)
        {
            _h.HttpContext = context.GetHttpContext();
            return next(context);
        }
    }

    public class InstanceFilter : IHubFilter
    {
        public ValueTask<object> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object>> next)
        {
            return next(invocationContext);
        }
    }

    public class Startup
    {

        private readonly JsonWriterOptions _jsonWriterOptions = new JsonWriterOptions { Indented = true };

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddConnections();

            services.AddSignalR(options =>
            {
                options.AddFilter<CustomHubFilter>();
                options.AddFilter(new InstanceFilter());
            })
            .AddHubOptions<Chat>(options =>
            {
                options.AddFilter(new InstanceFilter());
            })
            .AddMessagePackProtocol();
            //.AddStackExchangeRedis();

            services.AddSingleton<IHubFilter, CustomHubFilter>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseFileServer();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<DynamicChat>("/dynamic");
                endpoints.MapHub<Chat>("/default");
                endpoints.MapHub<Streaming>("/streaming");
                endpoints.MapHub<UploadHub>("/uploading");
                endpoints.MapHub<HubTChat>("/hubT");

                endpoints.MapConnectionHandler<MessagesConnectionHandler>("/chat");

                endpoints.MapGet("/deployment", async context =>
                {
                    var attributes = Assembly.GetAssembly(typeof(Startup)).GetCustomAttributes<AssemblyMetadataAttribute>();

                    context.Response.ContentType = "application/json";
                    await using (var writer = new Utf8JsonWriter(context.Response.BodyWriter, _jsonWriterOptions))
                    {
                        writer.WriteStartObject();
                        var commitHash = string.Empty;

                        foreach (var attribute in attributes)
                        {
                            writer.WriteString(attribute.Key, attribute.Value);

                            if (string.Equals(attribute.Key, "CommitHash"))
                            {
                                commitHash = attribute.Value;
                            }
                        }

                        if (!string.IsNullOrEmpty(commitHash))
                        {
                            writer.WriteString("GitHubUrl", $"https://github.com/aspnet/SignalR/commit/{commitHash}");
                        }

                        writer.WriteEndObject();
                        await writer.FlushAsync();
                    }
                });
            });
        }
    }
}
