// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Xml.Schema;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace QueueSharing;

public static class Program
{
    public static void Main(string[] args)
    {
        Console.Write("Create and (c)reate, (a)ttach to existing, or attach (o)r create? ");
        var key = Console.ReadKey();
        Console.WriteLine();
        var mode = RequestQueueMode.Create;
        switch (key.KeyChar)
        {
            case 'a':
                mode = RequestQueueMode.Attach;
                break;
            case 'o':
                mode = RequestQueueMode.CreateOrAttach;
                break;
            case 'c':
                mode = RequestQueueMode.Create;
                break;
            default:
                Console.WriteLine("Unknown option, defaulting to (c)create.");
                break;
        }

        var host = new HostBuilder()
            .ConfigureLogging(factory => factory.AddConsole())
            .ConfigureWebHost(webHost =>
            {
                webHost.UseHttpSys(options =>
                {
                    // Skipping this to ensure the server works without any prefixes in attach mode.
                    if (mode != RequestQueueMode.Attach)
                    {
                        options.UrlPrefixes.Add("http://localhost:5002");
                    }
                    options.RequestQueueName = "QueueName";
                    options.RequestQueueMode = mode;
                    options.MaxAccepts = 1; // Better load rotation between instances.
                }).ConfigureServices(services =>
            {

            }).Configure(app =>
            {
                app.Run(async context =>
                {
                    context.Response.ContentType = "text/plain";
                    // There's a strong connection affinity between processes. Close the connection so the next request can be dispatched to a random instance.
                    // It appears to be round robin based and switch instances roughly every 30 requests when using new connections.
                    // This seems related to the default MaxAccepts (5 * processor count).
                    // I'm told this connection affinity does not apply to HTTP/2.
                    context.Response.Headers["Connection"] = "close";
                    await context.Response.WriteAsync("Hello world from " + context.Request.Host + " at " + DateTime.Now);
                });
            });

            })
            .Build();

        host.Run();
    }
}
