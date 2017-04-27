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
            string responseMessage = string.Empty;

            WebHost.CreateDefaultBuilder(new[] { "--cliKey", "cliValue" })
                .ConfigureServices((context, services) =>
                {
                    responseMessage = GetResponseMessage(context, services);
                })
                .Configure(app =>
                {
                    app.Map("/error", subApp =>
                    {
                        subApp.Run(context => throw new Exception());
                    });
                    app.Run(context =>
                    {
                        return context.Response.WriteAsync(responseMessage);
                    });
                })
                .Build().Run();
        }

        private static string GetResponseMessage(WebHostBuilderContext context, IServiceCollection services)
        {
            // Verify ContentRootPath set
            if (!string.Equals(Directory.GetCurrentDirectory(), context.HostingEnvironment.ContentRootPath, StringComparison.Ordinal))
            {
                return $"Current directory incorrect. Expected: {Directory.GetCurrentDirectory()} Actual: {context.HostingEnvironment.ContentRootPath}";
            }

            // Verify appsettings.json loaded
            if (!string.Equals("settingsValue", context.Configuration["settingsKey"], StringComparison.Ordinal))
            {
                return $"appsettings.json not loaded into Configuration.";
            }

            // Verify appsettings.environment.json loaded
            if (!string.Equals("devSettingsValue", context.Configuration["devSettingsKey"], StringComparison.Ordinal))
            {
                return $"appsettings.{context.HostingEnvironment.EnvironmentName}.json not loaded into Configuration.";
            }

            // TODO: Verify UserSecrets loaded

            // Verify environment variables loaded
            if (!string.Equals("envValue", context.Configuration["envKey"], StringComparison.Ordinal))
            {
                return $"Environment variables not loaded into Configuration.";
            }

            // Verify command line arguments loaded
            if (!string.Equals("cliValue", context.Configuration["cliKey"], StringComparison.Ordinal))
            {
                return $"Command line arguments not loaded into Configuration.";
            }

            // TODO: Verify AddConsole called
            // TODO: Verify AddDebug called
            // TODO: Verify UseIISIntegration called

            return context.HostingEnvironment.ApplicationName;
        }
    }
}