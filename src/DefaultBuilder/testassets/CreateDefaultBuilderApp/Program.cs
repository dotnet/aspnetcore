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
        string responseMessage = null;

        WebHost.CreateDefaultBuilder(new[] { "--cliKey", "cliValue" })
            .ConfigureServices((context, services) => responseMessage = responseMessage ?? GetResponseMessage(context))
            .ConfigureKestrel(options => options
                .Configure(options.ConfigurationLoader.Configuration)
                .Endpoint("HTTP", endpointOptions =>
                {
                    if (responseMessage == null
                        && !string.Equals("KestrelEndPointSettingValue", endpointOptions.ConfigSection["KestrelEndPointSettingName"]))
                    {
                        responseMessage = "Default Kestrel configuration not read.";
                    }
                }))
            .Configure(app => app.Run(context =>
            {
                var hostingEnvironment = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
                return context.Response.WriteAsync(responseMessage ?? hostingEnvironment.ApplicationName);
            }))
            .Build().Run();
    }

    private static string GetResponseMessage(WebHostBuilderContext context)
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

        // TODO: Verify AddConsole called
        // TODO: Verify AddDebug called
        // TODO: Verify UseIISIntegration called

        return null;
    }
}
