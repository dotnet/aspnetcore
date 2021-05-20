// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Views;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.IIS;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.StackTrace.Sources;


/// <summary>
/// Startup hooks are pieces of code that will run before a users program main executes
/// See: https://github.com/dotnet/core-setup/blob/master/Documentation/design-docs/host-startup-hook.md
/// The type must be named StartupHook without any namespace, and should be internal.
/// </summary>
internal class StartupHook
{
    /// <summary>
    /// Startup hooks are pieces of code that will run before a users program main executes
    /// See: https://github.com/dotnet/core-setup/blob/master/Documentation/design-docs/host-startup-hook.md
    /// </summary>
    public static void Initialize()
    {
        if (!NativeMethods.IsAspNetCoreModuleLoaded())
        {
            // This means someone set the startup hook for Microsoft.AspNetCore.Server.IIS
            // but are not running inprocess. Return at this point.
            return;
        }

        var detailedErrors = Environment.GetEnvironmentVariable("ASPNETCORE_DETAILEDERRORS");
        var enableStartupErrorPage = detailedErrors?.Equals("1", StringComparison.OrdinalIgnoreCase) ?? false;
        enableStartupErrorPage |= detailedErrors?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

        var aspnetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        enableStartupErrorPage |= aspnetCoreEnvironment?.Equals("Development", StringComparison.OrdinalIgnoreCase) ?? false;

        var dotnetEnvironment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        enableStartupErrorPage |= dotnetEnvironment?.Equals("Development", StringComparison.OrdinalIgnoreCase) ?? false;

        if (!enableStartupErrorPage)
        {
            // Not running in development or detailed errors aren't enabled
            return;
        }

        AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
        {
            var exception = (Exception)eventArgs.ExceptionObject;

            // Get the content root from IIS.
            var iisConfigData = NativeMethods.HttpGetApplicationProperties();
            var contentRoot = iisConfigData.pwzFullApplicationPath.TrimEnd(Path.DirectorySeparatorChar);

            var model = ErrorPageModelBuilder.CreateErrorPageModel(
                new PhysicalFileProvider(contentRoot),
                logger: null,
                showDetailedErrors: true,
                exception);

            var errorPage = new ErrorPage(model);

            var stream = new MemoryStream();

            // Never will go async because we are writing to a memory stream.
            errorPage.ExecuteAsync(stream).GetAwaiter().GetResult();

            // Get the raw content and set the error page.
            stream.Position = 0;
            var content = stream.ToArray();

            NativeMethods.HttpSetStartupErrorPageContent(content);
        };
    }
}
