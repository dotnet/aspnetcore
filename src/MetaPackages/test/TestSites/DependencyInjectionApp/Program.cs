// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;

namespace CreateDefaultBuilderApp
{
    public class Program
    {
        static void Main(string[] args)
        {
            WebHost.CreateDefaultBuilder()
                .UseUrls("http://localhost:5002")
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

        class Service<T>: IService<T>
        {
            public Service(T t)
            {
            }
        }

        class AnotherService: IAnotherService
        {
        }
    }
}
