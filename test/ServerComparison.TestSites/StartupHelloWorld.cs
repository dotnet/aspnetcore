// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Runtime;

namespace ServerComparison.TestSites
{
    /// <summary>
    /// To make runtime to load an environment based startup class, specify the environment by the following ways:
    /// 1. Drop a Microsoft.AspNet.Hosting.ini file in the wwwroot folder
    /// 2. Add a setting in the ini file named 'ASPNET_ENV' with value of the format 'Startup[EnvironmentName]'. For example: To load a Startup class named
    /// 'StartupHelloWorld' the value of the env should be 'HelloWorld' (eg. ASPNET_ENV=HelloWorld). Runtime adds a 'Startup' prefix to this and loads 'StartupHelloWorld'.
    /// If no environment name is specified the default startup class loaded is 'Startup'.
    /// Alternative ways to specify environment are:
    /// 1. Set the environment variable named SET ASPNET_ENV=HelloWorld
    /// 2. For selfhost based servers pass in a command line variable named --env with this value. Eg:
    /// "commands": {
    ///    "web": "Microsoft.AspNet.Hosting --server Microsoft.AspNet.Server.WebListener --server.urls http://localhost:5002 --ASPNET_ENV HelloWorld",
    ///  },
    /// </summary>
    public class StartupHelloWorld
    {
        public StartupHelloWorld(IApplicationEnvironment env)
        {
            //Below code demonstrates usage of multiple configuration sources. For instance a setting say 'setting1' is found in both the registered sources,
            //then the later source will win. By this way a Local config can be overridden by a different setting while deployed remotely.
            Configuration = new Configuration(env.ApplicationBasePath)
                        .AddJsonFile("config.json")
                        .AddEnvironmentVariables(); //All environment variables in the process's context flow in as configuration values.
        }

        public IConfiguration Configuration { get; private set; }

        public void ConfigureServices(IServiceCollection services)
        {
            // services.Configure<AppSettings>(Configuration.GetSubKey("AppSettings"));
        }

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