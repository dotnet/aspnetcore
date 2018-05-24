// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CreateDefaultBuilderOfTApp
{
    class Startup
    {
        public void Configure(IApplicationBuilder app, WebHostBuilderContext webHostBuilderContext)
        {
            app.Run(context =>
            {
                var message = GetResponseMessage(webHostBuilderContext, app.ApplicationServices.GetRequiredService<IOptions<HostFilteringOptions>>());
                return context.Response.WriteAsync(message);
            });
        }

        private static string GetResponseMessage(WebHostBuilderContext context, IOptions<HostFilteringOptions> hostFilteringOptions)
        {
            // Verify ContentRootPath set
            var contentRoot = Environment.GetEnvironmentVariable("ASPNETCORE_CONTENTROOT");
            if (!string.Equals(contentRoot, context.HostingEnvironment.ContentRootPath, StringComparison.Ordinal))
            {
                return $"ContentRootPath incorrect. Expected: {contentRoot} Actual: {context.HostingEnvironment.ContentRootPath}";
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

            // Verify allowed hosts were loaded
            var hosts = string.Join(',', hostFilteringOptions.Value.AllowedHosts);
            if (!string.Equals("example.com,localhost", hosts, StringComparison.Ordinal))
            {
                return $"AllowedHosts not loaded into Options.";
            }

            // TODO: Verify AddConsole called
            // TODO: Verify AddDebug called
            // TODO: Verify UseIISIntegration called

            return context.HostingEnvironment.ApplicationName;
        }
    }
}
