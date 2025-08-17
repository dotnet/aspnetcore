// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CreateDefaultBuilderApp;

public class Program
{
    static void Main(string[] args)
    {
        WebHost.CreateDefaultBuilder()
            .UseUrls("http://127.0.0.1:0")
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton(typeof(IService<>), typeof(Service<>));
                services.AddScoped<IAnotherService, AnotherService>();
            })
            .Configure(app =>
            {
                app.Run(context =>
                {
                    try
                    {
                        context.RequestServices.GetService<IService<IAnotherService>>();
                        return context.Response.WriteAsync("Success");
                    }
                    catch (Exception ex)
                    {
                        return context.Response.WriteAsync(ex.ToString());
                    }
                });
            })
            .Build().Run();
    }

    interface IService<T>
    {
    }

    interface IAnotherService
    {
    }

    class Service<T> : IService<T>
    {
        public Service(T t)
        {
        }
    }

    class AnotherService : IAnotherService
    {
    }
}
