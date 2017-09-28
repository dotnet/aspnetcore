// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace CreateDefaultBuilderOfTApp
{
    class Startup
    {
        public void Configure(IApplicationBuilder app, WebHostBuilderContext webHostBuilderContext)
        {
            app.Run(context =>
            {
                return context.Response.WriteAsync(GetResponseMessage(webHostBuilderContext));
            });
        }

        private static string GetResponseMessage(WebHostBuilderContext context)
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
