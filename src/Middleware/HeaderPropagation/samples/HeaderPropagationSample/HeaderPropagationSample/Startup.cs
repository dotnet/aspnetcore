using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HeaderPropagation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HeaderPropagationSample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();

            // A configuration similar to https://github.com/apache/incubator-zipkin-b3-propagation
            // but without sampling.
            //
            // To see this in action, send a request to / that includes headers like:
            // X-TraceId: 43a3cb27-f494-42c4-a05b-09fb256b9021
            // X-SpanId: 32756e86-0bef-409b-8dbc-86e6f8ddcea9
            //
            // This demonstrates three common uses of header propagation:
            // 1. Forward a header as-is
            // 2. Forward a header using a different header name
            // 3. Generate a new header value, or conditionally generate a header value
            services.AddHeaderPropagation(options =>
            {
                // Propagate the X-TraceId as a representation of the overall transaction.
                options.Headers.Add("X-TraceId");

                // Propagate the X-SpanId as X-ParentSpanId to represent the 'parent' of this HTTP call.
                options.Headers.Add(new HeaderPropagationEntry("X-SpanId", "X-ParentSpanId"));

                // Generate a new X-SpanId to represent this call, if it's being traced.
                options.Headers.Add(new HeaderPropagationEntry("X-SpanId")
                {
                    ValueFilter = (context) =>
                    {
                        return context.HttpContext.Request.Headers.ContainsKey("X-TraceId") ? Guid.NewGuid().ToString() : null;
                    },
                });
            });

            services
                .AddHttpClient("test")
                .AddHeaderPropagation();

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHttpClientFactory clientFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHeaderPropagation();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    foreach (var header in context.Request.Headers)
                    {
                        await context.Response.WriteAsync($"'/' Got Header '{header.Key}': {string.Join(", ", header.Value)}\r\n");
                    }

                    await context.Response.WriteAsync("Sending request to /forwarded\r\n");

                    var uri = UriHelper.BuildAbsolute(context.Request.Scheme, context.Request.Host, context.Request.PathBase, "/forwarded");
                    var client = clientFactory.CreateClient("test");
                    var response = await client.GetAsync(uri);

                    foreach (var header in response.RequestMessage.Headers)
                    {
                        await context.Response.WriteAsync($"Sent Header '{header.Key}': {string.Join(", ", header.Value)}\r\n");
                    }

                    await context.Response.WriteAsync("Got response\r\n");
                    await context.Response.WriteAsync(await response.Content.ReadAsStringAsync());
                });

                endpoints.MapGet("/forwarded", async context =>
                {
                    foreach (var header in context.Request.Headers)
                    {
                        await context.Response.WriteAsync($"'/forwarded' Got Header '{header.Key}': {string.Join(", ", header.Value)}\r\n");
                    }
                });
            });
        }
    }
}
