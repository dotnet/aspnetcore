// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HotAddSample;

// This sample shows how to dynamically add or remove prefixes for the underlying server.
// Be careful not to remove the prefix you're currently accessing because the connection
// will be reset before the end of the request.
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure<HttpSysOptions>(options =>
        {
            ServerOptions = options;
        });
    }

    public HttpSysOptions ServerOptions { get; set; }

    public void Configure(IApplicationBuilder app)
    {
        var addresses = ServerOptions.UrlPrefixes;
        addresses.Add("http://localhost:12346/pathBase/");

        app.Use(async (context, next) =>
        {
            // Note: To add any prefix other than localhost you must run this sample as an administrator.
            var toAdd = context.Request.Query["add"];
            if (!string.IsNullOrEmpty(toAdd))
            {
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync("<html><body>");
                try
                {
                    addresses.Add(toAdd);
                    await context.Response.WriteAsync("Added: <a href=\"" + toAdd + "\">" + toAdd + "</a>");
                }
                catch (Exception ex)
                {
                    await context.Response.WriteAsync("Error adding: " + toAdd + "<br>");
                    await context.Response.WriteAsync(ex.ToString().Replace(Environment.NewLine, "<br>"));
                }
                await context.Response.WriteAsync("<br><a href=\"" + context.Request.PathBase.ToUriComponent() + "\">back</a>");
                await context.Response.WriteAsync("</body></html>");
                return;
            }
            await next(context);
        });

        app.Use(async (context, next) =>
        {
            // Be careful not to remove the prefix you're currently accessing because the connection
            // will be reset before the response is sent.
            var toRemove = context.Request.Query["remove"];
            if (!string.IsNullOrEmpty(toRemove))
            {
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync("<html><body>");
                if (addresses.Remove(toRemove))
                {
                    await context.Response.WriteAsync("Removed: " + toRemove);
                }
                else
                {
                    await context.Response.WriteAsync("Not found: " + toRemove);
                }
                await context.Response.WriteAsync("<br><a href=\"" + context.Request.PathBase.ToUriComponent() + "\">back</a>");
                await context.Response.WriteAsync("</body></html>");
                return;
            }
            await next(context);
        });

        app.Run(async context =>
        {
            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync("<html><body>");
            await context.Response.WriteAsync("Listening on these prefixes: <br>");
            foreach (var prefix in addresses)
            {
                await context.Response.WriteAsync("<a href=\"" + prefix + "\">" + prefix + "</a> <a href=\"?remove=" + prefix + "\">(remove)</a><br>");
            }

            await context.Response.WriteAsync("<form action=\"" + context.Request.PathBase.ToUriComponent() + "\" method=\"GET\">");
            await context.Response.WriteAsync("<input type=\"text\" name=\"add\" value=\"http://localhost:12348\" >");
            await context.Response.WriteAsync("<input type=\"submit\" value=\"Add\">");
            await context.Response.WriteAsync("</form>");

            await context.Response.WriteAsync("</body></html>");
        });
    }

    public static Task Main(string[] args)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseStartup<Startup>()
                    .UseHttpSys();
            })
            .ConfigureLogging(factory => factory.AddConsole())
            .Build();

        return host.RunAsync();
    }
}
