// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Diagnostics;
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
    /// 'StartupHelloWorld' the value of the env should be 'NtlmAuthentication' (eg. ASPNET_ENV=HelloWorld). Runtime adds a 'Startup' prefix to this and loads 'StartupHelloWorld'.
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

            // Error page middleware displays a nice formatted HTML page for any unhandled exceptions in the request pipeline.
            // Note: ErrorPageOptions.ShowAll to be used only at development time. Not recommended for production.
            app.UseErrorPage(ErrorPageOptions.ShowAll);

            // Add the runtime information page that can be used by developers
            // to see what packages are used by the application
            // default path is: /runtimeinfo
            app.UseRuntimeInfoPage();

            // Add static files to the request pipeline
            // app.UseStaticFiles();

            app.Run(ctx =>
            {
                return ctx.Response.WriteAsync("Hello World");
            });
        }
    }
}