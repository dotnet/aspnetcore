// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ServerComparison.TestSites
{
    /// <summary>
    /// To make runtime to load an environment based startup class, specify the environment by the following ways:
    /// 1. Drop a Microsoft.AspNetCore.Hosting.ini file in the wwwroot folder
    /// 2. Add a setting in the ini file named 'ASPNET_ENV' with value of the format 'Startup[EnvironmentName]'. For example: To load a Startup class named
    /// 'StartupHelloWorld' the value of the env should be 'HelloWorld' (eg. ASPNET_ENV=HelloWorld). Runtime adds a 'Startup' prefix to this and loads 'StartupHelloWorld'.
    /// If no environment name is specified the default startup class loaded is 'Startup'.
    /// Alternative ways to specify environment are:
    /// 1. Set the environment variable named SET ASPNET_ENV=HelloWorld
    /// 2. For selfhost based servers pass in a command line variable named --env with this value. Eg:
    /// "commands": {
    ///    "web": "Microsoft.AspNetCore.Hosting --server Microsoft.AspNetCore.Server.WebListener --server.urls http://localhost:5002 --ASPNET_ENV HelloWorld",
    ///  },
    /// </summary>
    public class StartupHelloWorld
    {
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(minLevel: LogLevel.Warning);

            app.Run(ctx =>
            {
                return ctx.Response.WriteAsync("Hello World");
            });
        }
    }
}